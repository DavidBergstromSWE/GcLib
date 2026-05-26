using System;
using System.Collections.Generic;
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

    #endregion

    #region Constructors

    static IdsCam()
    {
        // Register device class info for this device type.
        Library.Initialize();

        DeviceClassInfo = new GcDeviceClassInfo("IDS Peak", Library.Version().ToString(), typeof(IdsCam));
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="deviceID">(Optional) Unique string identifier for device.</param>
    public IdsCam(string deviceID) : base()
    {
        // Find camera devices reachable from PC.
        var deviceManager = DeviceManager.Instance();
        deviceManager.Update();
        var devices = deviceManager.Devices();

        // Find device with matching ID.
        DeviceDescriptor device = null;
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].ID().Replace(":", string.Empty) == deviceID)
            {
                device = devices[i];
                break;
            }
        }

        if (device == null)
            throw new InvalidOperationException($"Failed to connect to device with unique ID {deviceID}.");

        // Connect to device.
        _device = device.OpenDevice(DeviceAccessType.Control);

        // Update device info.
        DeviceInfo = GetDeviceInfo(device);
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
    }

    #endregion

    /// <inheritdoc/>
    public override void Close()
    {
        base.Close();

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

    #endregion 
}
