namespace GcLib.Utilities.Imaging;

/// <summary>
/// Specifies the channel ordering used to interpret color component values.
/// </summary>
/// <remarks>Use RGB for red-green-blue ordering and BGR for blue-green-red ordering.</remarks>
public enum ColorOrder
{
    /// <summary>
    /// Represents red-green-blue channel order.
    /// </summary>
    RGB,
    /// <summary>
    /// Represents blue-green-red channel order.
    /// </summary>
    BGR
}