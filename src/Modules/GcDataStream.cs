using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cyotek.Collections.Generic;
using GcLib.Utilities.Numbers;

namespace GcLib;

/// <summary>
/// Represents the datastream level in the GenICam/GenTL standard module hierarchy, providing an acquisition engine and being responsible for transferring images from device to image data consumers. 
/// </summary>
/// <remarks>Buffers can be transferred synchronously or asynchronously according to the configured <see cref="AcquisitionDeliveryMode"/>.
/// In synchronous mode, buffers are enqueued to an internal output buffer queue and can be retrieved by consumers through <see cref="RetrieveImage(out GcBuffer, int)"/>.
/// In asynchronous mode, buffers are delivered to consumers through <see cref="BufferTransferred"/> events using a non-blocking event dispatcher.
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
    /// Selected delivery mode.
    /// </summary>
    private readonly AcquisitionDeliveryMode _deliveryMode = deliveryMode;

    /// <summary>
    /// Output buffer queue, containing images successfully transferred from input buffer pool (and available to GenTL consumer).
    /// </summary>
    private readonly CircularBuffer<GcBuffer> _outputBufferQueue = new(capacity: outputQueueCapacity, allowOverwrite: true);

    /// <summary>
    /// Provides a synchronization event used to block and release waiting threads.
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
    /// Synchronization object for input buffer pool access.
    /// </summary>
    private readonly Lock _inputBufferPoolLock = new();

    /// <summary>
    /// Input buffer pool that device classes can reuse when filling new frame data.
    /// </summary>
    private readonly Queue<byte[]> _inputBufferPool = [];

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
    public int AwaitDeliveryCount => _outputBufferQueue.Size;

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
    /// Event announcing that a new buffer has been received from device and is available for retrieval. Use <see cref="RetrieveImage(out GcBuffer, int)"/> to retrieve the buffer.
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
    public event EventHandler<FrameDroppedEventArgs> FrameDropped;

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

        // Flush input buffer pool.
        FlushPool();

        // Reset streaming statistics.
        ResetStatistics();

        IsOpen = false;
    }

    /// <summary>
    /// Starts streaming images from device continuously until manually stopped. If <paramref name="numImages"/> is specified the acquisition stops automatically after the specified number of images have been acquired.
    /// </summary>
    /// <remarks>
    /// Images can be grabbed from datastream by subscribing to <see cref="BufferTransferred"/> event or by grabbing them from output buffer queue using <see cref="RetrieveImage(out GcBuffer)"/>.
    /// </remarks>
    /// <param name="numImages">Number of images to acquire.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start(ulong numImages = Constants.GENTL_INFINITE)
    {
        if (IsStreaming)
            throw new InvalidOperationException("Datastream is already streaming!");

        // Reset streaming statistics.
        ResetStatistics();

        // Reset output buffer queue and input buffer pool to ensure that no stale buffers are present before starting acquisition.
        FlushQueue();
        FlushPool();

        // Pre-allocate payload-sized buffers in input buffer pool, to ensure that the pool is ready to receive new buffers from device.
        QueueBuffers();

        bool eventDispatchInitialized = false; // Flag indicating whether the event dispatch pipeline has been initialized successfully.
        bool acquisitionStarted = false; // Flag indicating whether acquisition has been started successfully.

        try
        {
            // Initialize event dispatch pipeline if asynchronous delivery mode is selected.
            if (_deliveryMode == AcquisitionDeliveryMode.Asynchronous)
            {
                InitializeEventDispatchPipeline();
                eventDispatchInitialized = true; // Mark that event dispatch pipeline has been initialized successfully.
            }

            // Number of images to acquire.
            _imagesToAcquire = numImages;

            // Register datastream as a receiver of buffers from device, providing callbacks for new buffer and failed buffer events.
            _bufferSinkRegistration = _device.RegisterBufferSink(StreamID, OnDeviceNewBuffer, OnDeviceFailedBuffer);

            // Reset wait handle to ensure that any waiting threads are blocked until a new buffer is available.
            _waitHandle.Reset();

            // Mark as streaming before starting acquisition so producer-side stream lookups can resolve this stream during startup callbacks.
            IsStreaming = true;

            // Start producer acquisition for this stream.
            if (_device is IStreamAcquisitionCoordinator coordinator)
                coordinator.StartStreamAcquisition(StreamID); // Start acquisition for this stream only, allowing multiple streams to acquire concurrently.
            else _device.StartAcquisition(); 
            acquisitionStarted = true; // Mark that acquisition has been started successfully.

            // Raise event that streaming has started.
            OnStreamingStarted();
        }
        catch
        {
            try
            {
                // Release producer acquisition if startup completed before a later step failed.
                if (acquisitionStarted)
                {
                    if (_device is IStreamAcquisitionCoordinator coordinator)
                        coordinator.StopStreamAcquisition(StreamID);
                    else
                        _device.StopAcquisition();
                }
            }
            finally
            {
                // Roll back partially initialized start state.
                _bufferSinkRegistration?.Dispose();
                _bufferSinkRegistration = null;
                IsStreaming = false;

                // Stop event dispatch pipeline if it was initialized before exception occurred.
                if (eventDispatchInitialized)
                    StopEventDispatchPipeline();
            }

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

        // Stop producer acquisition for this stream.
        if (_device is IStreamAcquisitionCoordinator coordinator)
            coordinator.StopStreamAcquisition(StreamID);
        else
            _device.StopAcquisition();

        // Unregister stream-bound sink callbacks.
        _bufferSinkRegistration?.Dispose();
        _bufferSinkRegistration = null;

        // Stop event dispatch pipeline if asynchronous delivery mode is selected.
        if (_deliveryMode == AcquisitionDeliveryMode.Asynchronous)
            StopEventDispatchPipeline();

        IsStreaming = false;

        // Raise streaming stopped event.
        OnStreamingStopped();
    }

    /// <summary>
    /// Retrieves <paramref name="buffer"/> from output buffer queue. The method blocks until a buffer is available or until a specified <paramref name="timeout"/> has elapsed.
    /// </summary>
    /// <param name="buffer">Buffer retrieved (or <see cref="null"/> if none was available within the specified timeout).</param>
    /// <param name="timeout">Timeout in milliseconds to wait.</param>
    /// <returns>True if buffer retrieval was successful, false if not.</returns>
    public bool RetrieveImage(out GcBuffer buffer, int timeout = 1000)
    {
        // Reset output parameter to null.
        buffer = null;

        // Only applicable in queued delivery mode.
        if (_deliveryMode != AcquisitionDeliveryMode.Synchronous)
            return false;

        // Wait for a buffer to be available in the output buffer queue, with a specified timeout.
        _waitHandle.WaitOne(timeout);

        // Dequeue buffer.
        if (_outputBufferQueue.IsEmpty == false)
            buffer = _outputBufferQueue.Get();

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

    #region Internal Methods

    /// <summary>
    /// Rents a reusable image buffer with at least <paramref name="minimumLength"/> bytes from the input buffer pool.
    /// </summary>
    /// <param name="minimumLength">Minimum required number of bytes.</param>
    /// <returns>Reusable image byte buffer.</returns>
    internal byte[] RentInputBuffer(int minimumLength)
    {
        // ToDo: Implement bucketed size-class pooling?

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumLength);

        lock (_inputBufferPoolLock)
        {
            while (_inputBufferPool.Count > 0)
            {
                var candidate = _inputBufferPool.Dequeue();
                if (candidate.Length >= minimumLength)
                    return candidate;
            }
        }

        return new byte[minimumLength]; // No suitable buffer was available in the pool, so allocate a new one.
    }

    /// <summary>
    /// Returns an image buffer to the input buffer pool for reuse.
    /// </summary>
    /// <param name="buffer">Image data buffer.</param>
    internal void ReturnInputBuffer(byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
            return;

        lock (_inputBufferPoolLock)
        {
            if (_inputBufferPool.Count < _device.BufferCapacity)
                _inputBufferPool.Enqueue(buffer);
        }
    }

    #endregion

    #region Private Methods

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
    }

    /// <summary>
    /// Clears output buffer queue and returns queued buffers to the input buffer pool.
    /// </summary>
    private void FlushQueue()
    {
        while (_outputBufferQueue.IsEmpty == false)
            ReturnInputBuffer(_outputBufferQueue.Get()?.ImageData);
    }

    /// <summary>
    /// Clears input buffer pool.
    /// </summary>
    private void FlushPool()
    {
        lock (_inputBufferPoolLock)
            _inputBufferPool.Clear();
    }

    /// <summary>
    /// Pre-allocates payload-sized buffers in the input buffer pool.
    /// </summary>
    private void QueueBuffers()
    {
        // Retrieve payload size from device.
        int payloadSize = (int)_device.PayloadSize;
        if (payloadSize <= 0)
            return; // No payload size is available, so skip pre-warming. Throw new InvalidOperationException("Device has no payload size available!")?

        // Pre-allocate buffers and add them to the input buffer pool.
        for (int i = 0; i < _device.BufferCapacity; i++)
        {
            byte[] buffer = new byte[payloadSize];
            lock (_inputBufferPoolLock)
            {
                if (_inputBufferPool.Count < _device.BufferCapacity)
                    _inputBufferPool.Enqueue(buffer);
            }
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

        // If the buffer was not accepted (channel is full), increment lost frame count and return the buffer to the input pool.
        if (accepted == false)
        {
            LostFrameCount++;
            OnFrameDropped();
            ReturnInputBuffer(buffer.ImageData);
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
                finally
                {
                    // Return the buffer to the input pool after dispatching.
                    ReturnInputBuffer(buffer.ImageData);
                }
            }
        }
    }

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

        // Calculate actual framerate.
        FrameRate = (double)TimeSpan.TicksPerSecond / (timeStamp - _lastTimeStamp);

        // Calculate average framerate.
        FrameRateAverage += (FrameRate - FrameRateAverage) / (DeliveredFrameCount + LostFrameCount); // running average since acquisition start

        // Update timestamp of last grabbed image.
        _lastTimeStamp = timeStamp;
    }

    /// <summary>
    /// Event-handling method to <see cref="GcDevice.NewBuffer"/> events raised in device, delivering buffers based on configured delivery mode.
    /// </summary>
    private void OnDeviceNewBuffer(object sender, NewBufferEventArgs e)
    {
        // Ignore buffers from other datastreams.
        if (string.IsNullOrEmpty(e.StreamID) == false && string.Equals(e.StreamID, StreamID, StringComparison.Ordinal) == false)
            return;

        // Ignore buffers with no payload.
        if (e.CopyImageData == null || e.PayloadSize <= 0)
            return; // Throw new InvalidOperationException("New buffer event has no payload!")?

        // Rent buffer from input buffer pool and copy image payload from new buffer event.
        byte[] imageData = RentInputBuffer(e.PayloadSize);
        e.CopyImageData(imageData);

        // Create stream-owned image buffer container.
        var buffer = new GcBuffer(imageData: imageData, width: e.Width, height: e.Height, pixelFormat: e.PixelFormat, frameID: e.FrameID, timeStamp: e.TimeStamp);

        // Update datastream stats.
        UpdateStatistics(buffer.FrameID, buffer.TimeStamp);

        // Deliver buffer to consumers based on configured delivery mode.
        if (_deliveryMode == AcquisitionDeliveryMode.Synchronous) // synchronous delivery mode
        {
            // If queue is full, recycle the oldest queued buffer before enqueueing this one.
            if (_outputBufferQueue.Size == _outputBufferQueue.Capacity)
            {
                var recycledBuffer = _outputBufferQueue.Get();
                ReturnInputBuffer(recycledBuffer?.ImageData); // Return recycled buffer to input buffer pool.
            }

            // Add image buffer to output buffer queue.
            _outputBufferQueue.Put(buffer);

            // Signal waiting threads that a buffer is available.
            _waitHandle.Set();

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
        FrameDropped?.Invoke(this, new FrameDroppedEventArgs(LostFrameCount));
    }

    #endregion
}