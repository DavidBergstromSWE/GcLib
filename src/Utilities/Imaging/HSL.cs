using System;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Color representation with hue, saturation and lightness components.
/// </summary>
public readonly struct HSL
{
    /// <summary>
    /// Hue, in range 0-1.
    /// </summary>
    public readonly double H;
    /// <summary>
    /// Saturation, in range 0-1.
    /// </summary>
    public readonly double S;
    /// <summary>
    /// Lightness (luminance), in range 0-1.
    /// </summary>
    public readonly double L;

    public HSL(double hue, double saturation, double lightness)
    {
        if (hue >= 0.0 & hue <= 1.0)
            H = hue;
        else throw new ArgumentException($"Hue must be in the range 0 - 1! (it was {hue})");
        if (saturation >= 0.0 & saturation <= 1.0)
            S = saturation;
        else throw new ArgumentException($"Saturation must be in the range 0 - 1! (it was {saturation})");
        if (lightness >= 0.0 & lightness <= 1.0)
            L = lightness;
        else throw new ArgumentException($"Lightness must be in the range 0 - 1! (it was {lightness})");
    }
}