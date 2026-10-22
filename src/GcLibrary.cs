using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: InternalsVisibleTo("GcLib.UnitTests")]

namespace GcLib;

/// <summary>
/// Class library providing a common interface to control settings and streaming of GenICam/GenTL-standardized cameras, using underlying APIs from supported third-party camera manufacturers and library providers.
/// </summary>
public static class GcLibrary
{
    #region Fields

    /// <summary>
    /// Device classes registered on the current system.
    /// </summary>
    private static readonly Dictionary<Type, GcDeviceClassInfo> _registeredDeviceClasses = [];

    /// <summary>
    /// Factory methods of registered device classes.
    /// </summary>
    private static readonly Dictionary<Type, Func<GcDeviceInfo, GcDevice>> _deviceFactories = [];

    /// <summary>
    /// Device enumeration methods of registered device classes.
    /// </summary>
    private static readonly Dictionary<Type, Func<List<GcDeviceInfo>>> _deviceEnumerators = [];

    #endregion

    #region Properties

    /// <summary>
    /// True if library has been initialized, false if not.
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Logger for logging events in the library.
    /// </summary>
    internal static ILogger Logger { get; private set; } = NullLogger.Instance;

    #endregion

    #region Public methods

    /// <summary>
    /// Initializes library, by registering device classes to be used. An optional logger can be provided for logging events.
    /// <para>
    /// By default, all built-in device classes available on the current system will be registered.
    /// This can be overrided by setting <paramref name="autoRegister"/> to <see langword="false"/>, in which registration has to be set manually using <see cref="Register{TDevice}"/>.
    /// </para>
    /// </summary>
    /// <param name="autoRegister">If <see langword="true"/>, all built-in device classes available on the current system are registered. If <see langword="false"/>, registration has to be done manually.</param>
    /// <param name="logger">(Optional) Logger to use for logging events in the library.</param>
    /// <exception cref="InvalidOperationException">Thrown if the library has already been initialized.</exception>
    public static void Init(bool autoRegister = true, ILogger logger = null)
    {
        if (IsInitialized)
            throw new InvalidOperationException("Library has already been initialized!");

        // Assign logger.
        Logger = logger ?? NullLogger.Instance;

        IsInitialized = true;

        // Register all implemented device classes in the library.
        if (autoRegister)
        {
            foreach (var deviceType in GetImplementedDeviceClasses())
                Register(deviceType);
        }

        Logger.LogDebug("GcLib initialized");
    }

    /// <summary>
    /// Retrieves information about a specific <see cref="GcDevice"/>-derived subclass.
    /// </summary>
    /// <typeparam name="TDevice"><see cref="GcDevice"/>-derived type.</typeparam>
    /// <returns>Device class information.</returns>
    public static GcDeviceClassInfo GetDeviceClassInfo<TDevice>() where TDevice : GcDevice<TDevice>, IDeviceFactory<TDevice>
    {
        return TDevice.DeviceClassInfo;
    }

    /// <summary>
    /// Registers a new device class to be used in the library.
    /// </summary>
    /// <typeparam name="TDevice">Device type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the library is not initialized.</exception>
    /// <exception cref="ArgumentException">Thrown if the device class is already registered.</exception>
    public static void Register<TDevice>() where TDevice : GcDevice<TDevice>, IDeviceFactory<TDevice>
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        // Check if the device class is already registered.
        if (_registeredDeviceClasses.ContainsKey(typeof(TDevice)))
            throw new ArgumentException("A device class of this type is already registered!", typeof(TDevice).Name);

