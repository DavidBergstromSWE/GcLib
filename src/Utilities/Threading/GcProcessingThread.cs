using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace GcLib.Utilities.Threading;

/// <summary>
/// Thread taking buffers out of a datastream in order to process (or display) them. The thread can be used to offload an image grabbing thread from processing or displaying related work. 
/// The thread contains an internal ring buffer for storing buffers to be processed.
/// </summary>
/// <remarks>
/// Register delegates to the events of the class in order to receive notifications. 
/// The most common use requires registration to the <see cref="BufferProcess"/> event and handling the supplied buffer in an eventhandler. 
/// </remarks>
public sealed class GcProcessingThread : IDisposable
{
    #region Fields

    // backing-fields
    private double _targetFPS;
    private bool _limitFPS;

    /// <summary>
    /// Processing thread.
    /// </summary>
    private Thread _processingThread;

    /// <summary>
    /// Stream of buffers.
    /// </summary>
    private IBufferStream _dataStream;

    /// <summary>
    /// True if object has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Handle used for blocking processing thread while waiting for new buffers to be queued.
    /// </summary>
    private readonly AutoResetEvent _waitHandle = new(false);

    /// <summary>
    /// Ring buffer, containing buffers queued for processing.
    /// </summary>
    /// ToDo: Use own implementation of circular buffer (in GcLib.Utilities.Collections namespace)?
    private readonly Cyotek.Collections.Generic.CircularBuffer<GcBuffer> _imageQueue;

    /// <summary>
    /// Frame rate manager, used for stabilizing the rate of buffers processed.
    /// </summary>
    private readonly FPSStabilizer _fpsStabilizer = new();

    #endregion

    #region Properties

    /// <summary>
    /// True if the processing thread is running. 
    /// </summary>
    public bool IsRunning { get; private set; } = false;

    /// <summary>
    /// Thread priority.
    /// </summary>
    public ThreadPriority Priority { get; set; } = ThreadPriority.Normal;

    /// <summary>
    /// Number of buffers currently queued for processing.
    /// </summary>
    public int QueuedCount => _imageQueue.Size;

    /// <summary>
    /// If true, buffers will be silently dropped to limit the number of buffers handled per second. Use <see cref="TargetFPS"/> to set frame rate limit.
    /// </summary>
    public bool LimitFPS
    {
        get => _limitFPS;
        set
        {
            _limitFPS = value;
            _fpsStabilizer.Reset();
        }
    }

    /// <summary>
    /// Targeted frame rate (buffers per seconds).
    /// </summary>
    public double TargetFPS
    {
        get => _targetFPS;
        set
        {
            _targetFPS = value;
            _fpsStabilizer.Reset();
        }
    }

    /// <summary>
    /// Returns the effective buffers per seconds processed.
    /// </summary>
    public double FPS => _fpsStabilizer.Average;

