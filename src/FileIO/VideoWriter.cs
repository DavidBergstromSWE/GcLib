using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GcLib.Utilities.Collections;
using Microsoft.Extensions.Logging;

namespace GcLib.FileIO;

/// <summary>
/// Video writer, taking buffers and compressing them into a mp4 file using a specified video codec (supported codecs are Motion JPEG, H.264 and H.265).
/// </summary>
public class VideoWriter : IDisposable
{
    #region Enums

    /// <summary>
    /// Supported video codecs.
    /// </summary>
    public enum CODEC
    {
        /// <summary>
        /// Motion JPEG. Intraframe-only compression in which each video frame or interlaced field of a digital video sequence is compressed separately as a JPEG image.
        /// </summary>
        MJPEG = 1196444237,
        /// <summary>
        /// H.264/AVC (Advanced Video Coding). Block-oriented, motion-compensated compression using integer discrete cosine transform (DCT) with 4×4 and 8×8 block sizes.
        /// </summary>
        H264 = 875967048,
        /// <summary>
        /// H.265/HEVC (High Efficiency Video Coding). Block-oriented, motion-compensated compression using both integer discrete cosine transform (DCT) and discrete sine transform (DST) with varied block sizes between 4×4 and 32×32.
        /// </summary>
        H265 = 892744264
    }

    #endregion

    #region Fields

    /// <summary>
    /// Video writer.
    /// </summary>
    private Emgu.CV.VideoWriter _videoWriter;

    /// <summary>
    /// Buffer queue, containing images waiting to be written to file.
    /// </summary>
    private readonly ConcurrentQueue<GcBuffer> _bufferQueue = new();

    /// <summary>
    /// Circular buffer of timestamps. Increase its capacity to improve accuracy of fps calculation.
    /// </summary>
    private readonly CircularBuffer<ulong> _timeStamps = new(capacity: 30, allowOverflow: false);

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
    /// Number of buffers currently queued for writing.
    /// </summary>
    public int BuffersQueued => _bufferQueue.Count;

    /// <summary>
    /// Number of frames written to video file.
    /// </summary>
    public int FramesWritten { get; private set; } = 0;

    /// <summary>
    /// Flag indicating whether writing is currently active.
    /// </summary>
    public bool IsWriting { get; private set; } = false;

    /// <summary>
    /// True if writer has been disposed (and is unuseable). Instantiate new object to write new data.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// A relative or absolute path to the file.
    /// </summary>
    public string FilePath { get; init; }

    /// <summary>
    /// Frame rate (in frames per second).
    /// </summary>
    public double FPS { get; private set; }

    /// <summary>
    /// Video codec to use im compression.
    /// </summary>
    public CODEC Codec { get; }

    #endregion

    /// <summary>
    /// Creates a new video writer, taking buffers and compressing them into a video file using the specified <paramref name="filePath"/> (in mp4 format). 
    /// Videos will be saved at a frame rate specified by <paramref name="fps"/>. If no frame rate is specified, it will be calculated from buffer timestamps. 
    /// Saved video will be compressed using the scheme specified by <paramref name="codec"/>.
    /// </summary>
    /// <param name="filePath">File path to save videos to (must have mp4 extension).</param>
    /// <param name="fps">Frame rate in frames per second. If not specified, it will be calculated from input buffer timestamps.</param>
    /// <param name="codec">Video codec to use in video compression.</param>
    public VideoWriter(string filePath, double fps = 0.0, VideoWriter.CODEC codec = VideoWriter.CODEC.MJPEG)
    {
        FilePath = filePath;
        FPS = fps;
        Codec = codec;
    }

    #region Public methods

    /// <summary>
    /// Start writing video frames.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (IsWriting)
            throw new InvalidOperationException("Writer is already in use!");

        // Start recording thread.
        _recordingThread = new Thread(ThreadProc);
        _recordingThread.Start();

