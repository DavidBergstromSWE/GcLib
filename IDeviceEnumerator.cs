using System.Collections.Generic;

namespace GcLib;

/// <summary>
/// Provides functionality to enumerate devices in a system.
/// </summary>
/// ToDo: Move to <see cref="GcDevice"/> class if static members of an abstract class are made allowed.
public interface IDeviceEnumerator
{
    /// <summary>
    /// Queries system and enumerates available devices.
    /// </summary>
    /// <returns>List of available devices.</returns>
    static abstract List<GcDeviceInfo> EnumerateDevices();
}