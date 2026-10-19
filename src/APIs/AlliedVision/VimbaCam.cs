using System;
using System.Collections.Generic;
using System.Linq;
using VmbNET;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to Allied Vision's Vimba X SDK.
/// </summary>
public sealed partial class VimbaCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    private readonly IVmbSystem _vmbSystem;
    private readonly IOpenCamera _camera;
    private uint _bufferCapacity = 4;

    /// <summary>
    /// Opens an Allied Vision camera identified by its VmbNET ID or serial number.
    /// </summary>
    /// <param name="deviceID">Optional unique camera ID or serial number.</param>
    public VimbaCam(string deviceID = "") : base()
    {
        _vmbSystem = IVmbSystem.Startup();
        IOpenCamera camera = null;

        try
        {
            var cameraInfo = _vmbSystem.GetCameras().FirstOrDefault(camera =>
                string.IsNullOrEmpty(deviceID)
                || string.Equals(camera.Id, deviceID, StringComparison.OrdinalIgnoreCase)
                || string.Equals(camera.Serial, deviceID, StringComparison.OrdinalIgnoreCase));

            if (cameraInfo == null)
                throw new ArgumentException("No camera found!", nameof(deviceID));

            camera = cameraInfo.Open();
            _camera = camera;
            DeviceInfo = GetDeviceInfo(cameraInfo);
            DeviceInfo.IsOpen = true;
            DeviceInfo.IsAccessible = false;

            Parameters = ImportParameters();
        }
        catch
        {
            try
            {
                camera?.Dispose();
            }
            finally
            {
                _vmbSystem.Dispose();
            }
            throw;
        }
    }

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

    /// <inheritdoc/>
    public override void Close()
    {
        if (IsAcquiring)
            StopAcquisition();

        base.Close();
        try
        {
            _camera.Dispose();
        }
        finally
        {
            _vmbSystem.Dispose();
        }
    }

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new("VmbNET", IVmbSystem.VmbCVersion, typeof(VimbaCam));

    /// <inheritdoc/>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        using var vmbSystem = IVmbSystem.Startup();
        return [.. vmbSystem.GetCameras().Select(GetDeviceInfo)];
    }

    private static GcDeviceInfo GetDeviceInfo(ICameraInfo camera)
    {
        bool isAccessible = camera.AccessModes.Contains(ICameraInfo.AccessModeValue.Full)
                            || camera.AccessModes.Contains(ICameraInfo.AccessModeValue.Exclusive);

        return new GcDeviceInfo(vendorName: "Allied Vision",
                                modelName: camera.ModelName ?? string.Empty,
                                serialNumber: camera.Serial ?? string.Empty,
                                uniqueID: camera.Id,
                                userDefinedName: camera.Name ?? string.Empty,
                                deviceClass: DeviceClassInfo,
                                isAccessible: isAccessible);
    }
}
