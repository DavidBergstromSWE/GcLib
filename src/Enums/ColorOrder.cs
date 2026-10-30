namespace GcLib;

/// <summary>
/// Specifies the red, green, blue and alpha channel ordering used to interpret color component values in images.
/// </summary>
public enum ColorOrder
{
    /// <summary>
    /// Represents red-green-blue channel order.
    /// </summary>
    RGB,
    /// <summary>
    /// Represents blue-green-red channel order.
    /// </summary>
    BGR,
    /// <summary>
    /// Represents red-blue-green-alpha channel order.
    /// </summary>
    RGBa,
    /// <summary>
    /// Represents blue-green-red-alpha channel order.
    /// </summary>
    BGRa
}