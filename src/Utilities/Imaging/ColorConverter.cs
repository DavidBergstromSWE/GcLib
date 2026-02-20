using System;
using System.Drawing;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Color converter class, containing methods for converting between different color representations.
/// </summary>
public static partial class ColorConverter
{
    /// <summary>
    /// Converts <see cref="HSL"/> color representation to RGB color representation (based on <see href="https://geekymonkey.com/Programming/CSharp/RGB2HSL_HSL2RGB.htm"/>).
    /// </summary>
    /// <param name="color">Color representation with hue, saturation and lightness components.</param>
    /// <returns>Color (RGB struct) in range of 0-255.</returns>
    public static Color HSL2RGB(HSL color)
    {
        double v;
        double r, g, b; // RGB components
        double h = color.H;

        r = color.L;   // default to gray
        g = color.L;
        b = color.L;

        v = (color.L <= 0.5) ? (color.L * (1.0 + color.S)) : (color.L + color.S - color.L * color.S);
        if (v > 0)
        {
            double m;
            double sv;
            int sextant;
            double fract, vsf, mid1, mid2;

            m = color.L + color.L - v;
            sv = (v - m) / v;
            h *= 6.0;
            sextant = (int)h;
            fract = h - sextant;
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
    /// Converts RGB color representation to <see cref="HSL"/> color representation (based on <see href="https://geekymonkey.com/Programming/CSharp/RGB2HSL_HSL2RGB.htm"/>).
    /// </summary>
    /// <param name="rgb">Color (RGB Struct) in range of 0-255.</param>
    /// <returns>Color representation with hue, saturation and lightness components.</returns>
    public static HSL RGB2HSL(Color rgb)
    {
        // HSL components.
        double h = 0;
        double s = 0;
        double l = 0;

        double r = rgb.R / 255.0;
        double g = rgb.G / 255.0;
        double b = rgb.B / 255.0;
        double v;
        double m;
        double vm;
        double r2, g2, b2;

        v = Math.Max(r, g);
        v = Math.Max(v, b);
        m = Math.Min(r, g);
        m = Math.Min(m, b);
        l = (m + v) / 2.0;

        if (l <= 0.0)
            return new HSL(h, s, l);

        vm = v - m;
        s = vm;
        if (s > 0.0)
            s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);
        else
            return new HSL(h, s, l);

        r2 = (v - r) / vm;
        g2 = (v - g) / vm;
        b2 = (v - b) / vm;
        if (r == v)
            h = (g == m ? 5.0 + b2 : 1.0 - g2);
        else if (g == v)
            h = (b == m ? 1.0 + r2 : 3.0 - b2);
        else h = (r == m ? 3.0 + g2 : 5.0 - r2);
        h /= 6.0;

        return new HSL(h, s, l);
    }
}