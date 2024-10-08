using System;
using System.Threading;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace GcLib;

public sealed partial class SpinCam : GcDevice, IDeviceEnumerator
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
    /// PC time when acquisition is started (given in PC ticks, where a single tick represents one hundred nanoseconds or one ten-millionth of a second).
    /// </summary>
    private ulong _pcTime0;

    /// <summary>
    /// Camera timestamp when acquisition is started (in nanoseconds).
    /// </summary>
    private ulong _acquisitionStartTime = 0;

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

        // PC time at acquisition start.
        _pcTime0 = (ulong)DateTime.Now.Ticks;

        // Timestamp of camera at acquisition start.
        _camera.TimestampLatch.Execute();
        _acquisitionStartTime = (ulong)_camera.TimestampLatchValue.Value;

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
        GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", _camera.TLDevice.DeviceModelName, _camera.TLDevice.DeviceSerialNumber);

        while (_threadIsRunning)
        {
            try
            {
                var buffer = _camera.GetNextImage(3000);

                if (buffer != null && buffer.ImageStatus != ImageStatus.IMAGE_NO_ERROR && buffer.IsIncomplete)
                    throw new SpinnakerException($"Failed to grab image", Error.SPINNAKER_ERR_INVALID_BUFFER);

                // Raise new buffer event.
                OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(buffer), DateTime.Now));

                // Release buffer to acquire next one.
                buffer.Release();
            }
            catch (SpinnakerException ex)
            {
                // Log debugging info.
                GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", _camera.TLDevice.DeviceModelName, _camera.TLDevice.DeviceSerialNumber);
            }
        }

        // Log debugging info.
        GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", _camera.TLDevice.DeviceModelName, _camera.TLDevice.DeviceSerialNumber);
    }

    /// <summary>
    /// Converts image buffer to <see cref="GcBuffer"/>.
    /// </summary>
    /// <param name="image">Image buffer.</param>
    /// <returns>Converted <see cref="GcBuffer"/>.</returns>
    private GcBuffer ToGcBuffer(IManagedImage image)
    {
        // Extract timestamp from image and convert to PC ticks.
        ulong timeStamp = _pcTime0 + (ulong)Math.Round((image.TimeStamp - (double)_acquisitionStartTime) / 100);

        // Parse pixel format.
        PixelFormat pixelFormat = (PixelFormat)Enum.Parse(typeof(PixelFormat), image.PixelFormat.ToString());

        return new GcBuffer(image.ManagedData, image.Width, image.Height, pixelFormat, GenICamConverter.GetDynamicRangeMax(pixelFormat), (long)image.FrameID, timeStamp);
    }

    #endregion
}