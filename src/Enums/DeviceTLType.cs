namespace GcLib;

/// <summary>
/// Transport layer type of the device.
/// </summary>
public enum DeviceTLType
{
    /// <summary>
    /// GigE Vision.
    /// </summary>
    GigEVision,
    /// <summary>
    /// Camera Link.
    /// </summary>
    CameraLink,
    /// <summary>
    /// Camera Link High Speed.
    /// </summary>
    CameraLinkHS,
    /// <summary>
    /// CoaXPress.
    /// </summary>
    CoaXPress,
    /// <summary>
    /// USB3 Vision.
    /// </summary>
    USB3Vision,
    /// <summary>
    /// USB.
    /// </summary>
    USB,
    /// <summary>
    /// Custom Transport Layer.
    /// </summary>
    Custom
}
