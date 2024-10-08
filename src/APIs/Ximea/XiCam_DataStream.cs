using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using xiApi.NET;

namespace GcLib;

public sealed partial class XiCam
{
    #region Constants

    /// <summary>
    /// Buffer policy enum (UNSAFE or SAFE).
    /// </summary>
    public enum BUFFERPOLICY
    {
        /// <summary>
        /// User gets pointer to internally allocated circle buffer and data may be overwritten by device.
        /// </summary>
        UNSAFE = 0,

        /// <summary>
        /// Data from device will be copied to user allocated buffer or xiApi allocated memory.
        /// </summary>
        SAFE = 1
    }

    #endregion

    #region Fields

    /// <summary>
    /// PC time when acquisition is started (given in PC ticks, where a single tick represents one hundred nanoseconds or one ten-millionth of a second).
    /// </summary>
    private ulong _pcTime0;

    /// <summary>
    /// Camera timestamp when acquisition is started (in nanoseconds).
    /// </summary>
    private ulong _acquisitionStartTime = 0;

    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private Thread _imageAcquisitionThread;

    /// <summary>
    /// Image acquisition thread stopping condition.
    /// </summary>
    private bool _threadIsRunning;

    /// <summary>
    /// Timeout (in milliseconds) used for GetImage in acquisition thread.
    /// </summary>
    private int _timeout = 1000;

    // backing-fields
    private uint _bufferCapacity;
    private BUFFERPOLICY _bufferPolicy;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override uint PayloadSize
    {
        get
        {
            _xiCam.GetParam(PRM.IMAGE_PAYLOAD_SIZE, out int val);
            return (uint)val;
        }
    }

    /// <inheritdoc/>
    public override uint BufferCapacity
    {
        get => _bufferCapacity;

        set
        {
            if (value <= _xiCam.GetParamInt(PRM.BUFFERS_QUEUE_SIZE + PRMM.MAX))
            {
                _bufferCapacity = value;
                _xiCam.SetParam(PRM.BUFFERS_QUEUE_SIZE, _bufferCapacity);
            }
        }
    }

    /// <summary>
    /// Defines buffer handling. Can be safe (data will be copied to user allocated buffer) or unsafe (user will get buffer from internal circle buffer, which can be overwritten). 
    /// </summary>
    public BUFFERPOLICY BufferPolicy
    {
        get => _bufferPolicy;
        set
        {
            _bufferPolicy = value;
            _xiCam.SetParam(PRM.BUFFER_POLICY, (int)_bufferPolicy);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Starts acquisition on the device.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="FormatException"/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        // Check if pixel format selected is currently supported.
        PixelFormat pixelFormat = GetPixelFormat(_xiCam.GetParamInt(PRM.IMAGE_DATA_FORMAT));
        if (pixelFormat == PixelFormat.InvalidPixelFormat)
            throw new FormatException($"Pixel format is not supported in device class {GetType().Name}!");

        // Check exposure time and frame rate and set appropriate timeout.
        _timeout = Convert.ToInt32(((float)_xiCam.GetParamInt(PRM.EXPOSURE) / 1000) + (1 / _xiCam.GetParamFloat(PRM.FRAMERATE) * 1000));
        _timeout = (int)Math.Ceiling(_timeout / 1000.0) * 1000; // round up to nearest second

        // PC time at acquisition start (in ticks).
        _pcTime0 = (ulong)DateTime.Now.Ticks;

        // Timestamp in camera at acquisition start (in nanoseconds).
        _acquisitionStartTime = _xiCam.GetParamUlong(PRM.TIMESTAMP);

        // Start image acquisition thread depending on buffer policy.
        _imageAcquisitionThread = BufferPolicy == BUFFERPOLICY.SAFE
            ? new Thread(ImageAcquisitionThreadSAFE)
            : new Thread(ImageAcquisitionThreadUNSAFE);
        _imageAcquisitionThread.Name = BufferPolicy == BUFFERPOLICY.SAFE ? "ImageAcquisitionThreadSAFE" : "ImageAcquisitionThreadUNSAFE";
        _threadIsRunning = true;
        _imageAcquisitionThread.Start();

        // Start acquisition on the device.
        _xiCam.StartAcquisition();

        IsAcquiring = true;

        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        // Stop image acquisition thread.
        _threadIsRunning = false;

        try
        {
            _xiCam.GetParam(PRM.IS_DEVICE_EXIST, out int val);
            _xiCam.StopAcquisition();
        }
        catch (xiExc ex)
        {
            // Can be raised if acquisition is stopped when connection was lost.
            GcLibrary.Logger.LogError(ex, "Failed to stop acquisition in {Device} (ID: {ID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);
        }
        finally
        {
            IsAcquiring = false;
            OnAcquisitionStopped();
        }
    }

    /// <summary>
    /// Image acquisition thread, based on using SAFE buffer policy version of GetImage from xiApi.NET.
    /// </summary>
    private void ImageAcquisitionThreadSAFE()
    {
        GcLibrary.Logger.LogTrace("Image acquisition thread (ID: {ThreadName}) in Device {ModelName} (ID: {ID}) started", _imageAcquisitionThread.Name, DeviceInfo.ModelName, DeviceInfo.UniqueID);

        // Using pre-allocated memory buffer and data copy.
        _xiCam.GetParam(PRM.IMAGE_PAYLOAD_SIZE, out int val);
        byte[] buffer = new byte[val];

        uint nBufferErrors = 0; // Number of consecutive buffer errors.
        uint bufferErrorLimit = 10; // Number of consecutive buffer errors limit.

        while (_threadIsRunning)
        {
            try
            {
                _xiCam.GetImage(buffer, _timeout);
                OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(buffer, _xiCam.GetLastImageParams()), DateTime.Now));
                nBufferErrors = 0;
            }
            catch (xiExc ex)
            {
                GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);

                // Increment consecutive error count.
                OnFailedBuffer();
                nBufferErrors++;

                // Abort thread if consecutive errors exceed limit.
                if (nBufferErrors > bufferErrorLimit)
                {
                    OnAcquisitionAborted(new AcquisitionAbortedEventArgs($"Acquisition timed out in Device: {DeviceInfo.ModelName} (ID: {DeviceInfo.UniqueID})!", ex));
                    break;
                }
            }                  
        }

