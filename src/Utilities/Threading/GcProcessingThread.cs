using System;
using System.Threading;
using Cyotek.Collections.Generic;
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
    private volatile bool _isRunning;

    /// <summary>
    /// Processing thread.
    /// </summary>
    private Thread _processingThread;

    /// <summary>
    /// Thread priority.
    /// </summary>
    private readonly ThreadPriority _priority;

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
    private readonly CircularBuffer<GcBuffer> _imageQueue;

    /// <summary>
    /// Synchronization lock for queue access.
    /// </summary>
    private readonly Lock _queueLock = new();

    /// <summary>
    /// Frame rate manager, used for stabilizing the rate of buffers processed.
    /// </summary>
    private readonly FPSStabilizer _fpsStabilizer = new();

    #endregion

    #region Properties

    /// <summary>
    /// True if the processing thread is running. 
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Number of buffers currently queued for processing.
    /// </summary>
    public int QueuedCount
    {
        get
        {
            lock (_queueLock)
                return _imageQueue.Size;
        }
    }

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
    /// <param name="priority">Thread priority.</param>
    /// <param name="ID">Thread ID.</param>
    public GcProcessingThread(int bufferCapacity = 4, bool limitFPS = false, double targetFPS = 30.0, ThreadPriority priority = ThreadPriority.Normal, string ID = "")
    {
        _imageQueue = new CircularBuffer<GcBuffer>(capacity: bufferCapacity, allowOverwrite: true);
        _priority = priority;
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
        // Validate arguments and state.
        ObjectDisposedException.ThrowIf(_disposed, nameof(GcProcessingThread));
        ArgumentNullException.ThrowIfNull(dataStream);

        if (IsRunning)
            throw new InvalidOperationException("Thread is already started!");

        _dataStream = dataStream;

        // Initialize thread.
        _processingThread = new Thread(ThreadProc) { Priority = _priority, IsBackground = true };

        // Register to events in datastream.
        _dataStream.BufferTransferred += OnBufferTransferred;

        // Start thread.
        _isRunning = true;
        _processingThread.Start();
    }

    /// <summary>
    /// Stops the processing thread.
    /// </summary>
    /// <param name="aWait">(Optional) If true, waits for the thread to terminate. If false you can use WaitComplete.</param>
    public void Stop(bool aWait = false)
    {
        if (IsRunning == false)
            return;

        // Unregister from events.
        _dataStream?.BufferTransferred -= OnBufferTransferred;

        // Stop thread.
        _isRunning = false;

        // Stop thread immediately (without waiting for buffer).
        _ = _waitHandle.Set();

        // Wait for thread.
        if (aWait && _processingThread is not null && (ReferenceEquals(Thread.CurrentThread, _processingThread) == false))
            _processingThread.Join();

        // Discard remaining buffers.
        lock (_queueLock)
            _imageQueue.Clear();

        // Reset frame rate manager.
        _fpsStabilizer.Reset();
    }

    /// <summary>
    /// Waits for the processing thread to terminate.
    /// </summary>
    public void WaitComplete()
    {
        if (_processingThread is null)
            return;

        // Wait for thread to terminate, unless it is already terminated or the current thread is the processing thread.
        if (_processingThread.IsAlive == false || ReferenceEquals(Thread.CurrentThread, _processingThread))
            return;

        _processingThread.Join();
    }

    /// <summary>
    /// Set capacity of internal ring buffer.
    /// </summary>
    /// <param name="bufferCapacity">Total number of buffers that can be stored.</param>
    public void SetBufferCapacity(int bufferCapacity)
    {
        lock (_queueLock)
            _imageQueue.Capacity = bufferCapacity;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Make sure thread is stopped.
        if (IsRunning)
            Stop(aWait: true);
        else WaitComplete();

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
            lock (_queueLock)
                _imageQueue.Clear();

            _fpsStabilizer.Reset();
            _waitHandle.Dispose();
        }

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
            GcBuffer buffer = null;

            // Get next buffer from queue.
            lock (_queueLock)
            {
                if (_imageQueue.IsEmpty == false)
                    buffer = _imageQueue.Get();
            }

            // If no buffer is available, wait for a new buffer to be queued.
            if (buffer is null)
            {
                _ = _waitHandle.WaitOne(); 
                continue;
            }

            // If limiting FPS, check if it is time to process the buffer. If not, skip processing and clear the queue.
            if (LimitFPS)
            {
                if (_fpsStabilizer.IsTimeToDisplay(TargetFPS))
                {
                    try
                    {
                        OnBufferProcess(buffer);
                    }
                    catch (Exception ex)
                    {
                        GcLibrary.Logger.LogError(ex, "Unhandled exception while processing buffer in processing thread {ID}", _dataStream.StreamID);
                    }
                }

                _imageQueue.Clear(); // Clear queue to avoid processing old buffers.
                continue;
            }

            // Process buffer.
            try
            {
                OnBufferProcess(buffer);
            }
            catch (Exception ex)
            {
                GcLibrary.Logger.LogError(ex, "Unhandled exception while processing buffer in processing thread {ID}", _dataStream.StreamID);
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
        // If the processing thread is not running, ignore the event.
        if (_disposed || IsRunning == false)
            return;

        bool overflow; // Flag indicating that the internal ring buffer was full when adding the new buffer to the queue.

        // Queue buffer for processing.
        lock (_queueLock)
        {
            overflow = _imageQueue.Size == _imageQueue.Capacity; // Check if adding the buffer will cause an overflow.
            _imageQueue.Put(e.Buffer);
        }

        // Raise event if adding buffer caused an overflow of ring buffer.
        if (overflow)
            OnBufferOverFlow();

        // Unblock thread.
        try
        {
            _ = _waitHandle.Set();
        }
        catch (ObjectDisposedException)
        {
            // Ignore late event callback during disposal.
        }
    }

    #endregion
}