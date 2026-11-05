using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace GcLib;

public sealed partial class SpinCam
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

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override uint PayloadSize
    {
        get
        {
            var val = _nodeMap.GetNode<Integer>("PayloadSize");
            return (uint)val.Value;
        }
    }

    /// <inheritdoc/>
    public override uint BufferCapacity { get; set; } = 1;

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        // Start image acquisition thread.
        _imageAcquisitionThread = new Thread(ImageAcquisitionThread) { Name = "ImageAcquisitionThread (SpinCam)" };
        _threadIsRunning = true;
        _imageAcquisitionThread.Start();

        _camera.BeginAcquisition();

        IsAcquiring = true;

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
        _imageAcquisitionThread.Join();

        // Stop acquisition on the device.
        _camera.EndAcquisition();

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
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", _camera.TLDevice.DeviceModelName, _camera.TLDevice.DeviceSerialNumber);

        while (_threadIsRunning)
        {
            try
            {
                var buffer = _camera.GetNextImage(3000);

                if (buffer != null && buffer.ImageStatus != ImageStatus.IMAGE_NO_ERROR && buffer.IsIncomplete)
                    throw new SpinnakerException($"Failed to grab image", Error.SPINNAKER_ERR_INVALID_BUFFER);

                // Raise new buffer event.
                OnNewBuffer(ToNewBufferEventArgs(buffer));

                // Release buffer to acquire next one.
                buffer.Release();
            }
            catch (SpinnakerException ex)
            {
                // Announce failed buffer event.
                OnFailedBuffer();

                // Log debugging info.
                if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                    GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", _camera.TLDevice.DeviceModelName, _camera.TLDevice.DeviceSerialNumber);
            }
        }

        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", _camera.TLDevice.DeviceModelName, _camera.TLDevice.DeviceSerialNumber);
    }

    /// <summary>
    /// Converts image buffer to stream-transfer event data.
    /// </summary>
    /// <param name="image">Image buffer.</param>
    /// <returns>Event data containing metadata and payload copy callback.</returns>
    private NewBufferEventArgs ToNewBufferEventArgs(IManagedImage image)
    {
        // Parse pixel format.
        PixelFormat pixelFormat = Enum.Parse<PixelFormat>(image.PixelFormat.ToString());
        byte[] sourceData = image.ManagedData;

        return new NewBufferEventArgs(string.Empty,
                                      DateTime.Now,
                                      image.Width,
                                      image.Height,
                                      pixelFormat,
                                      (long)image.FrameID,
                                      (ulong)DateTime.Now.Ticks,
                                      sourceData.Length,
                                      destination => Buffer.BlockCopy(sourceData, 0, destination, 0, sourceData.Length));
    }

    #endregion
}