        try
        {
            // Add device class if devices can be enumerated (is there a better test of dll functionality?).
            TDevice.EnumerateDevices();

            // Add detected devices to list.
            _registeredDeviceClasses.Add(typeof(TDevice), TDevice.DeviceClassInfo);

            // Add factory and enumeration methods for this device class.
            _deviceFactories.Add(typeof(TDevice), TDevice.CreateDevice);
            _deviceEnumerators.Add(typeof(TDevice), TDevice.EnumerateDevices);

            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("{DeviceType} registered ({API} v{Version})", TDevice.DeviceClassInfo.DeviceType.Name, TDevice.DeviceClassInfo.Name, TDevice.DeviceClassInfo.Version);
        }
        catch (Exception ex)
        {
            // Log for debugging.
            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Unable to register device class of type {Name}", typeof(TDevice).Name);
        }
    }

    /// <summary>
    /// Registers a new device class to be used in the library.
    /// </summary>
    /// <param name="deviceType">The type of the device class to register.</param>
    /// <exception cref="InvalidOperationException">Thrown if the library is not initialized.</exception>
    /// <exception cref="ArgumentException">Thrown if the device class is already registered or the type is invalid.</exception>
    public static void Register(Type deviceType)
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        // Validate that the provided type is a subclass of GcDevice and implements required interfaces.
        if (!IsDeviceType(deviceType))
            throw new ArgumentException($"The provided type must be a subclass of {nameof(GcDevice)} and implement {nameof(IDeviceFactory<>)}.", nameof(deviceType));
        try
        {
            // Use reflection to invoke the generic Register<TDevice>() method.
            typeof(GcLibrary).GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null).MakeGenericMethod(deviceType).Invoke(null, null);
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException;
            if (inner != null)
            {
                ExceptionDispatchInfo.Capture(inner).Throw();
            }
            throw; // rethrow original TargetInvocationException if there's no inner exception
        }
    }

    /// <summary>
    /// Unregister a device class from being used in the library.
    /// </summary>
    /// <typeparam name="TDevice">Device type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the library is not initialized.</exception>
    /// <exception cref="ArgumentException">Thrown if the device class is not registered.</exception>
    public static void Unregister<TDevice>() where TDevice : GcDevice<TDevice>, IDeviceFactory<TDevice>
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        if (IsRegistered<TDevice>())
        {
            try
            {
                // Release resources used by API.
                TDevice.Release();
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.LogDebug(ex, "Failed to release resources for {DeviceType}", typeof(TDevice).Name);
            }

            // Remove device class from list.
            _registeredDeviceClasses.Remove(typeof(TDevice));

            // Remove factory and enumeration methods for this device class.
            _deviceFactories.Remove(typeof(TDevice));
            _deviceEnumerators.Remove(typeof(TDevice));
        }
        else throw new ArgumentException("Device class is not registered!", typeof(TDevice).Name);

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("{DeviceType} unregistered", typeof(TDevice).Name);
    }

    /// <summary>
    /// Unregister a device class from being used in the library.
    /// </summary>
    /// <param name="deviceType">The type of the device class to register.</param>
    /// <exception cref="InvalidOperationException">Thrown if the library is not initialized.</exception>
    /// <exception cref="ArgumentException">Thrown if the device class is not registered or the type is invalid.</exception>
    public static void Unregister(Type deviceType)
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        // Validate that the provided type is a subclass of GcDevice and implements required interfaces.
        if (!IsDeviceType(deviceType))
            throw new ArgumentException($"The provided type must be a subclass of {nameof(GcDevice)} and implement {nameof(IDeviceFactory<>)}.", nameof(deviceType));

        // Use reflection to invoke the generic Unregister<TDevice>() method.
        try
        {
            typeof(GcLibrary).GetMethod(nameof(Unregister), BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null).MakeGenericMethod(deviceType).Invoke(null, null);
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException;
            if (inner != null)
            {
                ExceptionDispatchInfo.Capture(inner).Throw();
            }
            throw; // rethrow original TargetInvocationException if there's no inner exception
        }
    }

    /// <summary>
    /// Finds all types in the current application domain that implement <see cref="GcDevice"/> and <see cref="IDeviceFactory{T}"/>.
    /// </summary>
    /// <returns>A list of Type objects for the discovered device classes.</returns>
    public static List<Type> GetImplementedDeviceClasses()
    {
        return [.. AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(assembly => { try { return assembly.GetTypes(); } catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }}) // Return only types that were successfully loaded
                                          .Where(IsDeviceType)]; // Return only the types that derive from GcDevice<T> and implement IDeviceFactory<T>
    }

    private static bool IsDeviceType(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(GcDevice<>) && baseType.GetGenericArguments()[0] == type)
                return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeviceFactory<>) && i.GetGenericArguments()[0] == type);
        }
        return false;
    }

    /// <summary>
    /// Retrieves device classes registered in the library.
    /// </summary>
    /// <remarks>Only device classes that have been successfully registered can be discovered, enumerated and created in the library.</remarks>
    /// <returns>Device classes registered.</returns>
    public static IReadOnlyCollection<GcDeviceClassInfo> GetRegisteredDeviceClasses()
    {
        return _registeredDeviceClasses.Values;
    }

    /// <summary>
    /// Determines whether the specified device class is registered in the library.
    /// </summary>
    /// <remarks>Only device classes that have been successfully registered can be discovered, enumerated and created in the library.</remarks>
    /// <typeparam name="TDevice">The device class to check.</typeparam>
    /// <returns><see langword="true"/> if the type is registered; otherwise, <see langword="false"/>.</returns>
    public static bool IsRegistered<TDevice>() where TDevice : GcDevice<TDevice>, IDeviceFactory<TDevice>
    {
        return _registeredDeviceClasses.ContainsKey(typeof(TDevice));
    }

    /// <summary>
    /// Determines whether a specific device class is registered for being used in the library.
    /// </summary>
    /// <remarks>Only device classes that have been successfully registered can be discovered, enumerated and created in the library.</remarks>
    /// <param name="deviceType">The device class to check.</param>
    /// <returns><see langword="true"/> if the type is registered; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">The type does not derive from <see cref="GcDevice"/> or does not implement <see cref="IDeviceFactory{T}"/>.</exception>
    public static bool IsRegistered(Type deviceType)
    {
        return _registeredDeviceClasses.ContainsKey(deviceType);
    }

    /// <summary>
    /// Closes library, by cleaning up resources set during initialization. To use the library again it needs to be re-initialized.
    /// </summary>
    public static void Close()
    {
        foreach (var deviceType in _registeredDeviceClasses.Values)
            Unregister(deviceType.DeviceType);

        IsInitialized = false;

        Logger.LogDebug("GcLib closed");
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Retrieves the factory method of a registered device class.
    /// </summary>
    /// <param name="deviceType">The registered device class.</param>
    /// <returns>Factory method creating devices of the class.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the device class is not registered.</exception>
    internal static Func<GcDeviceInfo, GcDevice> GetDeviceFactory(Type deviceType)
    {
        return _deviceFactories.TryGetValue(deviceType, out var factory) ? factory : throw new KeyNotFoundException($"Device class {deviceType.Name} is not registered!");
    }

    /// <summary>
    /// Retrieves the enumeration method of a registered device class.
    /// </summary>
    /// <param name="deviceType">The registered device class.</param>
    /// <returns>A method enumerating devices of the class.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the device class is not registered.</exception>
    internal static Func<List<GcDeviceInfo>> GetDeviceEnumerator(Type deviceType)
    {
        return _deviceEnumerators.TryGetValue(deviceType, out var enumerate) ? enumerate : throw new KeyNotFoundException($"Device class {deviceType.Name} is not registered!");
    }

    #endregion
}