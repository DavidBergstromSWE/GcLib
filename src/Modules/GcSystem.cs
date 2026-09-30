using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace GcLib;

/// <summary>
/// Represents the system level in the GenICam/GenTL standard module hierarchy and is responsible for camera device discovery, enumeration and instantiation.
/// </summary>
public sealed class GcSystem : IDeviceProvider, IEnumerable<GcDeviceInfo>, IDisposable
{
    #region Fields

    /// <summary>
    /// True if system has been instantiated already.
    /// </summary>
    private static bool _isInstantiated;

    /// <summary>
    /// List of available devices.
    /// </summary>
    private List<GcDeviceInfo> _availableDevices;

    /// <summary>
    /// List of connected devices.
    /// </summary>
    private readonly List<GcDevice> _connectedDevices;

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new system, allowing device discovery, enumeration and instantiation.
    /// </summary>
    /// <remarks>
    /// Note: Can only be instantiated once and throws an <see cref="InvalidOperationException"/> when attempted to instantiate a second time.
    /// </remarks>
    /// <exception cref="InvalidOperationException"></exception>
    public GcSystem()
    {
        if (_isInstantiated)
            throw new InvalidOperationException("System has already been instantiated! Only one instance ís allowed of this class.");

        _availableDevices = [];
        _connectedDevices = [];

        // Make sure library is initialized.
        if (GcLibrary.IsInitialized == false)
            GcLibrary.Init();

        _isInstantiated = true;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public bool UpdateDeviceList()
    {
        // Create new empty device list.
        var deviceList = new List<GcDeviceInfo>();

        // Retrieve device classes available.
        var availableDeviceClasses = new List<GcDeviceClassInfo>(GcLibrary.GetAvailableDeviceClasses());

        // Enumerate devices detected of all available classes.
        foreach (Type type in availableDeviceClasses.Select(item => item.DeviceType))
        {
            // Invoke device enumeration for device class.
            var devices = (List<GcDeviceInfo>)type.GetMethod(nameof(IDeviceEnumerator.EnumerateDevices))
                                                  .Invoke(type, null);

            // Add detected devices to list.
            deviceList.AddRange(devices);
        }

        // Check connected device list for overlapping or missing items (devices already connected may or may not be discoverable, depending on API).
        foreach (GcDevice device in _connectedDevices)
        {
            int index = deviceList.FindIndex(i => i.UniqueID == device.DeviceInfo.UniqueID);
            if (index >= 0)
                // Overlapping item: replace it with new status.
                deviceList[index] = device.DeviceInfo;
            else
                // Missing item: add new.
                deviceList.Add(device.DeviceInfo);
        }

        // Remove duplicates from device list (can happen if device is discoverable by multiple APIs).
        deviceList = [.. deviceList.Distinct()];

        // Update list of available devices.
        if (_availableDevices.SequenceEqual(deviceList) == false)
        {
            _availableDevices = deviceList;
            return true;
        }
        else return false;
    }

    /// <inheritdoc />
    public List<GcDeviceInfo> GetDeviceList()
    {
        return _availableDevices;
    }

    /// <inheritdoc />
    public uint GetNumDevices()
    {
        return (uint)_availableDevices.Count;
    }

    /// <summary>
    /// Opens a device using top-level device information and provides an instantiation of the corresponding <see cref="GcDevice"/>-derived camera class.
    /// </summary>
    /// <param name="deviceInfo">Top-level information about device.</param>
    /// <returns>Instantiated device object of the correct type.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="TargetInvocationException"/>
    public GcDevice OpenDevice(GcDeviceInfo deviceInfo)
    {
        return OpenDevice(deviceInfo.UniqueID);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"/>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="TargetInvocationException"/>
    public GcDevice OpenDevice(string uniqueID)
    {
        GcDevice device = null;

        // Find index of camera in device list using string identifier.
        int index = _availableDevices.FindIndex(x => x.UniqueID == uniqueID);
        if (index == -1)
            throw new ArgumentException($"Device with ID {uniqueID} not found!");

        // Retrieve information of found device.
        GcDeviceInfo deviceInfo = _availableDevices[index];

        // Check if device is already open (e.g. is a member of the list of connected devices).
        if (_connectedDevices.Any(device => device.DeviceInfo.UniqueID == deviceInfo.UniqueID))
            throw new InvalidOperationException($"Device {deviceInfo.ModelName} of ID {deviceInfo.UniqueID} is already open!");

        // Instantiate new device (will throw TargetInvocatonException if constructor fails).
        device = (GcDevice)Activator.CreateInstance(deviceInfo.DeviceClassInfo.DeviceType, uniqueID);

        // Log information.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
            GcLibrary.Logger.LogDebug("{deviceModel} (ID: {deviceID}) instantiated using {className} device class", deviceInfo.ModelName, deviceInfo.UniqueID, deviceInfo.DeviceClassInfo.DeviceType.Name);

        // Add device to list of connected devices.
        _connectedDevices.Add(device);

        // Update device info in device list.
        UpdateListWithUpdatedDeviceInfo(device.DeviceInfo);

        // Hook eventhandler to Closed events.
        device.Closed += Device_Closed;

        return device;
    }

    /// <inheritdoc />
    public string GetDeviceID(uint index)
    {
        return index < _availableDevices.Count ? _availableDevices[(int)index].UniqueID : null;
    }

    /// <inheritdoc />
    public GcDeviceInfo GetDeviceInfo(string uniqueID)
    {
        // Find device using string identifier.
        return _availableDevices.Find(x => x.UniqueID == uniqueID);
    }

    /// <summary>
    /// Closes all connected devices and clears references to all devices on the system.
    /// </summary>
    public void Dispose()
    {
        // Close all connected devices.
        foreach (var device in _connectedDevices.ToList())
        {
            device.Close();
            UpdateListWithUpdatedDeviceInfo(device.DeviceInfo);
        }

        // Clear lists of devices.
        _connectedDevices.Clear();
        _availableDevices.Clear();

        _isInstantiated = false;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Eventhandling method to <see cref="GcDevice.Closed"/> events, removing the device from the list of connected devices.
    /// </summary>
    private void Device_Closed(object sender, EventArgs e)
    {
        var device = (GcDevice)sender;

        // Acquire mutual-exclusion lock to list of connected devices for the current thread.
        lock (_connectedDevices)
        {
            // Remove device from list of connected devices.
            _connectedDevices.Remove(device);
        }

        // Unsubscribe from the event.
        device.Closed -= Device_Closed;
    }

    /// <summary>
    /// Updates device list with updated info about device.
    /// </summary>
    /// <param name="deviceInfo">Updated device info.</param>
    private void UpdateListWithUpdatedDeviceInfo(GcDeviceInfo deviceInfo)
    {
        // Find device.
        int index = _availableDevices.FindIndex(i => i.UniqueID == deviceInfo.UniqueID);
        if (index == -1)
            return;

        // Update device list.
        _availableDevices[index] = deviceInfo;
    }

    #endregion

    #region IEnumerable

    public IEnumerator<GcDeviceInfo> GetEnumerator()
    {
        return _availableDevices.GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}