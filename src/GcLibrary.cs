using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GcLib;

/// <summary>
/// Class library providing a common interface to control settings and streaming of GenICam/GenTL-standardized cameras, using underlying APIs from supported third-party camera manufacturers and library providers.
/// </summary>
public static class GcLibrary
{
    #region Fields

    /// <summary>
    /// Dictionary storing the device classes implemented in the library (as keys) and their class info (as values).
    /// </summary>
    private static readonly Dictionary<Type, GcDeviceClassInfo> _implementedDeviceClasses = [];

    /// <summary>
    /// Device classes available on the current system (may not be the same as the implemented, due to missing drivers etc).
    /// </summary>
    private static readonly List<GcDeviceClassInfo> _availableDeviceClasses = [];

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
    /// <exception cref="InvalidOperationException"></exception>
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
            Register<VirtualCam>();
            Register<PcoCam>();
            Register<PvCam>();
            Register<XiCam>();
            Register<SpinCam>();
            
            // add more device classes as they are implemented...
        }

        Logger.LogDebug("GcLib initialized");
    }

    /// <summary>
    /// Retrieves information about a specific <see cref="GcDevice"/>-derived subclass.
    /// </summary>
    /// <typeparam name="TDevice"><see cref="GcDevice"/>-derived type.</typeparam>
    /// <returns>Device class information.</returns>
    public static GcDeviceClassInfo GetDeviceClassInfo<TDevice>() where TDevice : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
    {
        return TDevice.DeviceClassInfo;
    }

    /// <summary>
    /// Registers a new device class to be used in the library.
    /// </summary>
    /// <typeparam name="TDevice">Device type.</typeparam>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void Register<TDevice>() where TDevice : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        if (_implementedDeviceClasses.ContainsKey(typeof(TDevice)))
            throw new ArgumentException("A device class of this type is already registered!", typeof(TDevice).Name);

        _implementedDeviceClasses.Add(typeof(TDevice), TDevice.DeviceClassInfo);

        try
        {
            // Add device class if devices can be enumerated (is there a better test of dll functionality?).
            TDevice.EnumerateDevices();

            // Add detected devices to list.
            _availableDeviceClasses.Add(_implementedDeviceClasses[typeof(TDevice)]);

            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("{DeviceType} registered ({API} v{Version})", _implementedDeviceClasses[typeof(TDevice)].DeviceType.Name, _implementedDeviceClasses[typeof(TDevice)].Name, _implementedDeviceClasses[typeof(TDevice)].Version);
        }
        catch (Exception ex)
        {
            // Log for debugging.
            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Unable to register device class of type {Name}", typeof(TDevice).Name);
        }
    }

    /// <summary>
    /// Unregister a device class from being used in the library.
    /// </summary>
    /// <typeparam name="TDevice">Device type.</typeparam>
    /// <exception cref="ArgumentException"></exception>
    public static void Unregister<TDevice>() where TDevice : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
    {
        if (IsInitialized == false)
            throw new InvalidOperationException("Library needs to be initialized before device registration/unregistration!");

        if (_implementedDeviceClasses.ContainsKey(typeof(TDevice)) == false)
            throw new ArgumentException("Device class is not registered!", typeof(TDevice).Name);

        if (_availableDeviceClasses.Contains(_implementedDeviceClasses[typeof(TDevice)]))
            _availableDeviceClasses.Remove(_implementedDeviceClasses[typeof(TDevice)]);
        _implementedDeviceClasses.Remove(typeof(TDevice));

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("{DeviceType} unregistered", typeof(TDevice).Name);
    }

    /// <summary>
    /// Retrieves device classes in the library which are available on the current system.
    /// </summary>
    /// <returns>Device classes available.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IReadOnlyCollection<GcDeviceClassInfo> GetAvailableDeviceClasses()
    {
        return IsInitialized ? _availableDeviceClasses : throw new InvalidOperationException("Library needs to be initialized before inquiring this info!");
    }

    /// <summary>
    /// Retrieves device classes registered in the library. 
    /// </summary>
    /// <remarks>Note: All of the returned classes may not be available on the current system, due to missing drivers, assemblies etc. To check available classes use <see cref="GetAvailableDeviceClasses"/>.</remarks>
    /// <returns>Device classes registered.</returns>
    public static IReadOnlyCollection<GcDeviceClassInfo> GetRegisteredDeviceClasses()
    {
        // throw if not initialized?
        return _implementedDeviceClasses.Values;
    }

    /// <summary>
    /// Closes library, by cleaning up resources set during initialization. To use the library again it needs to be re-initialized.
    /// </summary>
    public static void Close()
    {
        _availableDeviceClasses.Clear();
        _implementedDeviceClasses.Clear();

        IsInitialized = false;

        Logger.LogDebug("GcLib closed");
    }

    #endregion
}