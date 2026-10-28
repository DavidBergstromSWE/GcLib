using System;
using System.Collections.Generic;
using System.Timers;
using GcLib.Utilities.FileIO;
using IDSImaging.Peak.API;
using IDSImaging.Peak.API.Core;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the IDS Peak API from IDS Imaging Development Systems.
/// </summary>
public sealed partial class IdsCam : GcDevice<IdsCam>, IDeviceFactory<IdsCam>
{
    #region Private fields

    /// <summary>
    /// Represents the shared device manager instance.
    /// </summary>
    private static DeviceManager _deviceManager;

    /// <summary>
    /// Device-level class in IDS Peak API. Connects, configures and controls devices.
    /// </summary>
    private readonly Device _device;

    /// <summary>
    /// Timer object used to periodically check if device connection is valid.
    /// </summary>
    private readonly Timer _checkConnectionTimer;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor. Connects to device with specified unique ID and retrieves camera parameters. If no unique ID is provided, it will attempt to connect to the first available device.
    /// </summary>
    /// <param name="deviceID">(Optional) Unique string identifier for device.</param>
    public IdsCam(string deviceID = "") : base()
    {
        // Find camera devices reachable from PC.
        var deviceManager = DeviceManager.Instance();
        deviceManager.Update();
        var devices = deviceManager.Devices();

        DeviceDescriptor deviceDescriptor = null;

        if (string.IsNullOrEmpty(deviceID))
        {
            // Get first available device.
            if (devices.Count > 0)
                deviceDescriptor = devices[0];
        }
        else
        {
            // Find device with matching ID.        
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].ID().Replace(":", string.Empty) == deviceID)
                {
                    deviceDescriptor = devices[i];
                    break;
                }
            }
        }

        if (deviceDescriptor == null)
            throw new InvalidOperationException($"No camera found!");

        // Connect to device.
        _device = deviceDescriptor.OpenDevice(DeviceAccessType.Control);

        // Update device info.
        DeviceInfo = GetDeviceInfo(deviceDescriptor);
        DeviceInfo.IsOpen = true;
        DeviceInfo.IsAccessible = false;

        // Retrieve node map for device.
        _nodeMap = _device.RemoteDevice().NodeMaps()[0];

        // Retrieve collection of camera parameters from node map.
        Parameters = ImportParameters();

        // Set default buffer capacity.
        BufferCapacity = 4;

        // Open data stream for device.
        _dataStream = _device.DataStreams()[0].OpenDataStream();

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

    #region Protected methods

    /// <inheritdoc/>
    protected override void OnDeviceClosed()
    {
        // Stop connection checking timer and dispose of it.
        _checkConnectionTimer.Stop();
        try
        {
            _checkConnectionTimer.Dispose();
        }
        catch (Exception) { }

        // Close device.
        _device.Dispose();
    }

    #endregion

    #region IDeviceFactory

    /// <inheritdoc/>
    public static void Initialize()
    {
        // Initialize IDS Peak API library.
        Library.Initialize();

        // Initialize device manager for enumerating devices.
        _deviceManager = DeviceManager.Instance();

        // Update device class info with current version of IDS Peak API.
        DeviceClassInfo = new GcDeviceClassInfo("IDS Peak", Library.Version().ToString(), typeof(IdsCam));
    }

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; private set; } = new("IDS Peak", FileHelper.GetAssemblyFileVersion("IDSImaging.Peak.API.IPLExtension.dll"), typeof(IdsCam));

    /// <inheritdoc/>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        // Update device manager to get current list of devices.
        _deviceManager.Update();

        var devices = new List<GcDeviceInfo>();
        for (int i = 0; i < _deviceManager.Devices().Count; i++)
            devices.Add(GetDeviceInfo(_deviceManager.Devices()[i]));

        return devices;
    }

    /// <inheritdoc/>
    public static IdsCam CreateDevice(GcDeviceInfo deviceInfo)
    {
        return new IdsCam(deviceInfo.UniqueID);
    }

    /// <inheritdoc/>
    public static void Release()
    {
        // Dispose of the device manager.
        _deviceManager.Dispose();

        // Dispose of the IDS Peak API library.
        try
        {
            Library.Close();
        }
        catch (Exception)
        {

        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Get top-level info about device.
    /// </summary>
    /// <param name="deviceDescriptor">Device ID information.</param>
    /// <returns>Device info.</returns>
    private static GcDeviceInfo GetDeviceInfo(DeviceDescriptor deviceDescriptor)
    {
        return new GcDeviceInfo(
            vendorName: deviceDescriptor.VendorName(),
            modelName: deviceDescriptor.ModelName(),
            serialNumber: deviceDescriptor.SerialNumber(),
            uniqueID: deviceDescriptor.ID().Replace(":", string.Empty),
            userDefinedName: deviceDescriptor.UserDefinedName(),
            deviceClass: DeviceClassInfo,
            isAccessible: deviceDescriptor.AccessStatus() != DeviceAccessStatus.NoAccess);
    }

    /// <summary>
    /// Callback method for periodic checking of device connection validity. If the device is no longer accessible, it raises the ConnectionLost event.
    /// </summary>
    private void CheckConnection(object sender, ElapsedEventArgs e)
    {
        // Check if device is still connected and accessible.
        var deviceManager = DeviceManager.Instance();
        deviceManager.Update();
        var devices = deviceManager.Devices();
        if (devices.Count > 0)
        {
            foreach (var device in devices)
            {
                if (device.ID().Replace(":", string.Empty) == DeviceInfo.UniqueID)
                {
                    // Device is still accessible.
                    return;
                }
            }
        }

        // Device is no longer accessible, raise event.
        OnConnectionLost();
    }

    #endregion
}