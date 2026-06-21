using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace GcLib.FileIO;

public class VideoWriter : IDisposable
{
    private readonly Emgu.CV.VideoWriter _videoWriter;

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

    /// <summary>
    /// Number of buffers currently queued for writing.
    /// </summary>
    public int BuffersQueued => _bufferQueue.Count;

    /// <summary>
    /// Number of buffers written to file.
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
    public string FilePath { get; private set; }

    public VideoWriter(string filePath, int compressionCode, int fps, Size size, bool isColor)
    {
        _videoWriter = new(filePath, compressionCode, fps, size, isColor);
        _bufferQueue = new();
        FilePath = filePath;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (IsWriting)
            throw new InvalidOperationException("Writer is already in use!");

        // Start recording thread.
        _recordingThread = new Thread(ThreadProc);
        _recordingThread.Start();
    }

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
                    _videoWriter.Write(buffer.ToMat());
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

                FramesWritten++;
            }
        }
    }

    ~VideoWriter() 
    { 
        _videoWriter.Dispose(); 
    }


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
    private void OnWritingAborted(string message, Exception ex)
    {
        WritingAborted?.Invoke(this, new WritingAbortedEventArgs(message, ex));
    }

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

            _disposed = true;
        }
    }
}
