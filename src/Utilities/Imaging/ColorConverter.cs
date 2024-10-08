using System;
using System.Drawing;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Color converter class, containing methods for converting between different color representations.
/// </summary>
public static class ColorConverter
{
    /// <summary>
    /// Converts HSL color representation to RGB color representation (based on <see href="https://geekymonkey.com/Programming/CSharp/RGB2HSL_HSL2RGB.htm"/>).
    /// </summary>
    /// <param name="hue">Hue (0-1).</param>
    /// <param name="sat">Saturation (0-1).</param>
    /// <param name="lum">Luminosity (0-1).</param>
    /// <returns>Color (RGB struct) in range of 0-255.</returns>
    public static Color HSL2RGB(double hue, double sat, double lum)
    {
        double v;
        double r, g, b;

        r = lum;   // default to gray
        g = lum;
        b = lum;

        v = (lum <= 0.5) ? (lum * (1.0 + sat)) : (lum + sat - lum * sat);
        if (v > 0)
        {
            double m;
            double sv;
            int sextant;
            double fract, vsf, mid1, mid2;

            m = lum + lum - v;
            sv = (v - m) / v;
            hue *= 6.0;
            sextant = (int)hue;
            fract = hue - sextant;
            vsf = v * sv * fract;
            mid1 = m + vsf;
            mid2 = v - vsf;

            switch (sextant)
            {
                case 0:
                    r = v;
                    g = mid1;
                    b = m;
                    break;

                case 1:
                    r = mid2;
                    g = v;
                    b = m;
                    break;

                case 2:
                    r = m;
                    g = v;
                    b = mid1;
                    break;

                case 3:
                    r = m;
                    g = mid2;
                    b = v;
                    break;

                case 4:
                    r = mid1;
                    g = m;
                    b = v;
                    break;

                case 5:
                    r = v;
                    g = m;
                    b = mid2;
                    break;
            }
        }

        return Color.FromArgb(Convert.ToByte(r * 255.0f), Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f));
    }

    /// <summary>
    /// Converts RGB color representation to HSL color representation (based on <see href="https://geekymonkey.com/Programming/CSharp/RGB2HSL_HSL2RGB.htm"/>).
    /// </summary>
    /// <param name="rgb">Color (RGB Struct) in range of 0-255.</param>
    /// <param name="hue">Hue (0-1).</param>
    /// <param name="sat">Saturation (0-1).</param>
    /// <param name="lum">Luminosity (0-1).</param>
    public static void RGB2HSL(Color rgb, out double hue, out double sat, out double lum)
    {
        double r = rgb.R / 255.0;
        double g = rgb.G / 255.0;
        double b = rgb.B / 255.0;
        double v;
        double m;
        double vm;
        double r2, g2, b2;

        hue = 0; // default to black
        sat = 0;

        v = Math.Max(r, g);
        v = Math.Max(v, b);
        m = Math.Min(r, g);
        m = Math.Min(m, b);
        lum = (m + v) / 2.0;

        if (lum <= 0.0)
            return;

        vm = v - m;
        sat = vm;
        if (sat > 0.0)
            sat /= (lum <= 0.5) ? (v + m) : (2.0 - v - m);
        else
            return;

        r2 = (v - r) / vm;
        g2 = (v - g) / vm;
        b2 = (v - b) / vm;
        if (r == v)
            hue = (g == m ? 5.0 + b2 : 1.0 - g2);
        else if (g == v)
            hue = (b == m ? 1.0 + r2 : 3.0 - b2);
        else hue = (r == m ? 3.0 + g2 : 5.0 - r2);
        hue /= 6.0;
    }
}