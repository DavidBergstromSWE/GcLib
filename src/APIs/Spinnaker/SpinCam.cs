using System;
using System.Collections.Generic;
using GcLib.Utilities.IO;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to Spinnaker.NET from Teledyne FLIR.
/// </summary>
public sealed partial class SpinCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    /// <summary>
    /// Camera device.
    /// </summary>
    private readonly IManagedCamera _camera;

    /// <summary>
    /// Node map of the camera, containing features implemented by the device.
    /// </summary>
    private readonly INodeMap _nodeMap;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance and opens a new camera, using an optional string identifier.
    /// </summary>
    /// <remarks><see cref="ArgumentException"/> will be thrown if camera is not found.</remarks>
    /// <param name="deviceID">(Optional) Unique string identifier for device.</param>
    /// <exception cref="ArgumentException"></exception>
    public SpinCam(string deviceID = "") : base()
    {
        using var system = new ManagedSystem();
        var cameraList = system.GetCameras();

        if (string.IsNullOrEmpty(deviceID) && cameraList.Count > 0)
            _camera = cameraList[0]; // Get first available device.
        else _camera = cameraList.GetByUniqueID(deviceID); // Get specific device.

        if (_camera == null)
            throw new ArgumentException("No camera found!");

        // Update device info.
        DeviceInfo = GetDeviceInfo(_camera);
        DeviceInfo.IsOpen = true;
        DeviceInfo.IsAccessible = false;

        // Initialize camera and retrieve node map.
        _nodeMap = _camera.Init();

        // Retrieve collection of camera parameters.
        Parameters = ImportParameters();
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void Close()
    {
        base.Close();

        // Stop acquisition (if running).
        if (_camera.IsStreaming())
            _camera.EndAcquisition();

        // De-initialize camera.
        _camera.DeInit();

        _camera.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Retrieve top-level info about camera device.
    /// </summary>
    /// <param name="camera">Camera.</param>
    /// <returns>Top-level info about device.</returns>
    private static GcDeviceInfo GetDeviceInfo(IManagedCamera camera)
    {
        return new GcDeviceInfo(vendorName: camera.TLDevice.DeviceVendorName,
                                modelName: camera.TLDevice.DeviceModelName,
                                serialNumber: camera.TLDevice.DeviceSerialNumber,
                                uniqueID: camera.TLDevice.DeviceID,
                                deviceClass: DeviceClassInfo,
                                userDefinedName: camera.TLDevice.DeviceUserID);
    }

    #endregion

    #region IDeviceEnumerator

    /// <inheritdoc/>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        var deviceList = new List<GcDeviceInfo>();

        using var system = new ManagedSystem();
        var cameraList = system.GetCameras();
        foreach (var camera in cameraList)
            deviceList.Add(GetDeviceInfo(camera));

        return deviceList;
    }

    #endregion

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new GcDeviceClassInfo("SpinnakerNet", FileHelper.GetAssemblyFileVersion("SpinnakerNET_v140.dll"), typeof(SpinCam));

    #endregion
}