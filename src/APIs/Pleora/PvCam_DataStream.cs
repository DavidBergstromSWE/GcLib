using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using PvDotNet;

namespace GcLib;

/// <summary>
/// Vendor-specific camera class providing an interface to eBUS SDK from Pleora Technologies.
/// </summary>
public sealed partial class PvCam
{
    #region Fields

    // timestamp-related fields

    /// <summary>
    /// Camera timestamp when acquisition is started.
    /// </summary>
    private ulong _acquisitionStartTime = 0;

    /// <summary>
    /// PC time when acquisition is started (given in PC ticks, where a single tick represents one hundred nanoseconds or one ten-millionth of a second).
    /// </summary>
    private ulong _pcTime0;

    /// <summary>
    /// Camera tick frequency (64-bit number indicating the number of timestamp ticks in 1 second). (Deprecated)
    /// </summary>
    private double _tickFrequency = 0;

    // streaming related fields

    /// <summary>
    /// Input buffer pool size (in PvStream object).
    /// </summary>
    private uint _bufferCapacity;

    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private Thread _imageAcquisitionThread;

    /// <summary>
    /// Image acquisition thread stopping signal.
    /// </summary>
    private bool _threadIsRunning;


    #endregion

    #region Properties

    /// <inheritdoc/>
    public override uint PayloadSize => _pvDevice.PayloadSize;

    /// <inheritdoc/>
    public override uint BufferCapacity
    {
        get => _bufferCapacity;
        set
        {
            // Check maximum number of possible buffers.
            _bufferCapacity = (_pvStream.QueuedBufferMaximum < value) ? _pvStream.QueuedBufferMaximum : value;
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Start acquiring buffers.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        // Test
        try
        {
            // Reapply setting.
            if (Parameters.IsImplemented("SyncMode"))
                Parameters.SetParameterValue("SyncMode", Parameters["SyncMode"].ToString());
            //Parameters.Update();
        }
        catch (Exception) { }

        // Reset pipeline.
        _pvPipeline.BufferCount = BufferCapacity;
        _pvPipeline.BufferSize = _pvDevice.PayloadSize;
        _pvPipeline.Reset();

        // PC time at acquisition start.
        _pcTime0 = (ulong)DateTime.Now.Ticks;

        // Get camera internal timings at acquisition start.
        (_acquisitionStartTime, _tickFrequency) = GetTimestampAndTickFrequency();

        // Start image acquisition thread.
        _imageAcquisitionThread = new Thread(ImageAcquisitionThread) { Name = "ImageAcquisitionThread (PvCam)" };
        _threadIsRunning = true;
        _imageAcquisitionThread.Start();

        // Start acquisition on the device.
        _pvAcquisitionStateManager.Start();

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

        try
        {
            // Stop acquisition on the device.
            _pvAcquisitionStateManager.Stop();
        }
        catch (PvException ex)
        {
            // Can be raised if acquisition is stopped when connection was lost.
            if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                GcLibrary.Logger.LogError(ex, "Failed to stop acquisition in {ModelName} (ID: {ID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);
        }

        IsAcquiring = false;

        // Announce event.
        OnAcquisitionStopped();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Image acquisition thread. Attempts to retrieve buffers from PvStream and signals a new buffer event if successfull.
    /// </summary>
    private void ImageAcquisitionThread()
    {
        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", _pvDeviceInfo.ModelName, _pvDeviceInfo.UniqueID);

        uint nBufferError = 0; // Counts the number of consecutive buffer errors.
        uint bufferErrorLimit = 10; // Number of consecutive buffer errors limit (before aborting).

        PvBuffer pvBuffer = null;

        while (_threadIsRunning)
        {
            try
            {
                // Retrieve buffer from pipeline.
                PvResult pvResult = _pvPipeline.RetrieveNextBuffer(aBuffer: ref pvBuffer, aTimeout: 3000);
                if (pvResult.IsOK)
                {
                    if (pvBuffer.OperationResult.IsOK)
                    {
                        // Reset error counting.
                        nBufferError = 0;

                        // Raise new buffer event.
                        OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(pvBuffer), DateTime.Now));
                    }
                    else throw new PvException(pvBuffer.OperationResult);

                    // Release buffer to the pipeline.
                    _pvPipeline.ReleaseBuffer(pvBuffer);
                }
            }
            catch (PvException ex)
            {
                // Log error.
                if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                    GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);

                // Increment consecutive error count.
                OnFailedBuffer();
                nBufferError++;

                // Abort thread if consecutive errors exceed limit.
                if (nBufferError > bufferErrorLimit)
                {
                    OnAcquisitionAborted(new AcquisitionAbortedEventArgs($"Acquisition timed out in Device: {DeviceInfo.ModelName} (ID: {DeviceInfo.UniqueID})!", ex));
                    break;
                }
            }
        }

        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", _pvDeviceInfo.ModelName, _pvDeviceInfo.UniqueID);
    }

