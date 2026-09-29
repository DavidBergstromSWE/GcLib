namespace GcLib;

/// <summary>
/// Type of Bayer color filter array applied to an image.
/// </summary>
public enum PixelColorFilter
{
    /// <summary>
    /// No color filter array applied.
    /// </summary>
    None = 0,
    /// <summary>
    /// Pixels filtered to red (0,0), green (0,1), green (1,0) and blue (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Red Green</item>
    /// <item>Green Blue</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerRGGB,
    /// <summary>
    /// Pixels filtered to green (0,0), blue (0,1), red (1,0) and green (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Green Blue</item>
    /// <item>Red Green</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerGBRG,
    /// <summary>
    /// Pixels filtered to green (0,0), red (0,1), blue (1,0) and green (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Green Red</item>
    /// <item>Blue Green</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerGRBG,
    /// <summary>
    /// Pixels filtered to blue (0,0), green (0,1), green (1,0) and red (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Blue Green</item>
    /// <item>Green Red</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerBGGR
}
