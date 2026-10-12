using System;
using Basler.Pylon;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;

namespace GcLib;

public sealed partial class PylonCam : GcDevice, IDeviceEnumerator
{
    #region Fields

    private uint _bufferCapacity = 10;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override uint PayloadSize => (uint)_camera.Parameters[PLCamera.PayloadSize].GetValue();

    /// <inheritdoc/>
    public override uint BufferCapacity
    {
        get => _bufferCapacity;
        set
        {
            if (IsAcquiring)
                throw new InvalidOperationException("Unable to change buffer capacity while acquiring!");

            _bufferCapacity = Math.Max(value, 1);
            _camera.Parameters[PLStream.MaxNumBuffer].SetValue(_bufferCapacity);
        }
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        _camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
        _camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);

        IsAcquiring = true;

        // Announce event.
        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        _camera.StreamGrabber.Stop();
        _camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;

        IsAcquiring = false;

        // Announce event.
        OnAcquisitionStopped();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Handles grab results delivered by the stream grabber thread.
    /// </summary>
    private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
    {
        using var grabResult = e.GrabResult;

        try
        {
            if (grabResult.GrabSucceeded == false)
            {
                if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                    GcLibrary.Logger.LogWarning("Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID}): {error}", DeviceInfo.ModelName, DeviceInfo.UniqueID, grabResult.ErrorDescription);

                OnFailedBuffer();
                return;
            }

            OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(grabResult), DateTime.Now));
        }
        catch (Exception ex)
        {
            if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);

            OnFailedBuffer();
        }
    }

    /// <summary>
    /// Converts grab result to <see cref="GcBuffer"/>.
    /// </summary>
    /// <param name="grabResult">Grab result.</param>
    /// <returns>Converted <see cref="GcBuffer"/>.</returns>
    private static GcBuffer ToGcBuffer(IGrabResult grabResult)
    {
        var pixelFormat = ToPixelFormat(grabResult.PixelTypeValue);
        if (pixelFormat == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Pixel type {grabResult.PixelTypeValue} is not supported!");

        // Pixel data is owned by the grab result, so copy it before the result is released.
        var data = (byte[])((byte[])grabResult.PixelData).Clone();

        return new GcBuffer(data, (uint)grabResult.Width, (uint)grabResult.Height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), grabResult.ImageNumber, (ulong)DateTime.Now.Ticks);
    }

    /// <summary>
    /// Converts pylon pixel type to <see cref="PixelFormat"/>.
    /// </summary>
    private static PixelFormat ToPixelFormat(PixelType pixelType)
    {
        var name = pixelType.ToString() switch
        {
            "RGB8packed" => "RGB8",
            "BGR8packed" => "BGR8",
            var other => other
        };

        return Enum.TryParse(name, ignoreCase: true, out PixelFormat pixelFormat) ? pixelFormat : PixelFormat.InvalidPixelFormat;
    }

    #endregion
}
