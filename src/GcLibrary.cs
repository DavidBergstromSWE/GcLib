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

    /// <summary>
    /// Gets current version of <see cref="GcLibrary"/>.
    /// </summary>
    public static string Version => typeof(GcLibrary).Assembly.GetName().Version.ToString();

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
    /// Retrieves information about a registered device class.
    /// </summary>
    /// <typeparam name="TDevice"><see cref="GcDevice"/>-derived type.</typeparam>
    /// <returns>Device class information.</returns>
    public static GcDeviceClassInfo GetDeviceClassInfo<TDevice>() where TDevice : GcDevice<TDevice>, IDeviceFactory<TDevice>
    {
        if (IsRegistered<TDevice>() == false)
            throw new ArgumentException("Device class is not registered!", typeof(TDevice).Name);
        return TDevice.DeviceClassInfo;
    }

    /// <summary>
    /// Registers a new device class to be used in the library.
    /// </summary>
    /// <remarks>If registration fails (due to missing dlls or other issues), the device class will be unavailable to use in the library. If logging is enabled, a log message will be written explaining the failure.</remarks>
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
            // Initialize device class, which may include loading and initializing the underlying API.
            TDevice.Initialize();

            // Validate that the device class info matches the device type.
            var deviceClassInfo = TDevice.DeviceClassInfo;
            if (deviceClassInfo.DeviceType != typeof(TDevice))
                throw new InvalidOperationException($"Device class info for {typeof(TDevice).Name} identifies {deviceClassInfo.DeviceType.Name}.");

            // Add device class if devices can be enumerated (is there a better test of dll functionality?).
            TDevice.EnumerateDevices();

            // Add detected devices to list.
            _registeredDeviceClasses.Add(typeof(TDevice), deviceClassInfo);

            // Add factory and enumeration methods for this device class.
            _deviceFactories.Add(typeof(TDevice), TDevice.CreateDevice);
            _deviceEnumerators.Add(typeof(TDevice), TDevice.EnumerateDevices);

            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("{DeviceType} registered ({API} v{Version})", deviceClassInfo.DeviceType.Name, deviceClassInfo.Name, deviceClassInfo.Version);
        }
        catch (Exception ex)
        {
            try
            {
                // Rollback registration if initialization fails.
                TDevice.Release(); // Release resources used by API.
            }
            catch (Exception cleanupException)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning(cleanupException, "Failed to release resources for {DeviceType} after registration failed", typeof(TDevice).Name);
            }

            // Log for debugging.
            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Unable to register device class of type {Name}", typeof(TDevice).Name);
        }
    }

    /// <summary>
    /// Registers a new device class to be used in the library.
    /// </summary>
    /// <param name="deviceType">The type of the device class to register.</param>
    /// <remarks>If registration fails (due to missing dlls or other issues), the device class will be unavailable to use in the library. If logging is enabled, a log message will be written explaining the failure.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the library is not initialized.</exception>
    /// <exception cref="ArgumentException">Thrown if the device class is already registered or the type is invalid.</exception>
    public static void Register(Type deviceType)
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        // Validate that the provided type is a subclass of GcDevice<TDevice> and implements IDeviceFactory<TDevice>.
        if (IsDeviceType(deviceType) == false)
            throw new ArgumentException($"The provided type must be a subclass of {nameof(GcDevice<>) } and implement {nameof(IDeviceFactory<>)}.", nameof(deviceType));
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

        // Validate that the provided type is a subclass of GcDevice<TDevice> and implements IDeviceFactory<TDevice>.
        if (IsDeviceType(deviceType) == false)
            throw new ArgumentException($"The provided type must be a subclass of {nameof(GcDevice<>)} and implement {nameof(IDeviceFactory<>)}.", nameof(deviceType));

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
    /// Gets all valid device classes implemented in the currently loaded assemblies.
    /// </summary>
    /// <returns>A list of valid device classes.</returns>
    public static List<Type> GetImplementedDeviceClasses()
    {
        return [.. AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(assembly => { try { return assembly.GetTypes(); } catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }}) // Return only types that were successfully loaded
                                          .Where(IsDeviceType)]; // Return only the types that are valid device types
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
    /// Closes library, by unregistering all device classes and cleaning up resources set during initialization.
    /// </summary>
    /// <remarks>After calling this method, the library needs to be re-initialized by calling <see cref="Init"/> again.</remarks>
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
    /// <returns>Factory method that can be used to create devices of the class.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the device class is not registered.</exception>
    internal static Func<GcDeviceInfo, GcDevice> GetDeviceFactory(Type deviceType)
    {
        return _deviceFactories.TryGetValue(deviceType, out var factory) ? factory : throw new KeyNotFoundException($"Device class {deviceType.Name} is not registered!");
    }

    /// <summary>
    /// Retrieves the enumeration method of a registered device class.
    /// </summary>
    /// <param name="deviceType">The registered device class.</param>
    /// <returns>Enumeration method that can be used to discover and list devices of the class.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the device class is not registered.</exception>
    internal static Func<List<GcDeviceInfo>> GetDeviceEnumerator(Type deviceType)
    {
        return _deviceEnumerators.TryGetValue(deviceType, out var enumerate) ? enumerate : throw new KeyNotFoundException($"Device class {deviceType.Name} is not registered!");
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Helper method that determines whether a <paramref name="type"/> is a valid device type.
    /// </summary>
    /// <remarks>A valid device type is a concrete (non-abstract) type that derives from <see cref="GcDevice{TDevice}"/> and implements <see cref="IDeviceFactory{TDevice}"/>.</remarks>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> is a valid device type; otherwise <see langword="false"/>.</returns>
    private static bool IsDeviceType(Type type)
    {
        // Abstract classes cannot be instantiated, so they cannot be valid device types.
        return type.IsAbstract == false && GcDeviceClassInfo.IsDeviceType(type);
    }

    #endregion
}