using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cyotek.Collections.Generic;
using GcLib.Utilities;
using GcLib.Utilities.Numbers;

namespace GcLib;

/// <summary>
/// Represents the datastream level in the GenICam/GenTL standard module hierarchy, responsible for transferring buffers from device to consumers. 
/// </summary>
/// <remarks>Buffers can be transferred synchronously or asynchronously according to the configured <see cref="AcquisitionDeliveryMode"/>:
/// <list type="bullet">
/// <item>
/// <description>Synchronous: Buffers are signaled by the <see cref="NewBuffer"/> event and enqueued to an internal output buffer queue. They can be retrieved by consumers through <see cref="RetrieveBuffer"/>.</description>
/// </item>
/// <item>
/// <description>Asynchronous: Buffers are delivered to consumers through <see cref="BufferTransferred"/> events, using a non-blocking event dispatcher.</description>
/// </item>
/// </list>
/// </remarks>
/// <param name="bufferProducer">Buffer producer.</param>
/// <param name="dataStreamID">Unique datastream ID.</param>
/// <param name="outputQueueCapacity">Maximum size of output buffer queue in <see cref="AcquisitionDeliveryMode.Synchronous"/> mode.</param>
/// <param name="deliveryMode">Delivery mode of transferred image buffers.</param>
public sealed class GcDataStream(IBufferProducer bufferProducer, string dataStreamID, int outputQueueCapacity = 4, AcquisitionDeliveryMode deliveryMode = AcquisitionDeliveryMode.Synchronous) : IBufferStream
{
    #region Fields

    /// <summary>
    /// Device connected to datastream.
    /// </summary>
    private readonly IBufferProducer _device = bufferProducer;

    /// <summary>
    /// Optional stream coordinator for coordinated start/stop across concurrent datastreams.
    /// </summary>
    private readonly IStreamCoordinator _streamCoordinator = bufferProducer as IStreamCoordinator;

    /// <summary>
    /// Selected delivery mode.
    /// </summary>
    private readonly AcquisitionDeliveryMode _deliveryMode = deliveryMode;

    /// <summary>
    /// Output buffer queue, containing buffers available to synchronous consumers.
    /// </summary>
    private readonly CircularBuffer<GcBuffer> _outputBufferQueue = new(capacity: outputQueueCapacity, allowOverwrite: true);

    /// <summary>
    /// Synchronization lock for output buffer queue access.
    /// </summary>
    private readonly Lock _outputQueueLock = new();

    /// <summary>
    /// Provides a synchronization event used to block and release threads waiting for a buffer to be available in the output buffer queue.
    /// </summary>
    private readonly AutoResetEvent _waitHandle = new(false);

    /// <summary>
    /// Index of last grabbed image (used for counting dropped images).
    /// </summary>
    private long _lastFrameID;

    /// <summary>
    /// Timestamp of last grabbed image (in ticks).
    /// </summary>
    private ulong _lastTimeStamp;

    /// <summary>
    /// Number of images to acquire.
    /// </summary>
    private ulong _imagesToAcquire;

    /// <summary>
    /// Buffer channel used in non-blocking event dispatch mode.
    /// </summary>
    private Channel<GcBuffer> _eventDispatchChannel;

    /// <summary>
    /// Cancellation token source for non-blocking event dispatch loop.
    /// </summary>
    private CancellationTokenSource _eventDispatchCancellationTokenSource;

    /// <summary>
    /// Background task dispatching transferred buffers to event subscribers.
    /// </summary>
    private Task _eventDispatchTask;

    /// <summary>
    /// Registration handle for stream-bound sink callbacks in producer.
    /// </summary>
    private IDisposable _bufferSinkRegistration;

    #endregion

    #region Properties

    /// <summary>
    /// Unique ID of datastream.
    /// </summary>
    public string StreamID { get; } = dataStreamID;

    /// <summary>
    /// Configured delivery mode.
    /// </summary>
    public AcquisitionDeliveryMode DeliveryMode => _deliveryMode;

    /// <summary>
    /// Number of buffers waiting in the output buffer queue.
    /// </summary>
    public int AwaitDeliveryCount
    {
        get
        {
            lock (_outputQueueLock)
                return _outputBufferQueue.Size;
        }
    }

    /// <summary>
    /// Number of delivered frames since last acquisition start.
    /// </summary>
    public ulong DeliveredFrameCount { get; private set; } = 0;