        IsWriting = true;
    }

    /// <summary>
    /// Stop writing video frames.
    /// </summary>
    public async Task StopAsync()
    {
        if (IsWriting == false)
            return;

        // Stop recording thread.
        _recordingThreadStoppingCondition = true;

        // Stop thread immediately (without waiting for next buffer).
        _ = _waitHandle.Set();

        // Wait for thread to terminate.
        _recordingThread?.Join();

        // Write remaining buffers to file.
        await WriteRemainingBuffersAsync();

        IsWriting = false;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Continuously waits for buffers to be queued and writes queued buffers to video file.
    /// </summary>
    private void ThreadProc()
    {
        while (_recordingThreadStoppingCondition == false)
        {
            // Block thread until buffer has been queued.  
            if (_bufferQueue.IsEmpty)
                _ = _waitHandle.WaitOne();

            // Retrieve buffer from queue.
            if (_bufferQueue.TryDequeue(out GcBuffer buffer))
            {
                try
                {
                    WriteBuffer(buffer);
                }
                catch (Exception ex)
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
            }
        }
    }

    /// <summary>
    /// Write remaining buffers in queue to file.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    private async Task WriteRemainingBuffersAsync()
    {
        while (_bufferQueue.TryDequeue(out var buffer))
        {
            try
            {
                WriteBuffer(buffer);
            }
            catch (Exception ex)
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
        }
    }

    /// <summary>
    /// Write buffer to video file.
    /// </summary>
    /// <param name="buffer">Buffer to be written to file.</param>
    private void WriteBuffer(GcBuffer buffer)
    {
        if (FPS == 0.0)
            FPS = TimeSpan.TicksPerSecond / (_timeStamps.Max() - (double)_timeStamps.Min()) * (_timeStamps.Size - 1); // Calculate average fps.

        // Initialize new video writer (if not done already), using MJPEG compression, calculated fps and buffer properties.
        _videoWriter ??= new(fileName: FilePath, compressionCode: (int)Codec, fps: FPS, size: new Size((int)buffer.Width, (int)buffer.Height), isColor: buffer.NumChannels > 1);
        
        // Write buffer (converted to Mat).
        _videoWriter.Write(buffer.ToMat());

        FramesWritten++;
    }

    #endregion

    #region Events

    /// <summary>
    /// Event-handling method to BufferTransferred events, queuing transferred buffer for writing.
    public void OnBufferTransferred(object sender, BufferTransferredEventArgs e)
    {
        if (IsWriting == false) 
            return;

        // Queue transferred buffer.
        _bufferQueue.Enqueue(e.Buffer);

        if (_timeStamps.IsFull == false)
            _timeStamps.Put(e.Buffer.TimeStamp); // Add buffer timestamp to circular buffer.
        else
        {         
            // Proceed with recording thread.
            _ = _waitHandle.Set();
        }
    }

    /// <summary>
    /// Event announcing that an <see cref="IOException"/> was thrown while writing, with the exception in the event arguments.
    /// </summary>
    public event EventHandler<WritingAbortedEventArgs> WritingAborted;

    /// <summary>
    /// Event-invoking method, announcing that an <see cref="IOException"/> was thrown while writing, with the exception in the event arguments.
    /// </summary>
    /// <param name="ex">Exception that was thrown.</param>
    private void OnWritingAborted(string message, Exception ex)
    {
        WritingAborted?.Invoke(this, new WritingAbortedEventArgs(message, ex));
    }

    #endregion

    #region IDisposable

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
                // Stop recording thread.
                if (IsWriting)
                    StopAsync().Wait();

                // Dispose writer.
                _videoWriter?.Dispose();

                // Flush queues.
                _bufferQueue.Clear();
                _timeStamps.Clear();
            }

            // Free unmanaged resources (unmanaged objects) (and set large fields to null).

            // Dispose WaitHandle.
            _waitHandle?.Dispose();

            _disposed = true;
        }
    }

    #endregion
}