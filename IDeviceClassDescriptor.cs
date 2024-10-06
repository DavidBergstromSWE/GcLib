namespace GcLib;

/// <summary>
/// Provides descriptions of a device class.
/// </summary>
/// ToDo: Move to <see cref="GcDevice"/> class if static members of an abstract class are made allowed.
public interface IDeviceClassDescriptor
{
    /// <summary>
    /// Contains information about device class and its API implementation.
    /// </summary>
    static abstract GcDeviceClassInfo DeviceClassInfo { get; }
}