    /// <summary>
    /// Number of lost frames due to queue underrun.
    /// </summary>
    public ulong LostFrameCount { get; private set; } = 0;

    /// <summary>
    /// Number of failed buffers during acquisition due to errors.
    /// </summary>
    public ulong FailedFrameCount { get; private set; } = 0;

    /// <summary>
    /// Actual frame rate (in frames per second).
    /// </summary>
    public double FrameRate { get; private set; } = 0;

    /// <summary>
    /// Average frame rate (in frames per second), calculated as a running average from start of acquisition.
    /// </summary>
    public double FrameRateAverage { get; private set; } = 0;

    /// <summary>
    /// True if the acquisition engine has started and streaming is active, false if not.
    /// </summary>
    public bool IsStreaming { get; private set; } = false;

    /// <summary>
    /// True if datastream is currently open, false if not.
    /// </summary>
    public bool IsOpen { get; private set; } = true;

    #endregion

    #region Events

    /// <summary>
    /// Event announcing that an image buffer has been successfully transferred from device to datastream, where the image buffer is supplied with the event.
    /// </summary>
    /// <remarks>This event will only be raised in <see cref="AcquisitionDeliveryMode.Asynchronous"/> delivery mode.</remarks>
    public event EventHandler<BufferTransferredEventArgs> BufferTransferred;

    /// <summary>
    /// Event announcing that a new buffer has been received from device and is available for retrieval. Use <see cref="RetrieveBuffer(out GcBuffer, int)"/> to retrieve the buffer.
    /// </summary>
    /// <remarks>This event will only be raised in <see cref="AcquisitionDeliveryMode.Synchronous"/> delivery mode.</remarks>
    public event EventHandler NewBuffer;

    /// <summary>
    /// Event announcing that acquisition in the datastream has been stopped.
    /// </summary>
    public event EventHandler StreamingStopped;

    /// <summary>
    /// Event announcing that acquisition in the datastream has been started.
    /// </summary>
    public event EventHandler StreamingStarted;

    /// <summary>
    /// Event announcing that an image frame has been dropped (lost) during acquisition, where the total (aggregated) number of frames dropped from the datastream is provided with the event.
    /// </summary>
    public event EventHandler<BufferDroppedEventArgs> BufferDropped;

    #endregion

    #region Public Methods

    /// <summary>
    /// Closes datastream by stopping any active acquisition and resetting streaming statistics.
    /// </summary>
    public void Close()
    {
        // Stop acquisition if it is currently active.
        if (IsStreaming)
            Stop();

        // Unregister stream-bound sink callbacks.
        _bufferSinkRegistration?.Dispose();
        _bufferSinkRegistration = null;

        // Dispose of wait handle.
        _waitHandle?.Dispose();

        // Flush output buffer queue.
        FlushQueue();

        // Reset streaming statistics.
        ResetStatistics();

        IsOpen = false;
    }

