using GcLib;

namespace FusionViewer;

/// <summary>
/// Brief top-level description of a device.
/// </summary>
/// <param name="VendorName"> Vendor name of device. </param>
/// <param name="ModelName"> Model name of device. </param>
/// <param name="UniqueID"> Unique ID of device. </param>
internal readonly record struct DeviceInfo(string VendorName, string ModelName, string UniqueID)
{
    /// <summary>
    /// Creates a brief top-level description of a device using provided detailed device info.
    /// </summary>
    /// <param name="deviceInfo">Top-level information about device.</param>
    public DeviceInfo(GcDeviceInfo deviceInfo) : this(deviceInfo.VendorName, deviceInfo.ModelName, deviceInfo.UniqueID) {}
}