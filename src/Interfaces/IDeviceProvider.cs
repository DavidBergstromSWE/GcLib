using System.Collections.Generic;

namespace GcLib;

/// <summary>
/// Interface for a service providing devices of type <see cref="GcDevice"/>.
/// </summary>
public interface IDeviceProvider
{
    /// <summary>
    /// Updates the list of available devices.
    /// </summary>
    /// <returns><see langword="True"/> if list was changed.</returns>
    bool UpdateDeviceList();

    /// <summary>
    /// Retrieves the number of currently available devices. Use <see cref="UpdateDeviceList"/> to update the number with recent changes.
    /// </summary>
    /// <returns>The number of available devices.</returns>
    uint GetNumDevices();

    /// <summary>
    /// Retrieves a list of currently available devices. Use <see cref="UpdateDeviceList"/> to update the list with recent changes.
    /// </summary>
    /// <returns>List of available devices.</returns>
    List<GcDeviceInfo> GetDeviceList();

    /// <summary>
    /// Inquires information about a device using string identifier. Prior to this call the <see cref="UpdateDeviceList"/> method must be called.
    /// </summary>
    /// <param name="uniqueID">Device string identifier.</param>
    /// <returns>Device information requested (or null if device is not available).</returns>
    GcDeviceInfo GetDeviceInfo(string uniqueID);

    /// <summary>
    /// Queries the unique device id corresponding to the <paramref name="index"/> of the device in the current list of devices. Prior to this call the <see cref="UpdateDeviceList"/> method must be called.
    /// </summary>
    /// <param name="index">List index of device.</param>
    /// <returns>Device string identifier (or null if device is not available).</returns>
    string GetDeviceID(uint index);

    /// <summary>
    /// Opens a device using a string identifier and provides an instantiation of the corresponding <see cref="GcDevice"/>-derived camera class.
    /// </summary>
    /// <param name="uniqueID">Device string identifier.</param>
    /// <returns>Instantiated device object of the correct type.</returns>
    GcDevice OpenDevice(string uniqueID);
}