    /// <summary>
    /// Starts streaming images from device continuously until manually stopped. If <paramref name="numImages"/> is specified the acquisition stops automatically after the specified number of images have been acquired.
    /// </summary>
    /// <remarks>
    /// Images can be grabbed from datastream by subscribing to <see cref="BufferTransferred"/> event or by grabbing them from output buffer queue using <see cref="RetrieveBuffer(out GcBuffer, int)"/>.
    /// </remarks>
    /// <param name="numImages">Number of images to acquire.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start(ulong numImages = Constants.GENTL_INFINITE)
    {
        if (IsOpen == false)
            throw new InvalidOperationException("Datastream is closed!");

        if (IsStreaming)
            throw new InvalidOperationException("Datastream is already streaming!");

        // Reset streaming statistics.
        ResetStatistics();

        // Reset output buffer queue to ensure that no stale buffers are present before starting acquisition.
        FlushQueue();

        // Flag indicating whether the event dispatch pipeline has been initialized successfully.
        bool eventDispatchInitialized = false;

        // Flag indicating whether stream acquisition reservation/start succeeded and requires rollback on failure.
        bool streamAcquisitionStarted = false;

        try
        {
            // Initialize event dispatch pipeline if asynchronous delivery mode is selected.
            if (_deliveryMode == AcquisitionDeliveryMode.Asynchronous)
            {
                InitializeEventDispatchPipeline();
                eventDispatchInitialized = true;
            }

            // Number of images to acquire.
            _imagesToAcquire = numImages;

            // Register datastream as a receiver of buffers from device, providing callbacks for new buffer and failed buffer events.
            _bufferSinkRegistration = _device.RegisterBufferSink(StreamID, OnDeviceNewBuffer, OnDeviceFailedBuffer);

            // Reset wait handle to ensure that any waiting threads are blocked until a new buffer is available.
            _waitHandle.Reset();

            // Mark as streaming before starting acquisition so producer-side stream lookups can resolve this stream during startup callbacks.
            IsStreaming = true;

            // Start acquisition coordinated with the producer, if supported.
            if (_streamCoordinator != null)
                _streamCoordinator.StartStream(StreamID);
            else
                _device.StartAcquisition();

            streamAcquisitionStarted = true;

            // Raise event that streaming has started.
            OnStreamingStarted();
        }
        catch
        {
            // If acquisition was started successfully, attempt to stop it to roll back to a clean state. Any exceptions during this rollback are ignored to avoid masking the original exception.
            if (streamAcquisitionStarted)
            {
                try
                {
                    if (_streamCoordinator != null)
                        _streamCoordinator.StopStream(StreamID);
                    else
                        _device.StopAcquisition();
                }
                catch
                {
                }
            }

            // Roll back partially initialized start state.
            _bufferSinkRegistration?.Dispose();
            _bufferSinkRegistration = null;
            IsStreaming = false;

            // Stop event dispatch pipeline if it was initialized before exception occurred.
            if (eventDispatchInitialized)
                StopEventDispatchPipeline();

            throw;
        }
    }

    /// <summary>
    /// Stops streaming images from camera.
    /// </summary>
    public void Stop()
    {
        if (IsStreaming == false)
            return;

        try
        {
            // Stop acquisition in device, coordinated across active datastreams if supported.
            if (_streamCoordinator != null)
                _streamCoordinator.StopStream(StreamID);
            else
                _device.StopAcquisition();
        }
        finally
        {
            // Unregister stream-bound sink callbacks.
            _bufferSinkRegistration?.Dispose();
            _bufferSinkRegistration = null;

            // Stop event dispatch pipeline if asynchronous delivery mode is selected.
            if (_deliveryMode == AcquisitionDeliveryMode.Asynchronous)
                StopEventDispatchPipeline();

            IsStreaming = false;
        }

        // Raise streaming stopped event.
        OnStreamingStopped();
    }

    /// <summary>
    /// Retrieves <paramref name="buffer"/> from output buffer queue. The method blocks until a buffer is available or until a specified <paramref name="timeout"/> has elapsed.
    /// </summary>
    /// <param name="buffer">Buffer retrieved (or <see cref="null"/> if none was available within the specified timeout).</param>
    /// <param name="timeout">Timeout in milliseconds to wait.</param>
    /// <returns>True if buffer retrieval was successful, false if not.</returns>
    public bool RetrieveBuffer(out GcBuffer buffer, int timeout = 1000)
    {
        // Reset output parameter to null.
        buffer = null;

        // Only applicable in queued delivery mode.
        if (_deliveryMode != AcquisitionDeliveryMode.Synchronous)
            return false;

        // Fast path: retrieve immediately if queue already contains data.
        lock (_outputQueueLock)
        {
            if (_outputBufferQueue.IsEmpty == false)
            {
                buffer = _outputBufferQueue.Get();
                return true;
            }
        }

        // Wait for a buffer to be available in the output buffer queue, with a specified timeout.
        _waitHandle.WaitOne(timeout);

        lock (_outputQueueLock)
        {
            if (_outputBufferQueue.IsEmpty == false)
                buffer = _outputBufferQueue.Get();
        }

        return buffer != null;
    }

