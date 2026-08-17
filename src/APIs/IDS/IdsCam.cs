using System;
using System.Collections.Generic;
using System.Timers;
using IDSImaging.Peak.API;
using IDSImaging.Peak.API.Core;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the IDS Peak API from IDS Imaging Development Systems.
/// </summary>
public sealed partial class IdsCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Private fields

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
    /// Static constructor. Initializes IDS Peak API library and registers device class info for this device type.
    /// </summary>
    static IdsCam()
    {
        // Initialize IDS Peak API library.
        Library.Initialize();

        // Register device class info for this device type.
        DeviceClassInfo = new GcDeviceClassInfo("IDS Peak", Library.Version().ToString(), typeof(IdsCam));
    }

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

    /// <inheritdoc/>
    public override void Close()
    {
        base.Close();

        // Stop connection checking timer and dispose of it.
        _checkConnectionTimer.Stop();
        try
        {
            _checkConnectionTimer.Dispose();
        }
        catch (Exception){ }

        // Close device.
        _device.Dispose();
    }

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; }

    #endregion

    #region IDeviceEnumerator

    /// <summary>
    /// Enumerates and returns a list of available devices of type <see cref="IdsCam"/>.
    /// </summary>
    /// <returns>List of available devices.</returns>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        var deviceManager = DeviceManager.Instance();
        deviceManager.Update();

        var devices = new List<GcDeviceInfo>();
        for (int i = 0; i < deviceManager.Devices().Count; i++)
            devices.Add(GetDeviceInfo(deviceManager.Devices()[i]));

        return devices;
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
