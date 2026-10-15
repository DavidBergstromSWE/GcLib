using System;
using System.Collections.Generic;
using System.Linq;
using Basler.Pylon;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to pylon .NET API from Basler.
/// </summary>
public sealed partial class PylonCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    /// <summary>
    /// Camera device.
    /// </summary>
    private readonly Camera _camera;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance and opens a new camera, using an optional string identifier.
    /// </summary>
    /// <remarks><see cref="ArgumentException"/> will be thrown if camera is not found.</remarks>
    /// <param name="deviceID">(Optional) Unique string identifier for device (full name or serial number).</param>
    /// <exception cref="ArgumentException"></exception>
    public PylonCam(string deviceID = "") : base()
    {
        var cameraInfos = CameraFinder.Enumerate();

        ICameraInfo info;
        if (string.IsNullOrEmpty(deviceID))
            info = cameraInfos.FirstOrDefault(); // Get first available device.
        else
            info = cameraInfos.FirstOrDefault(i => GetUniqueID(i) == deviceID || i.GetValueOrDefault(CameraInfoKey.SerialNumber, string.Empty) == deviceID); // Get specific device.

        if (info == null)
            throw new ArgumentException("No camera found!");

        try
        {
            _camera = new Camera(info);
            _camera.ConnectionLost += OnCameraConnectionLost;
            _camera.Open();
        }
        catch (Exception ex)
        {
            _camera?.Dispose();
            throw new ArgumentException("Unable to open camera!", ex);
        }

        // Update device info.
        DeviceInfo = GetDeviceInfo(info);
        DeviceInfo.IsOpen = true;
        DeviceInfo.IsAccessible = false;

        // Retrieve collection of camera parameters.
        Parameters = ImportParameters(useReflection: false);
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void Close()
    {
        base.Close();

        // Stop acquisition (if running).
        if (IsAcquiring)
            StopAcquisition();

        _camera.ConnectionLost -= OnCameraConnectionLost;

        if (_camera.IsOpen)
            _camera.Close();

        _camera.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Retrieve top-level info about camera device.
    /// </summary>
    /// <param name="info">Camera info.</param>
    /// <returns>Top-level info about device.</returns>
    private static GcDeviceInfo GetDeviceInfo(ICameraInfo info)
    {
        return new GcDeviceInfo(vendorName: info.GetValueOrDefault(CameraInfoKey.VendorName, "Basler"),
                                modelName: info.GetValueOrDefault(CameraInfoKey.ModelName, string.Empty),
                                serialNumber: info.GetValueOrDefault(CameraInfoKey.SerialNumber, string.Empty),
                                uniqueID: GetUniqueID(info),
                                deviceClass: DeviceClassInfo,
                                userDefinedName: info.GetValueOrDefault(CameraInfoKey.UserDefinedName, string.Empty));
    }

    /// <summary>
    /// Retrieves a unique identifier of the device (full name, which includes the transport layer and the serial number).
    /// </summary>
    private static string GetUniqueID(ICameraInfo info)
    {
        return info.GetValueOrDefault(CameraInfoKey.FullName, info.GetValueOrDefault(CameraInfoKey.SerialNumber, string.Empty));
    }

    /// <summary>
    /// Raises the <see cref="GcDevice.ConnectionLost"/> event when the connection to the camera is lost.
    /// </summary>
    private void OnCameraConnectionLost(object sender, EventArgs e)
    {
        OnConnectionLost();
    }

    #endregion

    #region IDeviceEnumerator

    /// <inheritdoc/>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        Environment.SetEnvironmentVariable("PYLON_CAMEMU", "1"); // Enable camera emulation (for testing purposes).
        var cameraInfos = CameraFinder.Enumerate(DeviceType.CameraEmulator); // Enumerate all available camera emulators (for testing purposes).

        //var cameraInfos = CameraFinder.Enumerate();

        return [.. cameraInfos.Select(GetDeviceInfo)];
    }

    #endregion

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new GcDeviceClassInfo("Pylon", Library.VersionInfo.ToString(), typeof(PylonCam));

    #endregion
}

internal static class CameraInfoExtensions
{
    /// <summary>
    /// Gets the value of a camera info key, or a default value if the key is not available.
    /// </summary>
    public static string GetValueOrDefault(this ICameraInfo info, string key, string defaultValue)
    {
        return info.ContainsKey(key) ? info[key] : defaultValue;
    }
}
