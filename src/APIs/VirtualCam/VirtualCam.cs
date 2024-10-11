using System.Collections.Generic;
using System.Reflection;

namespace GcLib;

/// <summary>
/// Virtual device class, for a software simulation of a physical camera.
/// </summary>
public sealed partial class VirtualCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    /// <summary>
    /// Time delay (in milliseconds) during connection to simulate hardware operations.
    /// </summary>
    public const int DeviceConnectionDelay = 0;

    /// <summary>
    /// GenApi module attached to camera.
    /// </summary>
    private readonly GenApi _genApi;

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

    #region IDeviceEnumerator

    /// <summary>
    /// Enumerates and returns a list of available devices of type <see cref="VirtualCam"/>.
    /// </summary>
    /// <returns>List of available devices.</returns>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        // Create two available devices.
        return [GetDeviceInfo("VirtualCam1"), GetDeviceInfo("VirtualCam2")];
    }

    #endregion

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new(nameof(VirtualCam), Assembly.GetExecutingAssembly().GetName().Version.ToString(), typeof(VirtualCam));

    #endregion
}