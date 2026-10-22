using System;
using System.Collections.Generic;
using System.Linq;
using VmbNET;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to Allied Vision's Vimba X SDK.
/// </summary>
public sealed partial class VimbaCam : GcDevice, IDeviceFactory<VimbaCam>
{
    #region Fields

    /// <summary>
    /// Provides access to camera operations through an <see cref="IOpenCamera"/> implementation.
    /// </summary>
    private readonly IOpenCamera _camera;

    /// <summary>
    /// The capacity of the buffer.
    /// </summary>
    private uint _bufferCapacity = 4;

    /// <summary>
    /// Stores the shared Vmb system instance.
    /// </summary>
    private static IVmbSystem _vmbSystem;

    #endregion

    #region Constructors

    /// <summary>
    /// Opens an Allied Vision camera identified by its VmbNET ID or serial number.
    /// </summary>
    /// <param name="deviceID">Optional unique camera ID or serial number.</param>
    public VimbaCam(string deviceID = "") : base()
    {
        // Initialize Vmb system if not already done.
        var cameraInfo = _vmbSystem.GetCameras().FirstOrDefault(camera => string.IsNullOrEmpty(deviceID) // If no device ID is provided, select the first available camera.
                                                                          || string.Equals(camera.Id, deviceID, StringComparison.OrdinalIgnoreCase) // If a device ID is provided, match it against the camera's VmbNET ID.
                                                                          || string.Equals(camera.Serial, deviceID, StringComparison.OrdinalIgnoreCase)) // If a device ID is provided, match it against the camera's serial number.
            ?? throw new ArgumentException("No camera found!", nameof(deviceID)); // Throw an exception if no matching camera is found.

        // Open the camera.
        _camera = cameraInfo.Open();

        // Update device info.
        DeviceInfo = GetDeviceInfo(cameraInfo);
        DeviceInfo.IsOpen = true;
        DeviceInfo.IsAccessible = false;

        // Import parameters from the camera.
        Parameters = ImportParameters();
    }

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override uint PayloadSize => _camera.Stream.PayloadSize;

    /// <inheritdoc/>
    public override uint BufferCapacity
    {
        get => _bufferCapacity;
        set
        {
            if (IsAcquiring)
                throw new InvalidOperationException("Unable to change buffer capacity while acquiring!");

            _bufferCapacity = Math.Max(value, 1);
        }
    }

    #endregion

    #region Protected methods

    /// <inheritdoc/>
    protected override void OnDeviceClosed()
    {
        if (IsAcquiring)
            StopAcquisition();

        // Dispose unmanaged resources of the camera.
        _camera?.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Creates top-level device information from the specified camera metadata.
    /// </summary>
    /// <param name="camera">Camera metadata.</param>
    /// <returns>A populated <see cref="GcDeviceInfo"/> for the camera, including whether full or exclusive access is available.</returns>
    private static GcDeviceInfo GetDeviceInfo(ICameraInfo camera)
    {
        bool isAccessible = camera.AccessModes.Contains(ICameraInfo.AccessModeValue.Full)
                            || camera.AccessModes.Contains(ICameraInfo.AccessModeValue.Exclusive);

        return new GcDeviceInfo(vendorName: "Allied Vision",
                                modelName: camera.ModelName ?? string.Empty,
                                serialNumber: camera.Serial ?? string.Empty,
                                uniqueID: camera.Id ?? string.Empty,
                                userDefinedName: camera.Name ?? string.Empty,
                                deviceClass: DeviceClassInfo,
                                isAccessible: isAccessible);
    }

    #endregion

    #region IDeviceFactory

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new("VmbNET", IVmbSystem.VmbCVersion, typeof(VimbaCam));

    /// <inheritdoc/>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        // Initialize the Vmb system if it hasn't been initialized yet.
        _vmbSystem = IVmbSystem.Startup();

        // Enumerate all discovered cameras and return their device info.
        return [.. _vmbSystem.GetCameras().Select(GetDeviceInfo)];
    }

    /// <inheritdoc/>
    public static void Release()
    {
        // Release unmanaged resources of the API.
        _vmbSystem?.Dispose();
        _vmbSystem?.Shutdown();
    }

    /// <inheritdoc/>
    public static VimbaCam CreateDevice(GcDeviceInfo deviceInfo)
    {
        return new VimbaCam(deviceInfo.UniqueID);
    }

    #endregion
}
