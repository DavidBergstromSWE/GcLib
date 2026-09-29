namespace GcLib;

/// <summary>
/// Location for temperature measurement within camera.
/// </summary>
public enum DeviceTemperatureSelector
{
    /// <summary>
    /// Temperature of the image sensor of the camera.
    /// </summary>
    Sensor = 0,
    /// <summary>
    /// Temperature of the device's mainboard.
    /// </summary>
    Mainboard,
    /// <summary>
    /// Device-specific temperature location.
    /// </summary>
    DeviceSpecific
}
