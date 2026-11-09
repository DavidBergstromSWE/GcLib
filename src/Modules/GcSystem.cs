using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace GcLib;

/// <summary>
/// Represents the system level in the GenICam/GenTL standard module hierarchy and is responsible for camera device discovery, enumeration and instantiation.
/// </summary>
public sealed class GcSystem : IDeviceProvider, IEnumerable<GcDeviceInfo>, IDisposable
{
    #region Fields

    /// <summary>
    /// Synchronization lock for singleton-style instantiation guard.
    /// </summary>
    private static readonly Lock _instantiationLock = new();

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

    /// <summary>
    /// Synchronization lock for mutable system state.
    /// </summary>
    private readonly Lock _stateLock = new();

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
        lock (_instantiationLock)
        {
            if (_isInstantiated)
                throw new InvalidOperationException("System has already been instantiated! Only one instance ís allowed of this class.");

            _isInstantiated = true;
        }

        _availableDevices = [];
        _connectedDevices = [];

        try
        {
            // Make sure library is initialized.
            if (GcLibrary.IsInitialized == false)
                GcLibrary.Init();
        }
        catch
        {
            lock (_instantiationLock)
                _isInstantiated = false;
            throw;
        }
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public bool UpdateDeviceList()
    {
        // Create new empty device list.
        var deviceList = new List<GcDeviceInfo>();

        // Retrieve device classes registered.
        var registeredDeviceClasses = new List<GcDeviceClassInfo>(GcLibrary.GetRegisteredDeviceClasses());

        // Enumerate devices detected of all available classes.
        foreach (GcDeviceClassInfo deviceClassInfo in registeredDeviceClasses)
        {
            // Invoke device enumeration for device class.
            var devices = GcLibrary.GetDeviceEnumerator(deviceClassInfo.DeviceType)();

            // Add detected devices to list.
            deviceList.AddRange(devices);
        }

        // Check connected device list for overlapping or missing items (devices already connected may or may not be discoverable, depending on API).
        List<GcDevice> connectedDevicesSnapshot;
        lock (_stateLock)
            connectedDevicesSnapshot = [.. _connectedDevices];

        foreach (GcDevice device in connectedDevicesSnapshot)
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
        lock (_stateLock)
        {
            if (_availableDevices.SequenceEqual(deviceList) == false)
            {
                _availableDevices = deviceList;
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public List<GcDeviceInfo> GetDeviceList()
    {
        lock (_stateLock)
            return [.. _availableDevices];
    }

    /// <inheritdoc />
    public uint GetNumDevices()
    {
        lock (_stateLock)
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
        GcDeviceInfo deviceInfo;

        // Find index of camera in device list using string identifier.
        lock (_stateLock)
        {
            int index = _availableDevices.FindIndex(x => x.UniqueID == uniqueID);
            if (index == -1)
                throw new ArgumentException($"Device with ID {uniqueID} not found!");

            // Retrieve information of found device.
            deviceInfo = _availableDevices[index];

            // Check if device is already open (e.g. is a member of the list of connected devices).
            if (_connectedDevices.Any(device => device.DeviceInfo.UniqueID == deviceInfo.UniqueID))
                throw new InvalidOperationException($"Device {deviceInfo.ModelName} of ID {deviceInfo.UniqueID} is already open!");
        }

        // Instantiate using factory method of the device class.
        var deviceFactory = GcLibrary.GetDeviceFactory(deviceInfo.DeviceClassInfo.DeviceType);
        var device = deviceFactory(deviceInfo);

        // Log information.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
            GcLibrary.Logger.LogDebug("{deviceModel} (ID: {deviceID}) instantiated using {className} device class", deviceInfo.ModelName, deviceInfo.UniqueID, deviceInfo.DeviceClassInfo.DeviceType.Name);

        // Add device to list of connected devices.
        lock (_stateLock)
        {
            if (_connectedDevices.Any(connectedDevice => connectedDevice.DeviceInfo.UniqueID == deviceInfo.UniqueID))
            {
                device.Close();
                throw new InvalidOperationException($"Device {deviceInfo.ModelName} of ID {deviceInfo.UniqueID} is already open!");
            }

            _connectedDevices.Add(device);

            // Update device info in device list.
            UpdateListWithUpdatedDeviceInfo(device.DeviceInfo);
        }

        // Hook eventhandler to Closed events.
        device.Closed += Device_Closed;

        return device;
    }

    /// <inheritdoc />
    public string GetDeviceID(uint index)
    {
        lock (_stateLock)
            return index < _availableDevices.Count ? _availableDevices[(int)index].UniqueID : null;
    }

    /// <inheritdoc />
    public GcDeviceInfo GetDeviceInfo(string uniqueID)
    {
        // Find device using string identifier.
        lock (_stateLock)
            return _availableDevices.Find(x => x.UniqueID == uniqueID);
    }

    /// <summary>
    /// Closes all connected devices and clears references to all devices on the system.
    /// </summary>
    public void Dispose()
    {
        // Close all connected devices.
        List<GcDevice> connectedDevicesSnapshot;
        lock (_stateLock)
            connectedDevicesSnapshot = [.. _connectedDevices];

        foreach (var device in connectedDevicesSnapshot)
        {
            device.Close();
            device.Closed -= Device_Closed;
            UpdateListWithUpdatedDeviceInfo(device.DeviceInfo);
        }

        // Clear lists of devices.
        lock (_stateLock)
        {
            _connectedDevices.Clear();
            _availableDevices.Clear();
        }

        lock (_instantiationLock)
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
        lock (_stateLock)
        {
            // Remove device from list of connected devices.
            _connectedDevices.Remove(device);

            UpdateListWithUpdatedDeviceInfo(device.DeviceInfo);
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
        lock (_stateLock)
        {
            // Find device.
            int index = _availableDevices.FindIndex(i => i.UniqueID == deviceInfo.UniqueID);
            if (index == -1)
                return;

            // Update mutable device state while preserving object identity.
            _availableDevices[index].IsOpen = deviceInfo.IsOpen;
            _availableDevices[index].IsAccessible = deviceInfo.IsAccessible;
        }
    }

    #endregion

    #region IEnumerable

    public IEnumerator<GcDeviceInfo> GetEnumerator()
    {
        lock (_stateLock)
            return _availableDevices.ToList().GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}