        GcLibrary.Logger.LogTrace("Image acquisition thread (ID: {ThreadName}) in Device {ModelName} (ID: {ID}) stopped", _imageAcquisitionThread.Name, DeviceInfo.ModelName, DeviceInfo.UniqueID);
    }

    /// <summary>
    /// Image acquisition thread, based on using UNSAFE buffer policy version of GetImage from xiApi.NET.
    /// </summary>
    private void ImageAcquisitionThreadUNSAFE()
    {
        uint nBufferErrors = 0; // Number of consecutive buffer errors.
        uint bufferErrorLimit = 10; // Number of consecutive buffer errors limit.

        GcLibrary.Logger.LogTrace("Image acquisition thread (ID: {ThreadName}) in Device {ModelName} (ID: {ID}) started", _imageAcquisitionThread.Name, DeviceInfo.ModelName, DeviceInfo.UniqueID);

        while (_threadIsRunning)
        {
            try
            {
                _xiCam.GetImage(img_arr: out byte[] buffer, timeout: _timeout);
                OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(buffer, _xiCam.GetLastImageParams()), DateTime.Now));
            }
            catch (xiExc ex)
            {
                GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);

                // Increment consecutive error count.
                OnFailedBuffer();
                nBufferErrors++;

                // Abort thread if consecutive errors exceed limit.
                if (nBufferErrors > bufferErrorLimit)
                {
                    OnAcquisitionAborted(new AcquisitionAbortedEventArgs($"Acquisition timed out in Device: {DeviceInfo.ModelName} (ID: {DeviceInfo.UniqueID})!", ex));
                    break;
                }
            }
        }

        GcLibrary.Logger.LogTrace("Image acquisition thread (ID: {ThreadName}) in Device {ModelName} (ID: {ID}) stopped", _imageAcquisitionThread.Name, DeviceInfo.ModelName, DeviceInfo.UniqueID);
    }

    /// <summary>
    /// Converts image buffer to GcBuffer.
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    /// <param name="imgParams">Chunk data information about image.</param>
    /// <returns>Image and chunkdata in GcBuffer object.</returns>
    private GcBuffer ToGcBuffer(byte[] buffer, ImgParams imgParams)
    {
        int width = imgParams.GetWidth();
        int height = imgParams.GetHeight();
        PixelFormat pixelFormat = GetPixelFormat(imgParams.GetDataFormat());
        long frameNumber = imgParams.GetFrameNum();
        ulong timeStamp = _pcTime0 + (ulong)Math.Round((imgParams.GetTimestamp() * TimeSpan.TicksPerSecond) - ((double)_acquisitionStartTime / 100));
        uint saturationValue = (uint)imgParams.GetLastImageStruct().data_saturation;

        return new GcBuffer(imageData: buffer,
                            width: (uint)width,
                            height: (uint)height,
                            pixelFormat: pixelFormat,
                            pixelDynamicRangeMax: saturationValue,
                            frameID: frameNumber,
                            timeStamp: timeStamp);
    }

    /// <summary>
    /// Get translated PFNC pixel format from xiAp.NET image format.
    /// </summary>
    /// <param name="imageFormat">Image format as defined by the xiApi.NET.</param>
    /// <returns>Pixel format according to GenICam PFNC.</returns>
    private static PixelFormat GetPixelFormat(int imageFormat)
    {
        return imageFormat switch
        {
            IMG_FORMAT.MONO8 => PixelFormat.Mono8,
            IMG_FORMAT.MONO16 => PixelFormat.Mono16,
            IMG_FORMAT.RGB24 => PixelFormat.BGR8,
            IMG_FORMAT.RGB32 => PixelFormat.BGRa8,
            //IMG_FORMAT.RGB48 => PixelFormat.BGR16,
            //IMG_FORMAT.RGB64 => PixelFormat.BGRa16,
            //IMG_FORMAT.RGBPLANAR => PixelFormat.RGB8_Planar,
            //IMG_FORMAT.RGB16_PLANAR => PixelFormat.RGB16_Planar,
            IMG_FORMAT.RAW8 => PixelFormat.BayerBG8,
            IMG_FORMAT.RAW16 => PixelFormat.BayerBG16,
            _ => PixelFormat.InvalidPixelFormat,
        };
    }

    #endregion
}