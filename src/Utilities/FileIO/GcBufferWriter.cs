using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;

namespace GcLib.Utilities.FileIO;

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
    private volatile bool _recordingThreadStoppingCondition;

    private const int StateIdle = 0;
    private const int StateStarting = 1;
    private const int StateRunning = 2;
    private const int StateStopping = 3;

    /// <summary>
    /// Writer state (idle, starting, running or stopping), changed atomically.
    /// </summary>
    private int _state = StateIdle;

    /// <summary>
    /// Non-zero if writing was aborted by an error (buffers are then no longer accepted).
    /// </summary>
    private int _aborted;

    /// <summary>
    /// Serializes claiming of the stop operation.
    /// </summary>
    private readonly Lock _stopLock = new();

    /// <summary>
    /// Stop operation in progress (shared by concurrent callers of <see cref="StopAsync"/>).
    /// </summary>
    private Task _stopTask;

    /// <summary>
    /// Non-zero if object has been disposed.
    /// </summary>
    private int _disposed;

    private long _fileSize;
    private int _buffersWritten;

    /// <summary>
    /// Reusable image header (only used by one thread at a time, as the recording thread is joined before remaining buffers are written).
    /// </summary>
    private readonly byte[] _imageHeader = new byte[ImageHeaderSize];

    /// <summary>
    /// Width, height and pixel format of the first buffer written. The reader requires all frames in a file to have the same size.
    /// </summary>
    private (uint Width, uint Height, PixelFormat PixelFormat)? _frameFormat;

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
    public long FileSize => Interlocked.Read(ref _fileSize);

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
    public int BuffersWritten => Volatile.Read(ref _buffersWritten);

    /// <summary>
    /// Flag indicating whether writing is currently active (from <see cref="Start"/> until <see cref="StopAsync"/> has completed, also after writing was aborted by an error).
    /// </summary>
    public bool IsWriting => Volatile.Read(ref _state) != StateIdle;

    /// <summary>
    /// Flag indicating priority of recording thread (default is <see cref="ThreadPriority.BelowNormal"/>).
    /// </summary>
    public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.BelowNormal;

    /// <summary>
    /// True if writer has been disposed (and is unuseable). Instantiate new object to write new data.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

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
        string directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory) == false)
            _ = Directory.CreateDirectory(directory);

        // Initialize buffer queue.
        _bufferQueue = new ConcurrentQueue<GcBuffer>();

        // Open asynchronous filestream, creating new file (or overwriting already existing one), with write access and without file sharing permission.          
        _fileStream = new FileStream(path: filePath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None, bufferSize: 4096, useAsync: true);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Stops recording, discards queued buffers, and releases resources.
    /// </summary>
    public void Dispose()
    {
        // Only the first call disposes.
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            // Stop recording thread, discarding queued buffers (waits for a stop already in progress).
            StopAsync(discardRemaining: true).Wait();
        }
        finally
        {
            _bufferQueue.Clear();
            _waitHandle.Dispose();
            _fileStream.Dispose();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event-handling method to BufferTransferred events, queuing transferred buffer for writing.
    /// </summary>
    public void OnBufferTransferred(object sender, BufferTransferredEventArgs e)
    {
        // Ignore buffers unless writing is active (so that the queue cannot grow unbounded when idle, stopping or after an error).
        if (Volatile.Read(ref _state) != StateRunning || Volatile.Read(ref _aborted) != 0)
            return;

        // Queue transferred buffer.
        _bufferQueue.Enqueue(e.Buffer);

        // Proceed with thread.
        try
        {
            _ = _waitHandle.Set();
        }
        catch (ObjectDisposedException)
        {
            // Writer was disposed concurrently.
        }
    }

    /// <summary>
    /// Event announcing that writing was aborted by an error (e.g., an <see cref="IOException"/>), with the exception in the event arguments. Raised once per recording.
    /// </summary>
    /// <remarks>The event is raised on a thread-pool thread, so handlers can safely stop or dispose the writer. Writing must still be stopped by calling <see cref="StopAsync"/>.</remarks>
    public event EventHandler<WritingAbortedEventArgs> WritingAborted;

    /// <summary>
    /// Event-invoking method, announcing that an exception was thrown while writing, with the exception in the event arguments.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="ex">Exception that was thrown.</param>
    private void OnWritingAborted(string message, Exception ex)
    {
        var handler = WritingAborted; // Copy to a temporary variable to be thread-safe.
        if (handler == null)
            return;

        var eventArgs = new WritingAbortedEventArgs(message, ex);

        // Always raised on a thread-pool thread: handlers may stop or dispose the writer, which would deadlock if invoked on the recording thread or inside the stop operation.
        _ = ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                handler.Invoke(this, eventArgs);
            }
            catch (Exception handlerException)
            {
                if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                    GcLibrary.Logger.LogError(handlerException, "WritingAborted event handler threw an exception");
            }
        });
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

        // Atomically claim the writer (buffers are not accepted until it has been reset below).
        if (Interlocked.CompareExchange(ref _state, StateStarting, StateIdle) != StateIdle)
            throw new InvalidOperationException("Writer is already in use!");

        // Reset state from any previous run.
        _bufferQueue.Clear();
        _ = _waitHandle.Reset();
        _recordingThreadStoppingCondition = false;
        Volatile.Write(ref _aborted, 0);

        try
        {
            // Write file header.
            _ = Interlocked.Add(ref _fileSize, WriteToFileStream(GetFileHeader()));
        }
        catch (Exception ex)
        {
            if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                GcLibrary.Logger.LogError(ex, "Failed to write data to {filePath}", FilePath);

            Volatile.Write(ref _state, StateIdle); // Reset state to idle, so that writer can be restarted.

            // Raise event.
            OnWritingAborted(ex.Message, ex);
            return;
        }

        // Start recording thread (as background thread, so that it never keeps the process alive).
        _recordingThread = new Thread(ThreadProc) { IsBackground = true, Priority = ThreadPriority };

        // Start accepting buffers.
        Volatile.Write(ref _state, StateRunning);

        try
        {
            _recordingThread.Start();
        }
        catch
        {
            Volatile.Write(ref _state, StateIdle); // Reset state to idle, so that writer can be restarted.
            throw;
        }
    }

    /// <summary>
    /// Stops writing buffers to file (asynchronously). Concurrent calls share the same stop operation. Does nothing if the writer is not writing.
    /// </summary>
    /// <param name="discardRemaining">(Optional) If true, remaining buffers in recording queue are discarded.</param>
    /// <returns>(awaitable) Task.</returns>
    public Task StopAsync(bool discardRemaining = false)
    {
        lock (_stopLock)
        {
            // Only one caller can stop a running writer.
            if (Interlocked.CompareExchange(ref _state, StateStopping, StateRunning) == StateRunning)
                _stopTask = StopCoreAsync(discardRemaining);
            else if (Volatile.Read(ref _state) != StateStopping)
                return Task.CompletedTask;

            return _stopTask;
        }
    }

    #endregion

    #region Private methods

    private async Task StopCoreAsync(bool discardRemaining)
    {
        // Stop recording thread.
        _recordingThreadStoppingCondition = true;

        // Stop thread immediately (without waiting for next buffer).
        _ = _waitHandle.Set();

        try
        {
            // Wait for thread to terminate and write remaining buffers to file (off the caller's thread).
            // Do not capture the caller's context, as Dispose blocks on this task and would otherwise deadlock on e.g. a UI thread.
            await Task.Run(() =>
            {
                _recordingThread?.Join();

                if (discardRemaining || Volatile.Read(ref _aborted) != 0)
                    _bufferQueue.Clear();
                else
                    WriteRemainingBuffers();

                // Flush to file, so that I/O errors are reported as aborted writing (rather than when disposing).
                if (Volatile.Read(ref _aborted) == 0)
                {
                    try
                    {
                        _fileStream.Flush();
                    }
                    catch (Exception ex)
                    {
                        HandleWriteError(ex);
                    }
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _bufferQueue.Clear();
            Volatile.Write(ref _state, StateIdle); // Reset state to idle, so that writer can be restarted.
        }
    }

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
                    WriteBuffer(buffer);
                }
                catch (Exception ex)
                {
                    HandleWriteError(ex);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Write remaining buffers in queue to file.
    /// </summary>
    private void WriteRemainingBuffers()
    {
        if (_bufferQueue.IsEmpty)
            return;

        if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
            GcLibrary.Logger.LogDebug("{bufferCount} buffers remaining. Finishing up...", _bufferQueue.Count);

        while (Volatile.Read(ref _aborted) == 0 && _bufferQueue.TryDequeue(out GcBuffer buffer))
        {
            try
            {
                WriteBuffer(buffer);
            }
            catch (Exception ex)
            {
                HandleWriteError(ex);
                break;
            }
        }

        if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
            GcLibrary.Logger.LogDebug("Writing complete. {bufferCount} buffers remaining.", _bufferQueue.Count);
    }

    /// <summary>
    /// Aborts writing after an error (clearing the queue) and reports it once.
    /// </summary>
    private void HandleWriteError(Exception ex)
    {
        // Stop accepting buffers before clearing the queue, so that nothing is queued behind the clear.
        bool alreadyReported = Interlocked.Exchange(ref _aborted, 1) != 0;
        _bufferQueue.Clear();

        if (alreadyReported)
            return;

        // Log error.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
            GcLibrary.Logger.LogError(ex, "Failed to write data to {filePath}", FilePath);

        // Raise event.
        OnWritingAborted(ex.Message, ex);
    }

    /// <summary>
    /// Returns the size (in bytes) of the image data of a buffer with specified size and pixel format (rounded up to whole bytes for packed formats).
    /// </summary>
    internal static long GetImageDataSize(uint width, uint height, PixelFormat pixelFormat)
    {
        return (((long)width * height * GenICamHelper.GetBitsPerPixel(pixelFormat)) + 7) / 8;
    }

    /// <summary>
    /// Writes buffer (image header and image data) to file.
    /// </summary>
    /// <exception cref="IOException"></exception>
    /// <exception cref="InvalidOperationException">Buffer differs in size or pixel format from the first buffer written.</exception>
    private void WriteBuffer(GcBuffer buffer)
    {
        // The reader derives the frame size from the first frame, so all frames must have the same size.
        var format = (buffer.Width, buffer.Height, buffer.PixelFormat);
        _frameFormat ??= format;
        if (_frameFormat.Value != format)
            throw new InvalidOperationException($"Buffer ({buffer.Width}x{buffer.Height}, {buffer.PixelFormat}) differs from the first buffer written ({_frameFormat.Value.Width}x{_frameFormat.Value.Height}, {_frameFormat.Value.PixelFormat})! All images in a file must have the same size and pixel format.");

        // Write exactly the image data size the reader expects (the buffer's array may be larger).
        long imageDataSize = GetImageDataSize(buffer.Width, buffer.Height, buffer.PixelFormat);
        if (imageDataSize > int.MaxValue)
            throw new InvalidOperationException($"Image data of {imageDataSize} bytes is too large to write!");

        // Image header.
        Span<byte> header = _imageHeader;
        BinaryPrimitives.WriteInt64LittleEndian(header[0..], buffer.FrameID); // frame ID
        BinaryPrimitives.WriteUInt64LittleEndian(header[8..], buffer.TimeStamp); // timestamp
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..], buffer.Width); // width
        BinaryPrimitives.WriteUInt32LittleEndian(header[20..], buffer.Height); // height
        BinaryPrimitives.WriteInt32LittleEndian(header[24..], (int)buffer.PixelFormat); // pixel format
        BinaryPrimitives.WriteUInt32LittleEndian(header[28..], buffer.PixelDynamicRangeMax); // dynamic range

        _fileStream.Write(_imageHeader, 0, _imageHeader.Length);
        _fileStream.Write(buffer.ImageData, 0, (int)imageDataSize);

        _ = Interlocked.Add(ref _fileSize, _imageHeader.Length + imageDataSize);
        _ = Interlocked.Increment(ref _buffersWritten);
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

    #endregion
}