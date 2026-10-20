using System;
using System.Runtime.InteropServices;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;
using VmbNET;

namespace GcLib;

public sealed partial class VimbaCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    /// <summary>
    /// Stores the acquisition service instance.
    /// </summary>
    private IAcquisition _acquisition;

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        // Subscribe to frame received event.
        _camera.FrameReceived += OnFrameReceived;

        // Start acquisition.
        _acquisition = _camera.StartFrameAcquisition();

        IsAcquiring = true;

        // Notify that acquisition has started.
        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        // Stop acquisition.
        _acquisition.Stop();

        // Dispose of the acquisition object.
        _acquisition.Dispose();

        // Unsubscribe from frame received event.
        _camera.FrameReceived -= OnFrameReceived;

        IsAcquiring = false;

        // Notify that acquisition has stopped.
        OnAcquisitionStopped();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Processes a received frame, validates completion status and pixel format, copies image data into a managed
    /// buffer, and raises a new-buffer notification.
    /// </summary>
    /// <remarks>If processing fails, signals a failed-buffer notification and logs a warning.</remarks>
    /// <param name="sender">Source of the frame-received event.</param>
    /// <param name="eventArgs">Event data containing the received frame.</param>
    private void OnFrameReceived(object sender, FrameReceivedEventArgs eventArgs)
    {
        try
        {
            // Get the received frame data.
            using var frame = eventArgs.Frame; // Ensure proper disposal of the frame after processing (so that frame can be filled with new data).

            // Validate frame completion status.
            if (frame.FrameStatus != IFrame.FrameStatusValue.Completed)
                throw new InvalidOperationException($"Frame {frame.Id} was not completed successfully! Frame status: {frame.FrameStatus}");

            // Validate pixel format.
            var pixelFormat = (PixelFormat)(int)frame.PixelFormat;
            if (Enum.IsDefined(pixelFormat) == false)
                throw new NotSupportedException($"Pixel format {frame.PixelFormat} is not supported!");

            // Copy image data from unmanaged memory to managed byte array.
            var imageData = new byte[(int)frame.BufferSize];
            Marshal.Copy(frame.ImageData, imageData, 0, imageData.Length);

            // Create a GcBuffer with the copied image data and frame metadata.
            var buffer = new GcBuffer(imageData,
                                      frame.Width,
                                      frame.Height,
                                      pixelFormat,
                                      GenICamHelper.GetPixelDynamicRangeMax(pixelFormat),
                                      (long)frame.Id,
                                      (ulong)DateTime.Now.Ticks);

            OnNewBuffer(new NewBufferEventArgs(buffer, DateTime.Now));
        }
        catch (Exception ex)
        {
            // Signal that buffer processing failed.
            OnFailedBuffer();

            if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);
        }
    }

    #endregion
}
