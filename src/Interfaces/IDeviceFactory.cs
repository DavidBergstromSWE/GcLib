using System.Collections.Generic;

namespace GcLib;

/// <summary>
/// Factory for creating devices of a specific type derived from <see cref="GcDevice"/>.
/// </summary>
/// <typeparam name="TDevice">The device type created by the factory; must derive from <see cref="GcDevice"/>.</typeparam>
public interface IDeviceFactory<TDevice> where TDevice : GcDevice
{
    /// <summary>
    /// Gets the device class information for the implementing device type.
    /// </summary>
    static abstract GcDeviceClassInfo DeviceClassInfo { get; }

    /// <summary>
    /// Creates a device from the specified device information.
    /// </summary>
    /// <param name="deviceInfo">Information used to create the device.</param>
    /// <returns>The created device.</returns>
    static abstract TDevice CreateDevice(GcDeviceInfo deviceInfo);

    /// <summary>
    /// Enumerates all available devices of the implementing device type.
    /// </summary>
    /// <returns>A list of available devices.</returns>
    static abstract List<GcDeviceInfo> EnumerateDevices();

    /// <summary>
    /// Releases resources held by the implementing type.
    /// </summary>
    static abstract void Dispose();
}
