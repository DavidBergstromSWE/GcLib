using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GcLib;

/// <summary>
/// Writer of image buffers to file.
/// </summary>
public sealed class GcBufferWriter : IDisposable
{
    #region Fields

    /// <summary>
    /// Provides a stream for a file, supporting both synchronous and asynchronous read and write operations.
    /// TODO: Replace with interface?
    /// </summary>
    private readonly FileStream _fileStream;

    /// <summary>
    /// Buffer queue, containing images waiting to be written to file.
    /// </summary>
    private readonly ConcurrentQueue<GcBuffer> _bufferQueue;

    /// <summary>
    /// Recording thread used in writing to file.
    /// </summary>
    private Thread _recordingThread;

    /// <summary>
    /// Stopping condition for recording thread.
    /// </summary>
    private bool _recordingThreadStoppingCondition;

    /// <summary>
    /// True if object has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Waithandle used for blocking recording thread while waiting for buffers to be queued.
    /// </summary>
    private readonly AutoResetEvent _waitHandle = new(false);

    #endregion

    #region Properties

    /// <summary>
    /// Size of file header (in bytes). File header currently is empty.
    /// </summary>
    public static readonly uint FileHeaderSize = 0;

    /// <summary>
    /// Size of image header (in bytes). Image header contains the following properties:
    /// <para>
    /// <list type="bullet">
    /// <item><see cref="GcBuffer.FrameID"/></item>
    /// <item><see cref="GcBuffer.TimeStamp"/></item>
    /// <item><see cref="GcBuffer.Width"/></item>
    /// <item><see cref="GcBuffer.Height"/></item>
    /// <item><see cref="GcBuffer.PixelFormat"/></item>
    /// <item><see cref="GcBuffer.PixelDynamicRangeMax"/></item>
    /// </list>
    /// </para> 
    /// </summary>
    public static readonly uint ImageHeaderSize = sizeof(long) // Frame ID
                                                  + sizeof(ulong) // Timestamp
                                                  + sizeof(uint) // Width
                                                  + sizeof(uint) // Height
                                                  + sizeof(int) // Pixel format
                                                  + sizeof(uint); // Dynamic range

    /// <summary>
    /// Total size of file (in bytes).
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// A relative or absolute path to the file.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// Number of buffers currently queued for writing.
    /// </summary>
    public int BuffersQueued => _bufferQueue.Count;

    /// <summary>
    /// Number of buffers written to file.
    /// </summary>
    public int BuffersWritten { get; private set; } = 0;

    /// <summary>
    /// Flag indicating whether writing is currently active.
    /// </summary>
    public bool IsWriting { get; private set; } = false;

    /// <summary>
    /// Flag indicating priority of recording thread (default is <see cref="ThreadPriority.BelowNormal"/>).
    /// </summary>
    public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.BelowNormal;

    /// <summary>
    /// True if writer has been disposed (and is unuseable). Instantiate new object to write new data.
    /// </summary>
    public bool IsDisposed => _disposed;

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new writer of images, for recording images (and chunk data) to specified file.
    /// </summary>
    /// <param name="filePath">A relative or absolute path to the file.</param>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="PathTooLongException"></exception>
    public GcBufferWriter(string filePath)
    {
        FilePath = filePath;

        // Create directory if it doesn't already exist.
        _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // Initialize buffer queue.
        _bufferQueue = new ConcurrentQueue<GcBuffer>();

        // Open asynchronous filestream, creating new file (or overwriting already existing one), with write access and without file sharing permission.          
        _fileStream = new FileStream(path: filePath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None, bufferSize: 4096, useAsync: true);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and (optionally) managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources, called from the finalizer only.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).

