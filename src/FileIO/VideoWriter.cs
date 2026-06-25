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
/// Creates a new video writer, using specified file path. Saved videos will be compressed using MJPEG.
/// </summary>
/// <param name="filePath">File path to save videos to.</param>
/// <param name="fps">Frame rate in frames per second. If not specified (0.0), it will be calculated from input buffer timestamps.</param>
public class VideoWriter(string filePath, double fps = 0.0) : IDisposable
{
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
    public string FilePath { get; init; } = filePath;

    /// <summary>
    /// Frame rate (in frames per second).
    /// </summary>
    public double FPS { get; private set; } = fps;

    #endregion

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
                    // Write buffer (converted to Mat).
                    _videoWriter?.Write(buffer.ToMat());

                    FramesWritten++;
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
        if (_bufferQueue.IsEmpty == false)
            if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
                GcLibrary.Logger.LogDebug("{bufferCount} buffers remaining. Finishing up...", _bufferQueue.Count);

        while (_bufferQueue.TryDequeue(out var buffer))
        {
            try
            {            
                if (FPS == 0.0)
                    FPS = TimeSpan.TicksPerSecond / (_timeStamps.Max() - (double)_timeStamps.Min()) * (_timeStamps.Size - 1); // Calculate average fps.

                // Initialize new video writer (if not done already), using MJPEG compression, calculated fps and buffer properties.
                _videoWriter ??= new(fileName: FilePath, compressionCode: Emgu.CV.VideoWriter.Fourcc('M', 'J', 'P', 'G'), fps: FPS, size: new Size((int)buffer.Width, (int)buffer.Height), isColor: buffer.NumChannels > 1);

                // Write buffer (converted to Mat).
                _videoWriter.Write(buffer.ToMat());

                FramesWritten++;
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

        if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
            GcLibrary.Logger.LogDebug("Writing complete. {bufferCount} buffers remaining.", _bufferQueue.Count);
    }

    #endregion

    #region Events

    /// <summary>
    /// Event-handling method to BufferTransferred events, queuing transferred buffer for writing.
    /// </summary>
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
            if (FPS == 0.0)
                FPS = TimeSpan.TicksPerSecond / (_timeStamps.Max() - (double)_timeStamps.Min()) * (_timeStamps.Size - 1); // Calculate average fps.

            // Initialize new video writer (if not done already), using MJPEG compression, calculated fps and buffer properties.
            _videoWriter ??= new(fileName: FilePath, compressionCode: Emgu.CV.VideoWriter.Fourcc('M', 'J', 'P', 'G'), fps: FPS, size: new Size((int)e.Buffer.Width, (int)e.Buffer.Height), isColor: e.Buffer.NumChannels > 1);
            
            // Proceed with thread.
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