    /// <summary>
    /// Retrieves a handle to the parent device of the datastream.
    /// </summary>
    /// <returns>Handle to the parent device.</returns>
    public GcDevice GetParentDevice()
    {
        return _device is GcDevice device ? device : throw new InvalidOperationException("Object is not a device.");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates streaming statistics, including number of delivered frames, lost frames and actual/average framerates.
    /// </summary>
    /// <param name="frameID">Frame ID of image.</param>
    /// <param name="timeStamp">Timestamp of image (in ticks).</param>
    private void UpdateStatistics(long frameID, ulong timeStamp)
    {
        // Increment number of successfully grabbed frames.
        DeliveredFrameCount++;

        // Check for dropped (unaccounted) frames.
        if (frameID > _lastFrameID + 1)
        {
            LostFrameCount += (ulong)(frameID - _lastFrameID - 1);
            OnFrameDropped();
        }

        // Update frame ID of last grabbed image.
        _lastFrameID = frameID;

        // Calculate actual/average framerate only when timestamp delta is valid.
        if (timeStamp > _lastTimeStamp)
        {
            ulong deltaTicks = timeStamp - _lastTimeStamp;
            FrameRate = FrameRateCalculator.Calculate(deltaTicks, 1, TimeSpan.TicksPerSecond);
            FrameRateAverage += (FrameRate - FrameRateAverage) / (DeliveredFrameCount + LostFrameCount); // running average since acquisition start
        }

        // Update timestamp of last grabbed image.
        _lastTimeStamp = timeStamp;
    }

    /// <summary>
    /// Reset streaming statistics.
    /// </summary>
    private void ResetStatistics()
    {
        DeliveredFrameCount = 0;
        LostFrameCount = 0;
        FrameRate = 0;
        FrameRateAverage = 0;
        FailedFrameCount = 0;
        _lastFrameID = 0;
        _lastTimeStamp = 0;
    }

    /// <summary>
    /// Clears output buffer queue.
    /// </summary>
    private void FlushQueue()
    {
        lock (_outputQueueLock)
        {
            while (_outputBufferQueue.IsEmpty == false)
                _outputBufferQueue.Get();
        }
    }

    /// <summary>
    /// Initializes the non-blocking event-dispatch pipeline.
    /// </summary>
    private void InitializeEventDispatchPipeline()
    {
        if (_deliveryMode != AcquisitionDeliveryMode.Asynchronous)
            return;

        // Create a bounded channel for event dispatching.
        int capacity = Math.Max(1, _outputBufferQueue.Capacity); // Use the same capacity as the output buffer queue.
        _eventDispatchChannel = Channel.CreateBounded<GcBuffer>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait, // Block when full to avoid dropping buffers.
            SingleReader = true,                    // Single reader for dispatching buffers.
            SingleWriter = false,                   // Multiple writers can enqueue buffers from different threads.
            AllowSynchronousContinuations = false   // Prevent synchronous continuations to avoid potential deadlocks.
        });

