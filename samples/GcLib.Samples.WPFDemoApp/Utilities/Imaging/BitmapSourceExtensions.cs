using System;
using System.Windows.Media.Imaging;
using GcLib;
using PixelFormat = GcLib.PixelFormat;

namespace ImagerViewer.Utilities.Imaging;

/// <summary>
/// Provides extension methods for the <see cref="GcBuffer"/> class in <see cref="GcLib"/> library to support WPF applications.
/// </summary>
internal static class BitmapSourceExtensions
{
    /// <summary>
    /// Converts image to <see cref="BitmapSource"/> format.
    /// </summary>
    /// <remarks>
    /// Note: Conversion allocates new memory (to hold the image data).
    /// </remarks>
    /// <returns><see cref="BitmapSource"/> image.</returns>
    public static BitmapSource ToBitmapSource(this GcBuffer buffer)
    {
        var pixelFormat = GetMediaPixelFormat(buffer.PixelFormat);
        var stride = CalculateStride((int)buffer.Width, pixelFormat);

        return BitmapSource.Create(pixelWidth: (int)buffer.Width,
                                   pixelHeight: (int)buffer.Height,
                                   dpiX: 96,
                                   dpiY: 96,
                                   pixelFormat: pixelFormat,
                                   palette: null,
                                   pixels: buffer.ImageData,
                                   stride: stride);
    }

    /// <summary>
    /// Converts <see cref="PixelFormat"/> to <see cref="System.Windows.Media.PixelFormat"/>.
    /// </summary>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>Converted pixel format.</returns>
    /// <exception cref="NotSupportedException"></exception>
    private static System.Windows.Media.PixelFormat GetMediaPixelFormat(PixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.Mono8 => System.Windows.Media.PixelFormats.Gray8,
            PixelFormat.Mono10 or PixelFormat.Mono12 or PixelFormat.Mono14 or PixelFormat.Mono16 => System.Windows.Media.PixelFormats.Gray16,
            PixelFormat.RGB8 => System.Windows.Media.PixelFormats.Rgb24,
            PixelFormat.BGR8 => System.Windows.Media.PixelFormats.Bgr24,
            //PixelFormat.RGBa8 => System.Windows.Media.PixelFormats.Rgba32,
            PixelFormat.BGRa8 => System.Windows.Media.PixelFormats.Bgra32,
            PixelFormat.RGB16 => System.Windows.Media.PixelFormats.Rgb48,
            //PixelFormat.BGR16 => System.Windows.Media.PixelFormats.Bgr48,
            //PixelFormat.RGBa16 or PixelFormat.BGRa16 => System.Windows.Media.PixelFormats.Prgba64,
            _ => throw new NotSupportedException("Pixelformat is not supported!"),
        };
    }

    /// <summary>
    /// Calculates the stride (bytes per row) of an image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>Stride in bytes.</returns>
    private static int CalculateStride(int width, System.Windows.Media.PixelFormat pixelFormat)
    {
        int bytesPerPixel = (pixelFormat.BitsPerPixel + 7) / 8;
        return 4 * ((width * bytesPerPixel + 3) / 4);
    }
}