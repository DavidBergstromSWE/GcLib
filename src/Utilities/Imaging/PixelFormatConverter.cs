using System;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Utility class for handling pixel formats conversions.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class PixelFormatConverter
{
    /// <summary>
    /// Converts GeniCam pixel format to <see cref="System.Drawing.Imaging.PixelFormat"/> (used in Windows GDI+ API).
    /// </summary>
    /// <param name="pixelFormat">Pixel format in GenICam PFNC.</param>
    /// <returns>Converted format.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static System.Drawing.Imaging.PixelFormat GetDrawingPixelFormat(PixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.Mono8 => System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
            PixelFormat.Mono16 or PixelFormat.Mono14 => System.Drawing.Imaging.PixelFormat.Format16bppGrayScale,
            PixelFormat.RGB8 or PixelFormat.BGR8 => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
            PixelFormat.BGRa8 or PixelFormat.RGBa8 => System.Drawing.Imaging.PixelFormat.Format32bppArgb,
            PixelFormat.RGB16 or PixelFormat.BGR16 => System.Drawing.Imaging.PixelFormat.Format48bppRgb,
            PixelFormat.RGBa16 or PixelFormat.BGRa16 => System.Drawing.Imaging.PixelFormat.Format64bppArgb,
            _ => throw new NotSupportedException("Pixelformat is not supported!"),
        };
    }

    /// <summary>
    /// Converts <see cref="System.Drawing.Imaging.PixelFormat"/> (used in Windows GDI+ API) to GenICam pixel format.
    /// </summary>
    /// <param name="pixelFormat">Pixel format in <see cref="System.Drawing.Imaging"/> namespace.</param>
    /// <returns>Converted format.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static PixelFormat GetPixelFormat(System.Drawing.Imaging.PixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            System.Drawing.Imaging.PixelFormat.Format1bppIndexed => PixelFormat.Mono1p,
            System.Drawing.Imaging.PixelFormat.Format4bppIndexed => PixelFormat.Mono4p,
            System.Drawing.Imaging.PixelFormat.Format8bppIndexed => PixelFormat.Mono8,
            System.Drawing.Imaging.PixelFormat.Format16bppGrayScale => PixelFormat.Mono16,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb or System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormat.RGB8,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormat.RGBa8,
            System.Drawing.Imaging.PixelFormat.Format48bppRgb => PixelFormat.RGB16,
            System.Drawing.Imaging.PixelFormat.Format64bppArgb => PixelFormat.RGBa16,
            _ => throw new NotSupportedException(),
        };
    }
}