        // Start the background task for dispatching buffers to event subscribers, with cancellation support.
        _eventDispatchCancellationTokenSource = new CancellationTokenSource();
        _eventDispatchTask = Task.Run(() => DispatchBuffersAsync(_eventDispatchCancellationTokenSource.Token));
    }

    /// <summary>
    /// Stops and disposes non-blocking event-dispatch pipeline. 
    /// </summary>
    private void StopEventDispatchPipeline()
    {
        // Cancel the dispatch task and signal that no more buffers will be queued.
        _eventDispatchCancellationTokenSource?.Cancel();
        _eventDispatchChannel?.Writer.TryComplete();

        try
        {
            // Wait for the dispatch task to complete, handling cancellation exceptions gracefully.
            _eventDispatchTask?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerException is OperationCanceledException)
        {
            // Ignore OperationCanceledException, which is expected when the task is canceled. Only rethrow if cancellation was not requested.
            if (_eventDispatchCancellationTokenSource?.IsCancellationRequested == false)
                throw;
        }
        catch (OperationCanceledException)
        {
        }

        // Dispose of the cancellation token source and reset the dispatch task and channel references.
        _eventDispatchCancellationTokenSource?.Dispose();
        _eventDispatchTask = null;
        _eventDispatchCancellationTokenSource = null;
        _eventDispatchChannel = null;
    }

    /// <summary>
    /// Tries to queue a buffer for asynchronous event dispatch.
    /// </summary>
    /// <param name="buffer">Buffer to dispatch.</param>
    /// <returns>True if buffer was accepted for dispatch, otherwise false.</returns>
    private bool TryPublishBufferEventDriven(GcBuffer buffer)
    {
        if (_eventDispatchChannel == null)
            return false;

        // Try to write the buffer to the channel without blocking.
        bool accepted = _eventDispatchChannel.Writer.TryWrite(buffer); // If the channel is full, this will return false.

        // If the buffer was not accepted (channel is full), increment lost frame count.
        if (accepted == false)
        {
            LostFrameCount++;
            OnFrameDropped();
        }

        return accepted;
    }

    /// <summary>
    /// Dispatches buffers asynchronously to event subscribers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="ChannelClosedException">Thrown if the channel is closed before all buffers are dispatched.</exception>
    private async Task DispatchBuffersAsync(CancellationToken cancellationToken)
    {
        // If the event dispatch channel is not initialized, exit the method.
        if (_eventDispatchChannel == null)
            return;

        // Get the reader for the channel.
        ChannelReader<GcBuffer> reader = _eventDispatchChannel.Reader;

        // Continuously read buffers from the channel and invoke the BufferTransferred event.
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Read all available buffers from the channel.
            while (reader.TryRead(out GcBuffer buffer))
            {
                try
                {
                    // Invoke the BufferTransferred event for the buffer.
                    OnBufferTransferred(buffer);
                }
                catch
                {
                    // Subscribers may throw exceptions; log or handle them as needed. For now, we just ignore them to ensure the dispatch loop continues.
                }
            }
        }
    }

    /// <summary>
    /// Event-handling method to <see cref="GcDevice.NewBuffer"/> events raised in device, delivering buffers based on configured delivery mode.
    /// </summary>
    private void OnDeviceNewBuffer(object sender, NewBufferEventArgs e)
    {
        // Ignore late callbacks when stream is closed/stopped.
        if (IsOpen == false || IsStreaming == false)
            return;

        // Ignore buffers from other datastreams.
        if (string.IsNullOrEmpty(e.StreamID) == false && string.Equals(e.StreamID, StreamID, StringComparison.Ordinal) == false)
            return;

        // Ignore buffers with no payload.
        if (e.Buffer?.ImageData == null || e.Buffer.ImageData.Length <= 0)
            return; // Throw new InvalidOperationException("New buffer event has no payload!")?

        var buffer = e.Buffer;

        // Update datastream stats.
        UpdateStatistics(buffer.FrameID, buffer.TimeStamp);

        // Deliver buffer to consumers based on configured delivery mode.
        if (_deliveryMode == AcquisitionDeliveryMode.Synchronous) // synchronous delivery mode
        {
            lock (_outputQueueLock)
            {
                // If queue is full, recycle the oldest queued buffer before enqueueing this one.
                if (_outputBufferQueue.Size == _outputBufferQueue.Capacity)
                {
                    _outputBufferQueue.Get();
                    LostFrameCount++;
                    OnFrameDropped();
                }

                // Add image buffer to output buffer queue.
                _outputBufferQueue.Put(buffer);
            }

            // Signal waiting threads that a buffer is available.
            try
            {
                _waitHandle.Set();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            // Raise new buffer event.
            OnNewBuffer();
        }
        else // asynchronous delivery mode
        {
            // Try to publish buffer to event dispatch channel.
            TryPublishBufferEventDriven(buffer);
        }

        // Automatically stop acquisition when frame count target condition is met.
        if (DeliveredFrameCount == _imagesToAcquire)
            Stop();
    }

    /// <summary>
    /// Event-handling method to <see cref="GcDevice.FailedBuffer"/> events, incrementing the number of failed buffer.
    /// </summary>
    private void OnDeviceFailedBuffer(object sender, EventArgs e)
    {
        if (IsOpen == false || IsStreaming == false)
            return;

        FailedFrameCount++;
    }

    /// <summary>
    /// Event-invoking method announcing that an image buffer has been successfully transferred from device to datastream, where the image buffer is supplied with the event.
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    private void OnBufferTransferred(GcBuffer buffer)
    {
        BufferTransferred?.Invoke(this, new BufferTransferredEventArgs(buffer));
    }

    /// <summary>
    /// Event-invoking method announcing to consumers that a new buffer is available for retrieval and further processing.
    /// </summary>
    private void OnNewBuffer()
    {
        NewBuffer?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event-invoking method announcing that acquisition in the datastream has been stopped.
    /// </summary>
    private void OnStreamingStopped()
    {
        StreamingStopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event-invoking method announcing that acquisition in the datastream has been started.
    /// </summary>
    private void OnStreamingStarted()
    {
        StreamingStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event-invoking method announcing that an image frame has been dropped (lost) during acquisition.
    /// </summary>
    private void OnFrameDropped()
    {
        BufferDropped?.Invoke(this, new BufferDroppedEventArgs(LostFrameCount));
    }

    #endregion
}