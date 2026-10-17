using System;
using System.Threading;
using Basler.Pylon;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;

namespace GcLib;

public sealed partial class PylonCam : GcDevice, IDeviceEnumerator
{
    #region Fields

    /// <summary>
    /// The capacity of the buffer.
    /// </summary>
    private uint _bufferCapacity = 10;

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
                using var grabResult = _camera.StreamGrabber.RetrieveResult(3000, TimeoutHandling.ThrowException);

                if (grabResult == null)
                    throw new InvalidOperationException("Failed to retrieve grab result!");

                if (grabResult.GrabSucceeded == false)
                    throw new InvalidOperationException($"Failed to grab image: {grabResult.ErrorDescription}");

                // Raise new buffer event.
                OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(grabResult), DateTime.Now));

                // Stop acquisition immediately in SingleFrame or MultiFrame acquisition modes.
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
    /// Converts grab result to <see cref="GcBuffer"/>.
    /// </summary>
    /// <param name="grabResult">Grab result.</param>
    /// <returns>Converted <see cref="GcBuffer"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when the payload type or pixel type is not supported.</exception>
    private static GcBuffer ToGcBuffer(IGrabResult grabResult)
    {
        // Check if the payload type is an image.
        if (grabResult.PayloadTypeValue != PayloadType.Image)
            throw new NotSupportedException($"Payload type {grabResult.PayloadTypeValue} is not supported!");

        // Convert pixel type to GenICam pixel format.
        var pixelFormat = (PixelFormat)(int)grabResult.PixelTypeValue;
        if (pixelFormat == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Pixel type {grabResult.PixelTypeValue} is not supported!");

        // Copy pixel data to a new byte array.
        byte[] imageData = new byte[grabResult.PayloadSize];
        Buffer.BlockCopy(grabResult.PixelData as byte[], 0, imageData, 0, (int)grabResult.PayloadSize);

        // Alternative method using Marshal.Copy:
        //Marshal.Copy(grabResult.PixelDataPointer, imageData, 0, (int)grabResult.PayloadSize);

        // Use the timestamp from the grab result if available, otherwise use the current PC time.
        var timeStamp = grabResult.Timestamp == 0 ? (ulong)DateTime.Now.Ticks : (ulong)grabResult.Timestamp;

        return new GcBuffer(imageData, (uint)grabResult.Width, (uint)grabResult.Height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), grabResult.ImageNumber, timeStamp);
    }

    #endregion
}
