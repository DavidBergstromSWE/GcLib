using System.Collections.Generic;

namespace GcLib;

/// <summary>
/// Virtual device class, for a software simulation of a physical camera.
/// </summary>
public sealed partial class VirtualCam : GcDevice, IDeviceFactory<VirtualCam>
{
    #region Fields

    /// <summary>
    /// GenApi module attached to camera.
    /// </summary>
    private readonly GenApi _genApi;

    #endregion

    #region Properties

    /// <summary>
    /// Time delay (in milliseconds) during connection to simulate hardware operations.
    /// </summary>
    public static int DeviceConnectionDelay { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of devices that will be discovered when <see cref="EnumerateDevices"/> is called.
    /// </summary>
    public static uint NumberOfAvailableDevices { get; set; } = 2;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    public VirtualCam(string deviceID = "") : base()
    {
        // Update device info.
        DeviceInfo = GetDeviceInfo(deviceID);
        DeviceInfo.IsOpen = true;
        DeviceInfo.IsAccessible = false;

        // Initialize GenApi module.
        _genApi = new GenApi(this);

        // Initialize properties.
        BufferCapacity = 1;

        // Retrieve collection of camera parameters.
        Parameters = ImportParameters();

        // Add time delay to simulate hardware operations.
        System.Threading.Tasks.Task.Delay(DeviceConnectionDelay).Wait();
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void Close()
    {
        _genApi?.Dispose();

        base.Close();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Retrieve top-level info about device.
    /// </summary>
    /// <param name="uniqueID">Unique string identifier of device.</param>
    /// <returns>Device information.</returns>
    private static GcDeviceInfo GetDeviceInfo(string uniqueID)
    {
        return new GcDeviceInfo(vendorName: "MySimLabs",
                                modelName: nameof(VirtualCam),
                                serialNumber: "1.0",
                                uniqueID: uniqueID,
                                userDefinedName: uniqueID,
                                deviceClass: DeviceClassInfo);
    }

    #endregion

    #region IDeviceFactory

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new(nameof(VirtualCam), "1.0", typeof(VirtualCam));

    /// <inheritdoc/>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        // Create available devices.
        var devices = new List<GcDeviceInfo>();
        for (uint i = 1; i <= NumberOfAvailableDevices; i++)
            devices.Add(GetDeviceInfo($"VirtualCam{i}"));

        return devices;
    }

    /// <inheritdoc/>
    public static VirtualCam CreateDevice(GcDeviceInfo deviceInfo)
    {
        return new VirtualCam(deviceInfo.UniqueID);
    }

    /// <inheritdoc/>
    public static void Dispose()
    {
        // No specific cleanup required for VirtualCam.
    }

    #endregion
}