    /// <summary>
    /// Eventhandler to changes of acquisition state.
    /// </summary>
    private void OnAcquisitionStateChanged(PvDevice aDevice, PvStream aStream, uint aSource, PvAcquisitionState aState)
    {
        if (aState == PvAcquisitionState.Unlocked)
        {
            if (_threadIsRunning)
            {
                _threadIsRunning = false;
                OnAcquisitionStopped();
            }
        }
    }

    /// <summary>
    /// Converts PvBuffer to <see cref="GcBuffer"/> object.
    /// </summary>
    /// <param name="buffer">PvBuffer containing image and chunkdata.</param>
    /// <returns>Image and chunkdata in a <see cref="GcBuffer"/> object.</returns>
    private unsafe GcBuffer ToGcBuffer(PvBuffer buffer)
    {
        // Allocate new byte array to hold image data.
        byte[] byteArray = new byte[buffer.Image.RequiredSize];
        
        // Copy image data to byte array.
        Marshal.Copy((nint)buffer.Image.DataPointer, byteArray, 0, byteArray.Length);

        // Extract timestamp for image (in PC ticks).
        ulong timeStamp;
        if (_tickFrequency > 0)
            timeStamp = _pcTime0 + (ulong)Math.Round((buffer.Timestamp - (double)_acquisitionStartTime) / _tickFrequency * 1e7); // deprecated GigeVision style
        else
            timeStamp = _pcTime0 + (ulong)Math.Round((buffer.Timestamp - (double)_acquisitionStartTime) / 100); // modern style (buffer timestamp in ns)

        // Extract bit depth.
        uint bitCount = PvImage.GetPixelBitCount(buffer.Image.PixelType);

        // Return new data container.
        return new GcBuffer(imageData: byteArray,
                            width: buffer.Image.Width,
                            height: buffer.Image.Height,
                            pixelFormat: (PixelFormat)(int)buffer.Image.PixelType,
                            pixelDynamicRangeMax: (uint)Math.Pow(2, bitCount) - 1,
                            frameID: (long)buffer.BlockID,
                            timeStamp: timeStamp);
    }

    /// <summary>
    /// Get camera internal timestamp and tick frequency.
    /// </summary>
    /// <returns>Tuple containing timestamp and frequency.</returns>
    private (ulong, double) GetTimestampAndTickFrequency()
    {
        // Latch current timestamp counter.
        if (_pvDevice.Parameters.GetCommand("GevTimestampControlLatch") != null) // deprecated version
            _pvDevice.Parameters.ExecuteCommand("GevTimestampControlLatch");
        else if (_pvDevice.Parameters.GetCommand("TimestampLatch") != null)
            _pvDevice.Parameters.ExecuteCommand("TimestampLatch");

        if (_pvDevice.Parameters.GetCommand("TimestampReset") != null)
            _pvDevice.Parameters.ExecuteCommand("TimestampReset");

        // Timestamp of camera at acquisition start.
        ulong timeStamp;
        double tickFrequency;
        PvGenInteger pvGenInteger = _pvDevice.Parameters.GetInteger("TimeStamp"); // does not update?           
        if (pvGenInteger != null)
        {
            timeStamp = (ulong)pvGenInteger.Value;
            tickFrequency = 0;
        }
        else
        {
            pvGenInteger = _pvDevice.Parameters.GetInteger("GevTimestampValue"); // deprecated version
            if (pvGenInteger != null)
            {
                timeStamp = (ulong)pvGenInteger.Value;
                tickFrequency = _pvDevice.Parameters.GetIntegerValue("GevTimestampTickFrequency");
            }
            else
            {
                throw new NotSupportedException("Unable to extract camera timestamp at acquisition start!");
            }
        }
        return (timeStamp, tickFrequency);
    }

    #endregion
}