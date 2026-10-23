using System;
using System.Linq;

namespace GcLib;

/// <summary>
/// Provides information about a <see cref="GcDevice"/>-derived subclass and its API implementation.
/// </summary>
public record GcDeviceClassInfo
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
        ArgumentNullException.ThrowIfNull(deviceType);

        if (IsDeviceType(deviceType) == false)
            throw new ArgumentException($"The provided type must be a subclass of {nameof(GcDevice<>)} and implement {nameof(IDeviceFactory<>)}.", nameof(deviceType));

        Name = name;
        Version = version;
        DeviceType = deviceType;
    }

    public override string ToString()
    {
        return DeviceType.ToString();
    }

    /// <summary>
    /// Determines whether the specified type follows the device pattern.
    /// </summary>
    /// <remarks>Abstract types are allowed (e.g. mockable test devices).</remarks>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>true if the type derives from <see cref="GcDevice{T}"/> with itself as T and implements <see cref="IDeviceFactory{T}"/>;
    /// otherwise, false.</returns>
    internal static bool IsDeviceType(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(GcDevice<>) && baseType.GetGenericArguments()[0] == type)
                return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeviceFactory<>) && i.GetGenericArguments()[0] == type);
        }

        return false;
    }
}