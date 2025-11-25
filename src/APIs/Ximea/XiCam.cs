using System;
using System.Collections.Generic;
using System.Timers;
using GcLib.Utilities.IO;
using Microsoft.Extensions.Logging;
using xiApi.NET;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to xiAPI.NET from Ximea GmbH.
/// </summary>
public sealed partial class XiCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    /// <summary>
    /// Camera device.
    /// </summary>
    private readonly xiCam _xiCam;

    /// <summary>
    /// Timer object used to periodically check if device connection is valid.
    /// </summary>
    private readonly Timer _checkConnectionTimer;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance and opens a new camera, using an optional string identifier.
    /// </summary>
    /// <param name="deviceID">(Optional) Unique string identifier for device.</param>
    /// <exception cref="ArgumentException"></exception>
    public XiCam(string deviceID = "") : base()
    {
        _xiCam = new xiCam();

        if (_xiCam.GetNumberDevices() == 0)
            throw new ArgumentException("No camera found!");

        if (string.IsNullOrEmpty(deviceID))
        {
            // Get first available device.
            _xiCam.OpenDevice(0);
        }
        else
        {
            // Get specific device.
            _xiCam.OpenDeviceBy(OPEN_BY.OPEN_BY_SN, deviceID);
        }

        // Update device info.
        _xiCam.GetParam(PRM.DEVICE_NAME, out string deviceName);
        _xiCam.GetParam(PRM.DEVICE_SN, out string serialNumber);
        _xiCam.GetParam(PRM.DEVICE_ID, out string ID);

        DeviceInfo = new GcDeviceInfo(vendorName: "Ximea",
                                      modelName: deviceName,
                                      serialNumber: serialNumber,
                                      uniqueID: ID.Replace("\0", string.Empty), // remove null characters
                                      deviceClass: DeviceClassInfo,
                                      isAccessible: false,
                                      isOpen: true);

        // Set default buffer policy.
        BufferPolicy = BUFFERPOLICY.UNSAFE;

        // Initialize some suitable camera settings. (remove?)
        try
        {
            _xiCam.SetParam(PRM.GAIN, 0);
            _xiCam.SetParam(PRM.EXPOSURE, 10000);
            _xiCam.SetParam(PRM.ACQ_TIMING_MODE, ACQ_TIMING_MODE.FRAME_RATE_LIMIT);
            _xiCam.SetParam(PRM.FRAMERATE, 30);
        }
        catch (xiExc)
        {
            //
        }

        // Initialize properties.
        BufferCapacity = 4;

        // Retrieve collection of camera parameters.
        Parameters = ImportParameters();

        // Start periodic checking of device connection validity.
        _checkConnectionTimer = new Timer()
        {
            Interval = 3000, // milliseconds
            AutoReset = true
        };
        _checkConnectionTimer.Elapsed += CheckConnection;
        _checkConnectionTimer.Start();
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void Close()
    {
        // Stop connection checking timer and dispose of it.
        _checkConnectionTimer.Stop();
        try
        {
            _checkConnectionTimer.Dispose();
        }
        catch (Exception)
        {
            //
        }

        // Stops active acquisitions on device and closes datastream.
        base.Close();

        // Close device (will take long time if connection was lost! show dialog?).
        _xiCam.CloseDevice();
    }

    #endregion

    #region Private methods


    /// <summary>
    /// Checks if connection to device is still valid (and raises <see cref="GcDevice.ConnectionLost"/> event if it is not).
    /// </summary>
    private void CheckConnection(object sender, ElapsedEventArgs e)
    {
        bool isAvailable = true;

        try
        {
            _xiCam.GetParam(PRM.IS_DEVICE_EXIST, out int val);
        }
        catch (xiExc ex)
        {
            _checkConnectionTimer.Stop();
            GcLibrary.Logger.LogError(ex, "Failed to detect {device} (ID: {ID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);
            isAvailable = false;
        }
        finally
        {
            if (isAvailable == false)
            {
                _checkConnectionTimer.Stop();
                OnConnectionLost();
            }
        }
    }

    #endregion

    #region IDeviceEnumerator

    /// <summary>
    /// Enumerates and returns a list of available devices of type <see cref="XiCam"/>.
    /// </summary>
    /// <returns>List of available devices.</returns>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        var xiCamEnum = new xiCamEnum();
        int cameraCount = xiCamEnum.ReEnumerate();
        var deviceList = new List<GcDeviceInfo>(cameraCount);
        for (int i = 0; i < cameraCount; i++)
        {
            // ToDo: Add method to check if camera is accessible or not (opened in another application)

            deviceList.Add(new GcDeviceInfo(vendorName: "Ximea",
                                            modelName: xiCamEnum.GetDevNameById(i),
                                            serialNumber: xiCamEnum.GetSerialNumById(i),
                                            uniqueID: xiCamEnum.GetSerialNumById(i),
                                            userDefinedName: xiCamEnum.GetDeviceUserIdById(i),
                                            deviceClass: DeviceClassInfo));
        }
        return deviceList;
    }

    #endregion

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new GcDeviceClassInfo("xiApi.NET", FileHelper.GetAssemblyFileVersion("xiApi.NETX64.dll"), typeof(XiCam));

    #endregion
}