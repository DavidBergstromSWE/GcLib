using System;

namespace GcLib;

/// <summary>
/// Provides information about a <see cref="GcDevice"/>-derived subclass and its API implementation.
/// </summary>
public class GcDeviceClassInfo
{
    /// <summary>
    /// Name of API.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Version of API.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// <see cref="GcDevice"/>-derived class type.
    /// </summary>
    public Type DeviceType { get; }

    /// <summary>
    /// Instantiates a new info provider of a <see cref="GcDevice"/>-derived class.
    /// </summary>
    /// <param name="name">Name of API/SDK.</param>
    /// <param name="version">Version of API/SDK.</param>
    /// <param name="deviceType"><see cref="GcDevice"/>-derived type.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcDeviceClassInfo(string name, string version, Type deviceType)
    {
        if (deviceType.IsSubclassOf(typeof(GcDevice)))
        {
            Name = name;
            Version = version;
            DeviceType = deviceType;
        }
        else
        {
            throw new ArgumentException($"Type {deviceType} is not a subclass of {nameof(GcDevice)} as required!");
        }
    }

    public override string ToString()
    {
        return DeviceType.ToString();
    }
}