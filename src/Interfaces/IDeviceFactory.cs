using System.Collections.Generic;

namespace GcLib;

/// <summary>
/// Factory for creating devices of a specific type derived from <see cref="GcDevice"/>.
/// </summary>
/// <typeparam name="TDevice">The device type created by the factory; must derive from <see cref="GcDevice"/>.</typeparam>
public interface IDeviceFactory<TDevice> where TDevice : GcDevice
{
    /// <summary>
    /// Provides information about the implementing device type and its API implementation.
    /// </summary>
    static abstract GcDeviceClassInfo DeviceClassInfo { get; }

    /// <summary>
    /// Creates an instance of the implementing device type, from the specified top-level device information.
    /// </summary>
    /// <param name="deviceInfo">Top-level information of the device.</param>
    /// <returns> An instance of <see cref="TDevice"/>.</returns>
    static abstract TDevice CreateDevice(GcDeviceInfo deviceInfo);

    /// <summary>
    /// Enumerates all available devices of the implementing device type.
    /// </summary>
    /// <returns>A list of available devices.</returns>
    static abstract List<GcDeviceInfo> EnumerateDevices();

    /// <summary>
    /// Releases shared resources held by the implementing device type and its API implementation.
    /// </summary>
    static abstract void Release();
}
