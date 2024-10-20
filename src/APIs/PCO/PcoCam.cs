using System;
using System.Collections.Generic;
using System.Timers;
using GcLib.Utilities.IO;
using PCO.SDK;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the PCO SDK from PCO Imaging (PCO AG).
/// </summary>
public sealed partial class PcoCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    /// <summary>
    /// GenApi module attached to camera.
    /// </summary>
    private readonly GenApi _genApi;

    /// <summary>
    /// Timer object used to periodically check if device connection is valid.
    /// </summary>
    private readonly Timer _checkConnectionTimer;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="deviceID">(Optional) Unique string identifier for device.</param>
    public PcoCam(string deviceID = "") : base()
    {
        try
        {
            _genApi = new GenApi(this, deviceID);
        }
        catch (DivideByZeroException)
        {
            var apiInfo = DeviceClassInfo;
            throw new InvalidOperationException($"Exception caused by API ({apiInfo.Name} {apiInfo.Version}) when attempting to open a device of type {apiInfo.DeviceType.Name}.");
        }

        // Update device info.
        DeviceInfo = _genApi.DeviceInfo;
        DeviceInfo.IsOpen = true;
        DeviceInfo.IsAccessible = false;

        // Retrieve collection of camera parameters.
        Parameters = ImportParameters();

        // Set timestamp of camera (to current date and time).
        _genApi.TimestampReset.Execute();

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
        _checkConnectionTimer?.Stop();
        _checkConnectionTimer?.Dispose();

        _genApi?.Dispose();

        base.Close();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Checks if connection to device is still valid (and raises ConnectionLost event if it is not).
    /// </summary>
    private void CheckConnection(object sender, ElapsedEventArgs e)
    {
        if (_genApi.IsAvailable() == false)
        {
            OnConnectionLost();
            _checkConnectionTimer.Stop();
        }
    }

    /// <summary>
    /// Get top-level info about device.
    /// </summary>
    /// <param name="cameraHandle">Handle to previously opened camera.</param>
    /// <returns>Device info.</returns>
    private static GcDeviceInfo GetDeviceInfo(nint cameraHandle)
    {
        // Get camera name.
        var name = LibWrapper.GetCameraName(cameraHandle);

        // Get camera type.
        var serial = LibWrapper.GetCameraSerialNumber(cameraHandle);

        return new GcDeviceInfo(vendorName: "PCO AG",
                                modelName: name,
                                serialNumber: serial,
                                uniqueID: serial, // use serial number as unique ID
                                deviceClass: DeviceClassInfo,
                                isAccessible: true);

    }

    #endregion

    #region IDeviceEnumerator

    /// <summary>
    /// Enumerates and returns a list of available devices of type <see cref="PcoCam"/>.
    /// </summary>
    /// <returns>List of available devices.</returns>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        var deviceList = new List<GcDeviceInfo>();
        var cameraHandleList = new List<nint>();

        // Request new device.
        nint cameraHandle = nint.Zero;

        // all handles need to be reset before resetting!
        //LibWrapper.PCO_ResetLib();

        // Find next available camera.
        while (LibWrapper.OpenCamera(ref cameraHandle))
        {
            deviceList.Add(GetDeviceInfo(cameraHandle));
            cameraHandleList.Add(cameraHandle);

            // Reset handle (to open next available one).
            cameraHandle = nint.Zero;
        }

        // Close all opened camera handles.
        cameraHandleList.ForEach(handle =>
        {
            try
            {
                LibWrapper.CloseCamera(handle);
            }
            catch (PcoException) { }
        });

        return deviceList;
    }


    #endregion

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new GcDeviceClassInfo("pco.sdk", FileHelper.GetAssemblyFileVersion("SC2_Cam.dll"), typeof(PcoCam));

    #endregion
}