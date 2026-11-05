using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cyotek.Collections.Generic;
using GcLib.Utilities.Numbers;

namespace GcLib;

/// <summary>
/// Defines how buffers are delivered from datastream to consumers.
/// </summary>
public enum AcquisitionDeliveryMode
{
    /// <summary>
    /// Buffers are enqueued to output buffer queue and can be retrieved through <see cref="GcDataStream.RetrieveImage(out GcBuffer, int)"/>.
    /// </summary>
    Queued,

    /// <summary>
    /// Buffers are delivered using <see cref="GcDataStream.BufferTransferred"/> from a non-blocking dispatcher.
    /// </summary>
    EventDrivenNonBlocking
}

/// <summary>
/// Represents the datastream level in the GenICam/GenTL standard module hierarchy, providing an acquisition engine and being responsible for transferring images from device to image data consumers. 
/// </summary>
/// <param name="bufferProducer">Buffer producer.</param>
/// <param name="dataStreamID">Unique datastream ID.</param>
/// <param name="outputQueueCapacity">Maximum size of output buffer queue in <see cref="AcquisitionDeliveryMode.Queued"/> mode.</param>
/// <param name="deliveryMode">Delivery mode of transferred image buffers.</param>
public sealed class GcDataStream(IBufferProducer bufferProducer, string dataStreamID, int outputQueueCapacity = 4, AcquisitionDeliveryMode deliveryMode = AcquisitionDeliveryMode.Queued) : IBufferStream
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
    /// Output buffer queue, containing images successfully transferred from device input buffer pool and available to GenTL consumer.
    /// </summary>
    private readonly CircularBuffer<GcBuffer> _outputBufferQueue = new(capacity: outputQueueCapacity, allowOverwrite: true);

    /// <summary>
    /// Provides a synchronization event used to block and release a waiting thread.
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
    /// Event announcing that an image buffer has been successfully transferred from the camera to the datastream, where the image buffer is supplied with the event.
    /// </summary>
    public event EventHandler<BufferTransferredEventArgs> BufferTransferred;

    /// <summary>
    /// Event-invoking method announcing that an image buffer has been successfully transferred from the camera to the datastream, where the image buffer is supplied with the event.
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    private void OnBufferTransferred(GcBuffer buffer)
    {
        BufferTransferred?.Invoke(this, new BufferTransferredEventArgs(buffer));
    }

    /// <summary>
    /// Event announcing that a new buffer is added to the output buffer queue, indicating to consumers that data is available for retrieval and further processing. Use <see cref="GcDataStream.RetrieveImage(out GcBuffer, int)"/> to retrieve the buffer from the queue.
    /// </summary>
    public event EventHandler NewBuffer;

    /// <summary>
    /// Raises the <see cref="NewBuffer"/> event.
    /// </summary>
    private void OnNewBuffer()
    {
        NewBuffer?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that acquisition in the datastream has been stopped.
    /// </summary>
    public event EventHandler StreamingStopped;

    /// <summary>
    /// Event-invoking method announcing that acquisition in the datastream has been stopped.
    /// </summary>
    private void OnStreamingStopped()
    {
        StreamingStopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that acquisition in the datastream has been started.
    /// </summary>
    public event EventHandler StreamingStarted;

    /// <summary>
    /// Event-invoking method announcing that acquisition in the datastream has been started.
    /// </summary>
    private void OnStreamingStarted()
    {
        StreamingStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that an image frame has been dropped (lost) during acquisition, where the total (aggregated) number of frames dropped from the datastream is provided with the event.
    /// </summary>
    public event EventHandler<FrameDroppedEventArgs> FrameDropped;

    /// <summary>
    /// Event-invoking method announcing that an image frame has been dropped (lost) during acquisition.
    /// </summary>
    private void OnFrameDropped()
    {
        FrameDropped?.Invoke(this, new FrameDroppedEventArgs(LostFrameCount));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Closes datastream by stopping any active acquisition and resetting streaming statistics.
    /// </summary>
    public void Close()
    {
        // Stop active acquisition and unregister from NewBuffer events.
        if (IsStreaming)
            Stop();

        _bufferSinkRegistration?.Dispose();
        _bufferSinkRegistration = null;

        // Dispose of wait handle.
        _waitHandle?.Dispose();

        // Flush output buffer queue.
        ClearOutputBufferQueue();

        // Flush input buffer pool.
        ClearInputBufferPool();

        // Reset streaming statistics.
        ResetStatistics();

        IsOpen = false;
    }

    /// <summary>
    /// Retrieves image buffer from beginning of output buffer queue.
    /// </summary>
    /// <param name="buffer">Image buffer (or null if none is available).</param>
    /// <param name="timeout">Timeout in milliseconds to wait for a buffer.</param>
    /// <returns>True if buffer retrieval was successful, false if not.</returns>
    public bool RetrieveImage(out GcBuffer buffer, int timeout = 1000)
    {
        buffer = null;

        if (_deliveryMode != AcquisitionDeliveryMode.Queued)
            return false;

        _waitHandle.WaitOne(timeout);

        // Dequeue buffer.
        if (_outputBufferQueue.IsEmpty == false)
            buffer = _outputBufferQueue.Get();

        return buffer != null;
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

        ResetStatistics();

        // Flush queue.
        ClearOutputBufferQueue();

        // Reset input pool and pre-warm with payload-sized buffers.
        ClearInputBufferPool();
        PreWarmInputBufferPool();

        bool eventDispatchInitialized = false;

        try
        {
            if (_deliveryMode == AcquisitionDeliveryMode.EventDrivenNonBlocking)
            {
                InitializeEventDispatchPipeline();
                eventDispatchInitialized = true;
            }

            // Number of images to acquire.
            _imagesToAcquire = numImages;

            // Register stream-bound sink callbacks in producer.
            _bufferSinkRegistration = _device.RegisterBufferSink(StreamID, OnNewBuffer, OnFailedBuffer);

            _waitHandle.Reset();

            // Mark as streaming before starting acquisition so producer-side stream lookups can resolve this stream during startup callbacks.
            IsStreaming = true;

            // Start acquisition in camera.
            _device.StartAcquisition();

            // Raise event.
            OnStreamingStarted();
        }
        catch
        {
            // Roll back partially initialized start state.
            _bufferSinkRegistration?.Dispose();
            _bufferSinkRegistration = null;
            IsStreaming = false;

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

        // Stop acquisition in device.
        _device.StopAcquisition();

        // Unregister stream-bound sink callbacks.
        _bufferSinkRegistration?.Dispose();
        _bufferSinkRegistration = null;

        if (_deliveryMode == AcquisitionDeliveryMode.EventDrivenNonBlocking)
            StopEventDispatchPipeline();

        IsStreaming = false;

        // Raise event.
        OnStreamingStopped();
    }

    /// <summary>
    /// Rents a reusable image buffer with at least <paramref name="minimumLength"/> bytes from the input buffer pool.
    /// </summary>
    /// <param name="minimumLength">Minimum required number of bytes.</param>
    /// <returns>Reusable image byte buffer.</returns>
    internal byte[] RentInputBuffer(int minimumLength)
    {
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
            if (_inputBufferPool.Count < _inputBufferPool.Capacity)
                _inputBufferPool.Enqueue(buffer);
        }
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
    /// Clears output queue and returns queued image buffers to the input buffer pool.
    /// </summary>
    private void ClearOutputBufferQueue()
    {
        while (_outputBufferQueue.IsEmpty == false)
            ReturnInputBuffer(_outputBufferQueue.Get()?.ImageData);
    }

    /// <summary>
    /// Clears input buffer pool.
    /// </summary>
    private void ClearInputBufferPool()
    {
        lock (_inputBufferPoolLock)
            _inputBufferPool.Clear();
    }

    /// <summary>
    /// Pre-allocates payload-sized buffers in the input buffer pool.
    /// </summary>
    private void PreWarmInputBufferPool()
    {
        int payloadSize = (int)_device.PayloadSize;
        if (payloadSize <= 0)
            return;

        lock (_inputBufferPoolLock)
        {
            for (int i = 0; i < _device.BufferCapacity; i++)
                _inputBufferPool.Enqueue(new byte[payloadSize]);
        }
    }

    /// <summary>
    /// Initializes the non-blocking event-dispatch pipeline.
    /// </summary>
    private void InitializeEventDispatchPipeline()
    {
        if (_deliveryMode != AcquisitionDeliveryMode.EventDrivenNonBlocking)
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

        // Start the background task for dispatching buffers to event subscribers.
        _eventDispatchCancellationTokenSource = new CancellationTokenSource();
        _eventDispatchTask = Task.Run(() => DispatchBuffersAsync(_eventDispatchCancellationTokenSource.Token));
    }

    /// <summary>
    /// Stops and disposes non-blocking event-dispatch pipeline.
    /// </summary>
    private void StopEventDispatchPipeline()
    {
        _eventDispatchCancellationTokenSource?.Cancel();

        _eventDispatchChannel?.Writer.TryComplete();

        try
        {
            _eventDispatchTask?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerException is OperationCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }

        _eventDispatchCancellationTokenSource?.Dispose();

        _eventDispatchTask = null;
        _eventDispatchCancellationTokenSource = null;
        _eventDispatchChannel = null;
    }

    /// <summary>
    /// Tries to enqueue a buffer for asynchronous event dispatch.
    /// </summary>
    /// <param name="buffer">Buffer to dispatch.</param>
    /// <returns>True if buffer was accepted for dispatch, otherwise false.</returns>
    private bool TryPublishBufferEventDriven(GcBuffer buffer)
    {
        if (_eventDispatchChannel == null)
            return false;

        // Try to write the buffer to the channel without blocking.
        bool accepted = _eventDispatchChannel.Writer.TryWrite(buffer);

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
    private async Task DispatchBuffersAsync(CancellationToken cancellationToken)
    {
        if (_eventDispatchChannel == null)
            return;

        // Get the reader for the channel.
        ChannelReader<GcBuffer> reader = _eventDispatchChannel.Reader;

        // Continuously read buffers from the channel and invoke the BufferTransferred event.
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out GcBuffer buffer))
            {
                try
                {
                    OnBufferTransferred(buffer);
                }
                catch
                {
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
    /// Event-handling method to <see cref="GcDevice.NewBuffer"/> events raised in device, delivering buffers based on configured delivery mode and signaling a <see cref="BufferTransferred"/> event.
    /// </summary>
    private void OnNewBuffer(object sender, NewBufferEventArgs e)
    {
        // Ignore buffers from other datastreams.
        if (string.IsNullOrEmpty(e.StreamID) == false && string.Equals(e.StreamID, StreamID, StringComparison.Ordinal) == false)
            return;

        if (e.CopyImageData == null || e.PayloadSize <= 0)
            return;

        // Rent destination image data and copy payload from producer event.
        byte[] imageData = RentInputBuffer(e.PayloadSize);
        e.CopyImageData(imageData);

        // Create stream-owned image buffer container.
        var buffer = new GcBuffer(imageData,
                                  e.Width,
                                  e.Height,
                                  e.PixelFormat,
                                  e.FrameID,
                                  e.TimeStamp);

        // Update datastream stats.
        UpdateStatistics(buffer.FrameID, buffer.TimeStamp);

        if (_deliveryMode == AcquisitionDeliveryMode.Queued)
        {
            // If queue is full, recycle the oldest queued buffer before enqueueing this one.
            if (_outputBufferQueue.Size == _outputBufferQueue.Capacity)
            {
                var recycledBuffer = _outputBufferQueue.Get();
                ReturnInputBuffer(recycledBuffer?.ImageData);
            }

            // Add image buffer to output buffer queue.
            _outputBufferQueue.Put(buffer);

            // Signal waiting threads that a buffer is available.
            _waitHandle.Set();

            // Raise new buffer event.
            OnNewBuffer();
        }
        else
        {
            TryPublishBufferEventDriven(buffer);
        }

        // Automatically stop acquisition when frame count target condition is met.
        if (DeliveredFrameCount == _imagesToAcquire)
            Stop();
    }

    /// <summary>
    /// Event-handling method to <see cref="GcDevice.FailedBuffer"/> events, incrementing the number of failed buffer.
    /// </summary>
    private void OnFailedBuffer(object sender, EventArgs e)
    {
        FailedFrameCount++;
    }

    #endregion
}