    /// <summary>
    /// String identifier of thread.
    /// </summary>
    public string ID { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new thread taking buffers out of a datastream in order to process (or display) them. 
    /// </summary>
    /// <param name="bufferCapacity">Size of internal ring buffer.</param>
    /// <param name="limitFPS">Limit rate of buffers handled.</param>
    /// <param name="targetFPS">Limiting rate of buffers handled per second.</param>
    /// <param name="ID">Thread ID.</param>
    public GcProcessingThread(int bufferCapacity = 4, bool limitFPS = false, double targetFPS = 30.0, string ID = "")
    {
        _imageQueue = new Cyotek.Collections.Generic.CircularBuffer<GcBuffer>(capacity: bufferCapacity, allowOverwrite: true);
        this.ID = ID;
        LimitFPS = limitFPS;
        TargetFPS = targetFPS;
    }

    #endregion

    #region Events

    /// <summary>
    /// Event announcing that an image buffer is ready for processing (or displaying), where the image buffer is supplied with the event.
    /// </summary>
    public event EventHandler<GcBuffer> BufferProcess;

    /// <summary>
    /// Event-invoking method announcing that an image buffer is ready for processing (or displaying), where the image buffer is supplied with the event.
    /// </summary>
    /// <param name="buffer">Buffer to be processed (or displayed).</param>
    private void OnBufferProcess(GcBuffer buffer)
    {
        BufferProcess?.Invoke(this, buffer);
    }

    /// <summary>
    /// Event announcing that the internal ring buffer was full when adding a new buffer to the queue.
    /// </summary>
    public event EventHandler BufferOverFlow;

    /// <summary>
    /// Event-invoking method announcing that the internal ring buffer was full when adding a new buffer to the queue.
    /// </summary>
    private void OnBufferOverFlow()
    {
        BufferOverFlow?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Starts the processing thread, grabbing buffers from a specified datastream.
    /// </summary>
    /// <param name="dataStream">The datastream supplying image buffers.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start(IBufferStream dataStream)
    {
        if (IsRunning)
            throw new InvalidOperationException("Thread is already started!");

        _dataStream = dataStream;

        // Initialize thread.
        _processingThread = new Thread(ThreadProc) { Priority = Priority };

        // Register to events in datastream.
        _dataStream.BufferTransferred += OnBufferTransferred;

        // Start thread.
        IsRunning = true;
        _processingThread.Start();
    }

    /// <summary>
    /// Stops the processing thread.
    /// </summary>
    /// <param name="aWait">(Optional) If true, waits for the thread to terminate. If false you can use WaitComplete.</param>
    public void Stop(bool aWait = false)
    {
        if (!IsRunning)
            return;

        // Unregister from events.
        _dataStream.BufferTransferred -= OnBufferTransferred;

        // Stop thread.
        IsRunning = false;

        // Stop thread immediately (without waiting for buffer).
        _ = _waitHandle.Set();

        if (aWait)
            _processingThread.Join();

        // Discard remaining buffers.
        _imageQueue.Clear();

        // Reset frame rate manager.
        _fpsStabilizer.Reset();
    }

    /// <summary>
    /// Waits for the processing thread to terminate.
    /// </summary>
    public void WaitComplete()
    {
        _processingThread.Join();
    }

    /// <summary>
    /// Set capacity of internal ring buffer.
    /// </summary>
    /// <param name="bufferCapacity">Total number of buffers that can be stored.</param>
    public void SetBufferCapacity(int bufferCapacity)
    {
        _imageQueue.Capacity = bufferCapacity;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Make sure thread is stopped.
        if (IsRunning)
            Stop();

        // Dispose of unmanaged resources.
        Dispose(true);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cleans up managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing"></param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed state (managed objects).                
            _imageQueue.Clear();
            _fpsStabilizer?.Reset();
        }

        // Free unmanaged resources (unmanaged objects) and override a finalizer below.
        // Set large fields to null.
        _waitHandle?.Dispose();

        _disposed = true;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Waits for images to be queued and raises events as they are ready for handling.
    /// </summary>
    private void ThreadProc()
    {
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Processingthread started (ID: {ID})", _dataStream.StreamID);

        while (IsRunning)
        {
            // Block thread until signaled that image has been queued. 
            if (_imageQueue.IsEmpty)
                _ = _waitHandle.WaitOne();

            // Retrieve image from queue and raise event that it needs to be handled.
            if (_imageQueue.IsEmpty == false)
            {
                // Check if frame rate is limited.
                if (LimitFPS)
                {
                    // Check if its time to process image.
                    if (_fpsStabilizer.IsTimeToDisplay(TargetFPS))
                    {
                        // Announce buffer.
                        OnBufferProcess(_imageQueue.Get());
                    }

                    // Drop remaining buffers in queue.
                    _imageQueue.Clear();

                    // Continue buffer acquisition.
                    continue;
                }
                else // unlimited frame rate
                {
                    // Announce buffer.
                    OnBufferProcess(_imageQueue.Get());
                }
            }
        }

        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Processingthread stopped (ID: {ID})", _dataStream.StreamID);
    }

    /// <summary>
    /// Event-handling method to events raised when a buffer has been transferred from a datastream.
    /// </summary>
    private void OnBufferTransferred(object sender, BufferTransferredEventArgs e)
    {

        if (_imageQueue.Size == _imageQueue.Capacity)
        {
            // Raise event that buffers will be overwritten.
            OnBufferOverFlow();
        }

        // Queue buffer.
        _imageQueue.Put(e.Buffer);

        // Unblock thread.
        _ = _waitHandle.Set();
    }

    #endregion
}