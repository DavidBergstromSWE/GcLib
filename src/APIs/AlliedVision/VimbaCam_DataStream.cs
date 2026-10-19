using System;
using System.Runtime.InteropServices;
using VmbNET;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;

namespace GcLib;

public sealed partial class VimbaCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    private IAcquisition _acquisition;

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        _camera.FrameReceived += OnFrameReceived;

        _acquisition = _camera.StartFrameAcquisition();

        IsAcquiring = true;
        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        _acquisition.Stop();

        _acquisition.Dispose();

        _camera.FrameReceived -= OnFrameReceived;
        _acquisition = null;
        IsAcquiring = false;
        OnAcquisitionStopped();
    }

    private void OnFrameReceived(object sender, FrameReceivedEventArgs eventArgs)
    {
        try
        {
            using var frame = eventArgs.Frame;
            if (frame.FrameStatus != IFrame.FrameStatusValue.Completed)
                throw new InvalidOperationException($"Frame {frame.Id} was not completed successfully! Frame status: {frame.FrameStatus}");

            var pixelFormat = (PixelFormat)(int)frame.PixelFormat;
            if (Enum.IsDefined(pixelFormat) == false)
                throw new NotSupportedException($"Pixel format {frame.PixelFormat} is not supported!");

            var imageData = new byte[(int)frame.BufferSize];
            Marshal.Copy(frame.ImageData, imageData, 0, imageData.Length);

            var buffer = new GcBuffer(imageData,
                                      frame.Width,
                                      frame.Height,
                                      pixelFormat,
                                      GenICamHelper.GetPixelDynamicRangeMax(pixelFormat),
                                      (long)frame.Id,
                                      frame.Timestamp);

            OnNewBuffer(new NewBufferEventArgs(buffer, DateTime.Now));
        }
        catch (Exception ex)
        {
            OnFailedBuffer();
            if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);
        }
    }
}
