using System;
using System.Collections.Generic;
using System.Threading;
using Cyotek.Collections.Generic;
using GcLib.Utilities.Numbers;

namespace GcLib;

/// <summary>
/// Represents the datastream level in the GenICam/GenTL standard module hierarchy, providing an acquisition engine and being responsible for transferring images from device to image data consumers. 
/// </summary>
/// <param name="bufferProducer">Buffer producer.</param>
/// <param name="dataStreamID">Unique datastream ID.</param>
/// <param name="outputQueueCapacity">Maximum size of output buffer queue.</param>
public sealed class GcDataStream(IBufferProducer bufferProducer, string dataStreamID, int outputQueueCapacity = 4) : IBufferStream
{
    #region Fields

    /// <summary>
    /// Device connected to datastream.
    /// </summary>
    private readonly IBufferProducer _device = bufferProducer;

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

    #endregion

    #region Properties

    /// <summary>
    /// Unique ID of datastream.
    /// </summary>
    public string StreamID { get; } = dataStreamID;

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
        _waitHandle.Dispose();

        // Stop active acquisition and unregister from NewBuffer events.
        if (IsStreaming)
            Stop();

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
    /// Images can be grabbed from datastream by subscribing to <see cref="BufferTransferred"/> event (preferred) or by grabbing them from output buffer queue using <see cref="RetrieveImage(out GcBuffer)"/>.
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

        // Number of images to acquire.
        _imagesToAcquire = numImages;

        // Register to events.
        _device.NewBuffer += OnNewBuffer;
        _device.FailedBuffer += OnFailedBuffer;

        _waitHandle.Reset();

        // Start acquisition in camera.
        _device.StartAcquisition();

        IsStreaming = true;

        // Raise event.
        OnStreamingStarted();
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

        // Unregister events.
        _device.NewBuffer -= OnNewBuffer;
        _device.FailedBuffer -= OnFailedBuffer;

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
        if (_device is not GcDevice device)
            return;

        int payloadSize = (int)device.PayloadSize;
        if (payloadSize <= 0)
            return;

        lock (_inputBufferPoolLock)
        {
            for (int i = 0; i < device.BufferCapacity; i++)
                _inputBufferPool.Enqueue(new byte[payloadSize]);
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
    /// Event-handling method to <see cref="GcDevice.NewBuffer"/> events raised in device, adding buffer supplied with event to output buffer queue and signaling a <see cref="BufferTransferred"/> event.
    /// </summary>
    private void OnNewBuffer(object sender, NewBufferEventArgs e)
    {
        // Retrieve image data from event argument.
        GcBuffer buffer = e.Buffer;

        if (buffer == null)
            return;

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

        // Update datastream stats.
        UpdateStatistics(buffer.FrameID, buffer.TimeStamp);

        // Raise event.
        OnBufferTransferred(buffer);

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