                // Flush queue.
                _bufferQueue.Clear();
            }

            // Free unmanaged resources (unmanaged objects) (and set large fields to null).

            // Dispose WaitHandle.
            _waitHandle?.Dispose();

            // Dispose filestream.
            _fileStream?.Dispose();

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~GcBufferWriter()
    {
        Dispose(false);
    }

    #endregion

    #region Events

    /// <summary>
    /// Event-handling method to BufferTransferred events, queuing transferred buffer for writing.
    /// </summary>
    public void OnBufferTransferred(object sender, BufferTransferredEventArgs e)
    {
        // Queue transferred buffer.
        _bufferQueue.Enqueue(e.Buffer);

        // Proceed with thread.
        _ = _waitHandle.Set();
    }

    /// <summary>
    /// Event announcing that an <see cref="IOException"/> was thrown while writing, with the exception in the event arguments.
    /// </summary>
    public event EventHandler<WritingAbortedEventArgs> WritingAborted;

    /// <summary>
    /// Event-invoking method, announcing that an <see cref="IOException"/> was thrown while writing, with the exception in the event arguments.
    /// </summary>
    /// <param name="ex">Exception that was thrown.</param>
    private void OnWritingAborted(string message, IOException ex)
    {
        WritingAborted?.Invoke(this, new WritingAbortedEventArgs(message, ex));
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Starts writing buffers to file.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (IsWriting)
            throw new InvalidOperationException("Writer is already in use!");

        IsWriting = true;

        try
        {
            // Write file header.
            FileSize += WriteToFileStream(GetFileHeader());
        }
        catch (IOException ex)
        {
            if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                GcLibrary.Logger.LogError(ex, "Failed to write data to {filePath}", FilePath);

            // Raise event.
            OnWritingAborted(ex.Message, ex);
            return;
        }

        // Start recording thread.
        _recordingThread = new Thread(ThreadProc);
        _recordingThread.Start();
    }

    /// <summary>
    /// Stops writing buffers to file (asynchronously).
    /// </summary>
    /// <param name="discardRemaining">(Optional) If true, remaining buffers in recording queue are discarded.</param>
    /// <returns>(awaitable) Task.</returns>
    public async Task StopAsync(bool discardRemaining = false)
    {
        if (IsWriting == false)
            return;

        // Stop recording thread.
        _recordingThreadStoppingCondition = true;

        // Stop thread immediately (without waiting for next buffer).
        _ = _waitHandle.Set();

        // Wait for thread to terminate (bad practice?).
        _recordingThread?.Join();

        if (discardRemaining == false)
        {
            // Write remaining buffers in queue to file.
            await WriteRemainingBuffersAsync();
        }
        else
        {
            // Discard buffers remaining.
            _bufferQueue.Clear();
        }

        IsWriting = false;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Continuously waits for buffers to be queued and writes queued buffers to file.
    /// </summary>
    private void ThreadProc()
    {
        while (_recordingThreadStoppingCondition == false)
        {
            // Block thread until buffer has been queued.  
            if (_bufferQueue.IsEmpty)
                _ = _waitHandle.WaitOne();

            // Retrieve buffer from queue and write to file.
            if (_bufferQueue.TryDequeue(out GcBuffer buffer))
            {
                try
                {
                    FileSize += WriteToFileStream(GetImageData(buffer));
                    //throw new IOException("Disk is full!");
                }
                catch (IOException ex)
                {
                    // Log error.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                        GcLibrary.Logger.LogError(ex, "Failed to write data to {filePath}", FilePath);

                    // Clear buffer.
                    _bufferQueue.Clear();

                    // Raise event.
                    OnWritingAborted(ex.Message, ex);
                    break;
                }

                BuffersWritten++;
            }
        }
    }

    /// <summary>
    /// Write remaining buffers in queue to file.
    /// </summary>
    private async Task WriteRemainingBuffersAsync()
    {
        if (_bufferQueue.IsEmpty == false)
        {
            if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
                GcLibrary.Logger.LogDebug("{bufferCount} buffers remaining. Finishing up...", _bufferQueue.Count);
            while (_bufferQueue.TryDequeue(out GcBuffer buffer))
            {
                try
                {
                    FileSize += await WriteToFileStreamAsync(GetImageData(buffer));
                }
                catch (IOException ex)
                {
                    // Log error.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                        GcLibrary.Logger.LogError(ex, "Failed to write data to {filePath}", FilePath);

                    // Clear buffer.
                    _bufferQueue.Clear();

                    // Raise event.
                    OnWritingAborted(ex.Message, ex);
                    break;
                }

                BuffersWritten++;
            }
            if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
                GcLibrary.Logger.LogDebug("Writing complete. {bufferCount} buffers remaining.", _bufferQueue.Count);
        }
    }

    /// <summary>
    /// Retrieve file header.
    /// </summary>
    /// <returns>File header (currently empty).</returns>
    private static byte[] GetFileHeader()
    {
        // Allocate space for file header.
        byte[] fileHeaderArray = null;

        // ToDo: Add useful info to file header.

        return fileHeaderArray;
    }

    /// <summary>
    /// Get image and chunk data from buffer.
    /// </summary>
    /// <param name="buffer">Buffer, containing image and chunk data.</param>
    /// <returns>Byte array containing image header and image itself.</returns>
    private static byte[] GetImageData(GcBuffer buffer)
    {
        // Allocate space for header and image.
        byte[] imageData = new byte[ImageHeaderSize + buffer.ImageData.Length];

        // Copy image header data.
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.FrameID), 0, imageData, 0, 8); // frame ID
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.TimeStamp), 0, imageData, 8, 8); // timestamp
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Width), 0, imageData, 16, 4); // width
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Height), 0, imageData, 20, 4); // height
        Buffer.BlockCopy(BitConverter.GetBytes((int)buffer.PixelFormat), 0, imageData, 24, 4); // pixel format         
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.PixelDynamicRangeMax), 0, imageData, 28, 4); // dynamic range        

        // Copy image data.
        Buffer.BlockCopy(buffer.ImageData, 0, imageData, (int)ImageHeaderSize, buffer.ImageData.Length);

        return imageData;
    }

    /// <summary>
    /// Writes data to file.
    /// </summary>
    /// <param name="byteArray">Data to write.</param>
    /// <returns>Number of bytes written.</returns>
    /// <exception cref="IOException"></exception>
    private int WriteToFileStream(byte[] byteArray)
    {
        if (byteArray != null)
        {
            _fileStream.Write(buffer: byteArray, offset: 0, count: byteArray.Length);
            return byteArray.Length;
        }
        else return 0;
    }

    /// <summary>
    /// Writes data to file (asynchronously).
    /// </summary>
    /// <param name="byteArray">Data to write.</param>
    /// <returns>Number of bytes written.</returns>
    /// <exception cref="IOException"></exception>
    private async Task<int> WriteToFileStreamAsync(byte[] byteArray)
    {
        if (byteArray != null)
        {
            await _fileStream.WriteAsync(buffer: byteArray);
            return byteArray.Length;
        }
        else return 0;
    }

    #endregion
}