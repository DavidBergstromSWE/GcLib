using System;

namespace GcLib;

/// <summary>
/// Represents the datastream level in the GenICam/GenTL standard module hierarchy, providing an acquisition engine and being responsible for transferring images from device to image data consumers. 
/// </summary>
/// <param name="bufferProducer">Buffer producer.</param>
/// <param name="dataStreamID">Unique datastream ID.</param>
/// <param name="bufferCapacity">Maximum size of ring buffer.</param>
public sealed class GcDataStream(IBufferProducer bufferProducer, string dataStreamID, int bufferCapacity = 4) : IBufferStream
{
    #region Fields

    /// <summary>
    /// Device connected to datastream.
    /// </summary>
    private readonly IBufferProducer _device = bufferProducer;

    /// <summary>
    /// Output buffer queue, containing images transferred from device input buffer pool and available to GenTL consumer.
    /// ToDo: Use own implementation for a circular buffer instead?
    /// </summary>
    private readonly Cyotek.Collections.Generic.CircularBuffer<GcBuffer> _outputBufferQueue = new(capacity: bufferCapacity, allowOverwrite: true);

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
    /// Size capacity of datastream output buffer queue.
    /// </summary>
    public uint OutputBufferQueueCapacity
    {
        get => (uint)_outputBufferQueue.Capacity;
        set => _outputBufferQueue.Capacity = (int)value;
    }

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
        // Stop active acquisition and unregister from NewBuffer events.
        if (IsStreaming)
            Stop();

        // Flush output buffer queue.      
        _outputBufferQueue.Clear();

        // Reset streaming statistics.
        ResetStatistics();

        IsOpen = false;
    }

    /// <summary>
    /// Retrieves image buffer from beginning of output buffer queue.
    /// </summary>
    /// <param name="buffer">Image buffer (or null if none is available).</param>
    /// <returns>True if buffer retrieval was successful, false if not.</returns>
    public bool RetrieveImage(out GcBuffer buffer)
    {
        buffer = null;

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
        _outputBufferQueue.Clear();

        // Number of images to acquire.
        _imagesToAcquire = numImages;

        // Register to events.
        _device.NewBuffer += OnNewBuffer;
        _device.FailedBuffer += OnFailedBuffer;

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

        // Add image buffer to output buffer queue.
        _outputBufferQueue.Put(buffer);

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