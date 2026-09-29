using System;

namespace GcLib;

/// <summary>
/// Provides top-level information about a device, including vendor name, model name, serial number, unique string identifier, device class and whether device is accessible or already opened/connected.
/// </summary>
/// <remarks>
/// Create top-level information about device.
/// </remarks>
/// <param name="vendorName">Name of camera manufacturer.</param>
/// <param name="modelName">Name of camera model. </param>
/// <param name="serialNumber">Serial number of camera.</param>
/// <param name="uniqueID">Unique string identifier for the camera.</param>
/// <param name="deviceClass">Camera device class type.</param>
/// <param name="isAccessible">Indicates if camera is accessible or not.</param>
/// <param name="isOpen">Indicates if camera is connected or not.</param>
public sealed class GcDeviceInfo(string vendorName, string modelName, string serialNumber, string uniqueID, GcDeviceClassInfo deviceClass, string userDefinedName = "", bool isAccessible = true, bool isOpen = false) : IEquatable<GcDeviceInfo>
{
    #region Properties

    /// <summary>
    /// Name of camera manufacturer.
    /// </summary>
    public string VendorName { get; } = vendorName;

    /// <summary>
    /// Name of camera model. 
    /// </summary>
    public string ModelName { get; } = modelName;

    /// <summary>
    /// Serial number of camera.
    /// </summary>
    public string SerialNumber { get; } = serialNumber;

    /// <summary>
    /// Unique string identifier for the camera.
    /// </summary>
    public string UniqueID { get; } = uniqueID;

    /// <summary>
    /// User defined name for the camera.
    /// </summary>
    public string UserDefinedName { get; } = userDefinedName;

    /// <summary>
    /// Device class info.
    /// </summary>
    public GcDeviceClassInfo DeviceClassInfo { get; } = deviceClass;

    /// <summary>
    /// Flag indicating if camera is accessible. Inaccessability may be an indication of camera already being connected/used or camera/SDK licensing issues.
    /// </summary>
    public bool IsAccessible { get; set; } = isAccessible;

    /// <summary>
    /// Flag indicating if camera is connected.
    /// </summary>
    public bool IsOpen { get; set; } = isOpen;

    #endregion

    #region Equality members

    public override bool Equals(object obj)
    {
        return obj is GcDeviceInfo dInfo
               && VendorName.Equals(dInfo.VendorName, StringComparison.OrdinalIgnoreCase)
               && ModelName.Equals(dInfo.ModelName, StringComparison.OrdinalIgnoreCase)
               && SerialNumber.Equals(dInfo.SerialNumber, StringComparison.OrdinalIgnoreCase)
               && UniqueID.Equals(dInfo.UniqueID, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(GcDeviceInfo other)
    {
        return VendorName.Equals(other.VendorName, StringComparison.OrdinalIgnoreCase)
               && ModelName.Equals(other.ModelName, StringComparison.OrdinalIgnoreCase)
               && SerialNumber.Equals(other.SerialNumber, StringComparison.OrdinalIgnoreCase)
               && UniqueID.Equals(other.UniqueID, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return VendorName.GetHashCode(StringComparison.OrdinalIgnoreCase)
               ^ ModelName.GetHashCode(StringComparison.OrdinalIgnoreCase)
               ^ SerialNumber.GetHashCode(StringComparison.OrdinalIgnoreCase)
               ^ UniqueID.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}