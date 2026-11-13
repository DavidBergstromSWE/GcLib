using System;
using System.Threading;
using Basler.Pylon;
using Microsoft.Extensions.Logging;

namespace GcLib;

public sealed partial class PylonCam
{
    #region Fields

    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private Thread _imageAcquisitionThread;

    /// <summary>
    /// Image acquisition thread stopping condition.
    /// </summary>
    private bool _threadIsRunning;

    /// <summary>
    /// Default camera timestamp tick frequency (ticks per second), used when the camera does not report one (timestamps in nanoseconds).
    /// </summary>
    private const double DefaultTickFrequency = 1e9;

    /// <summary>
    /// Camera timestamp of the first image in the current acquisition (in camera ticks), or -1 if no image has been received yet.
    /// </summary>
    private long _cameraTime0 = -1;

    /// <summary>
    /// PC time when the first image in the current acquisition was received (in PC ticks, where a single tick represents one hundred nanoseconds).
    /// </summary>
    private long _pcTime0;

    /// <summary>
    /// Camera timestamp tick frequency (ticks per second).
    /// </summary>
    private double _tickFrequency = DefaultTickFrequency;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override uint PayloadSize => (uint)_camera.Parameters[PLCamera.PayloadSize].GetValue();

    /// <inheritdoc/>
    public override uint BufferCapacity
    {
        get;
        set
        {
            field = Math.Max(value, 1);
            _camera.Parameters[PLStream.MaxNumBuffer].SetValue(field); // Set the maximum number of buffers in the stream grabber.
        }
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        // Reset timestamp synchronization (camera time is synchronized to PC time when the first image is received).
        _cameraTime0 = -1;
        _tickFrequency = GetTickFrequency();

        // Start acquisition on the device.
        _camera.StreamGrabber.Start(strategy: GrabStrategy.OneByOne, grabLoopType: GrabLoop.ProvidedByUser);

        IsAcquiring = true;

        // Start image acquisition thread.
        _imageAcquisitionThread = new Thread(ImageAcquisitionThread) { Name = "ImageAcquisitionThread (PylonCam)" };
        _threadIsRunning = true;
        _imageAcquisitionThread.Start();

        // Announce event.
        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        // Stop image acquisition thread.
        _threadIsRunning = false;

        // Wait for the image acquisition thread to finish.
        if (ReferenceEquals(Thread.CurrentThread, _imageAcquisitionThread) == false) // Avoid deadlock if StopAcquisition is called from the image acquisition thread.
            _imageAcquisitionThread.Join();

        // Stop acquisition on the device.
        _camera.StreamGrabber.Stop();

        IsAcquiring = false;

        // Announce event.
        OnAcquisitionStopped();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private void ImageAcquisitionThread()
    {
        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", DeviceInfo.ModelName, DeviceInfo.UniqueID);

        while (_threadIsRunning)
        {
            try
            {
                // Wait for a new buffer to be available.
                using var grabResult = _camera.StreamGrabber.RetrieveResult(3000, TimeoutHandling.ThrowException) ?? throw new InvalidOperationException("Failed to retrieve grab result!");
                if (grabResult.GrabSucceeded == false)
                    throw new InvalidOperationException($"Failed to grab image: {grabResult.ErrorDescription}");

                // Raise new buffer event.
                OnNewBuffer(ToNewBufferEventArgs(grabResult));

                // Stop acquisition if using SingleFrame or MultiFrame acquisition modes and the last image has been grabbed.
                if (_camera.Parameters.Contains(PLCamera.AcquisitionMode))
                {
                    var acquisitionMode = _camera.Parameters[PLCamera.AcquisitionMode].GetValue();
                    if (acquisitionMode == AcquisitionMode.SingleFrame.ToString() && grabResult.ImageNumber == 1
                        || acquisitionMode == AcquisitionMode.MultiFrame.ToString() && _camera.Parameters.Contains(PLCamera.AcquisitionFrameCount) && grabResult.ImageNumber == _camera.Parameters[PLCamera.AcquisitionFrameCount].GetValue())
                    {
                        StopAcquisition();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Announce failed buffer event.
                OnFailedBuffer();

                // Log debugging info.
                if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                    GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);

            }
        }

        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", DeviceInfo.ModelName, DeviceInfo.UniqueID);
    }

    /// <summary>
    /// Converts a grab result to stream-transfer event data.
    /// </summary>
    /// <param name="grabResult">Grab result.</param>
    /// <returns>Event data containing stream information and acquired buffer.</returns>
    /// <exception cref="NotSupportedException">Thrown when the payload type or pixel type is not supported.</exception>
    private NewBufferEventArgs ToNewBufferEventArgs(IGrabResult grabResult)
    {
        // Check if the payload type is an image.
        if (grabResult.PayloadTypeValue != PayloadType.Image)
            throw new NotSupportedException($"Payload type {grabResult.PayloadTypeValue} is not supported!");

        // Convert pixel type to GenICam pixel format.
        var pixelFormat = (PixelFormat)(int)grabResult.PixelTypeValue;
        if (pixelFormat == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Pixel type {grabResult.PixelTypeValue} is not supported!");

        // Copy pixel data to a new array to ensure that the buffer is owned by the GcBuffer instance and not tied to the lifetime of the grab result.
        var pixelData = (byte[])grabResult.PixelData;
        var ownedPixelData = new byte[pixelData.Length];
        Buffer.BlockCopy(pixelData, 0, ownedPixelData, 0, pixelData.Length);

        // Convert camera timestamp (in camera specific ticks) to PC ticks.
        var timeStamp = ToPcTicks(grabResult.Timestamp);

        var buffer = new GcBuffer(ownedPixelData,
                                  (uint)grabResult.Width,
                                  (uint)grabResult.Height,
                                  pixelFormat,
                                  grabResult.ImageNumber,
                                  timeStamp,
                                  (uint)grabResult.PaddingX,
                                  (uint)grabResult.PaddingY);

        return new NewBufferEventArgs(string.Empty,
                                      DateTime.Now,
                                      buffer);
    }

    /// <summary>
    /// Retrieves the tick frequency of the camera timestamp counter.
    /// </summary>
    /// <returns>Ticks per second (nanosecond resolution is assumed if the camera does not report a frequency).</returns>
    private double GetTickFrequency()
    {
        try
        {
            // Reported by GigE Vision cameras. Other transport layers (e.g., USB3 Vision) use nanosecond ticks.
            if (_camera.Parameters.Contains(PLCamera.GevTimestampTickFrequency) && _camera.Parameters[PLCamera.GevTimestampTickFrequency].IsReadable)
            {
                long frequency = _camera.Parameters[PLCamera.GevTimestampTickFrequency].GetValue();
                if (frequency > 0)
                    return frequency;
            }
        }
        catch (Exception ex)
        {
            if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                GcLibrary.Logger.LogWarning(ex, "Unable to read timestamp tick frequency in Device: {modelName} (ID: {uniqueID}), assuming nanosecond ticks", DeviceInfo.ModelName, DeviceInfo.UniqueID);
        }

        return DefaultTickFrequency;
    }

    /// <summary>
    /// Converts a camera timestamp to PC ticks, by synchronizing camera time with PC time when the first image in the acquisition is received.
    /// </summary>
    /// <param name="cameraTicks">Timestamp in camera specific ticks (0 if not available).</param>
    /// <returns>Timestamp in PC ticks (where a single tick represents one hundred nanoseconds).</returns>
    private ulong ToPcTicks(long cameraTicks)
    {
        // Use the current PC time if the camera does not provide a timestamp.
        if (cameraTicks <= 0)
            return (ulong)DateTime.Now.Ticks;

        // Synchronize camera time with PC time at first image.
        if (_cameraTime0 < 0)
        {
            _cameraTime0 = cameraTicks;
            _pcTime0 = DateTime.Now.Ticks;
        }

        long elapsedPcTicks = (long)Math.Round((cameraTicks - _cameraTime0) / _tickFrequency * TimeSpan.TicksPerSecond);
        return (ulong)Math.Max(_pcTime0 + elapsedPcTicks, 0);
    }

    #endregion
}
