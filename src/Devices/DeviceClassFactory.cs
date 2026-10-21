using System.Collections.Generic;

namespace GcLib;

/// <summary>
/// Non-generic access to the static factory members of a registered device class.
/// </summary>
internal interface IDeviceClassFactory
{
    /// <summary>
    /// Creates a device from the specified device information.
    /// </summary>
    GcDevice CreateDevice(GcDeviceInfo deviceInfo);

    /// <summary>
    /// Enumerates all available devices of the device class.
    /// </summary>
    List<GcDeviceInfo> EnumerateDevices();
}

/// <summary>
/// Forwards calls to the static <see cref="IDeviceFactory{T}"/> members of <typeparamref name="TDevice"/>, without using reflection.
/// </summary>
/// <typeparam name="TDevice">Device class.</typeparam>
internal sealed class DeviceClassFactory<TDevice> : IDeviceClassFactory where TDevice : GcDevice, IDeviceFactory<TDevice>
{
    /// <inheritdoc/>
    public GcDevice CreateDevice(GcDeviceInfo deviceInfo)
    {
        return TDevice.CreateDevice(deviceInfo);
    }

    /// <inheritdoc/>
    public List<GcDeviceInfo> EnumerateDevices()
    {
        return TDevice.EnumerateDevices();
    }
}
