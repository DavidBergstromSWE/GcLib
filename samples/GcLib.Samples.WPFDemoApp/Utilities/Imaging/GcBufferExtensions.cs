using System;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Util;
using GcLib;
using PixelFormat = GcLib.PixelFormat;

namespace FusionViewer.Utilities.Imaging;

/// <summary>
/// Provides extension methods for the <see cref="GcBuffer"/> class in <see cref="GcLib"/> library to support WPF applications.
/// </summary>
internal static class GcBufferExtensions
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
                                   stride: stride); // CalculateStride(buffer.Width, buffer.NumChannels * buffer.BitDepth)
    }

    /// <summary>
    /// Calculates histogram from image buffer. 
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    /// <param name="bins">Number of bins (histogram size).</param>
    /// <param name="mask">Optional mask.</param>
    public static double[,] GetHistogram(this GcBuffer buffer, int bins, Mat mask = null)
    {
        var histogramData = new double[buffer.NumChannels, bins];

        // Convert to Mat.
        using var mat = buffer.ToMat();

        // Store metadata.
        var histogramMaxValue = buffer.PixelDynamicRangeMax;
        var numChannels = (int)buffer.NumChannels;

        // Split image into array of single-channel (grayscale) images.
        Mat[] matChannels = mat.Split();

        // Calculate histogram for each image channel.
        for (int i = 0; i < Math.Min(numChannels, 3); i++)
        {
            using var histogram = new Mat();

            // Calculate histogram using EmguCV.
            using var vMat = new VectorOfMat(matChannels[i]);
            CvInvoke.CalcHist(images: vMat, channels: [0], mask: mask, hist: histogram, histSize: [bins], ranges: [0, histogramMaxValue + 1], accumulate: false);

            // Update histogram data via Span copy.
            ReadOnlySpan<float> floatSpan = new((float[])histogram.GetData(false));
            for (int j = 0; j < floatSpan.Length; j++)
                histogramData[i, j] = floatSpan[j];
        }
        return histogramData;
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
            PixelFormat.RGB8 or PixelFormat.BGR8 => System.Windows.Media.PixelFormats.Bgr24,
            PixelFormat.BGRa8 or PixelFormat.RGBa8 => System.Windows.Media.PixelFormats.Bgra32,
            PixelFormat.RGB16 or PixelFormat.BGR16 => System.Windows.Media.PixelFormats.Rgb48,
            PixelFormat.RGBa16 or PixelFormat.BGRa16 => System.Windows.Media.PixelFormats.Prgba64,
            _ => throw new NotSupportedException("Pixelformat is not supported!"),
        };
    }

    /// <summary>
    /// Calculates the stride (bytes per row) of an image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="bitsPerPixel">Number of bits per pixel (all channels included).</param>
    /// <returns>Stride in bytes.</returns>
    private static int CalculateStride(uint width, uint bitsPerPixel)
    {
        // Round to nearest 4 bytes.
        return (int)(width * bitsPerPixel / 8 + (width % 4));
        //return 4 * (int)Math.Round((double)width * bitsPerPixel / 8 / 4); // slower?

        //return (int)(width * bitsPerPixel / 8); // old version
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