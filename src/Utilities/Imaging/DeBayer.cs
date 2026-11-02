using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Provides methods for converting raw Bayer-patterned images to standard color image formats such as BGR and RGB.
/// </summary>
/// <remarks>The class is intended for use with images captured from cameras or sensors that output
/// Bayer-encoded data. It offers utility methods to perform demosaicing, enabling further image processing or display
/// in common color formats. All methods are static and thread-safe.</remarks>
public static class DeBayer
{
    /// <summary>
    /// Supported Bayer-patterned pixel formats that can be converted.
    /// </summary>
    public static readonly List<PixelFormat> SupportedBayerFormats =
    [
        PixelFormat.BayerBG8,
        PixelFormat.BayerBG10,
        PixelFormat.BayerBG12,
        PixelFormat.BayerBG14,
        PixelFormat.BayerBG16,
        PixelFormat.BayerGB8,
        PixelFormat.BayerGB10,
        PixelFormat.BayerGB12,
        PixelFormat.BayerGB14,
        PixelFormat.BayerGB16,
        PixelFormat.BayerRG8,
        PixelFormat.BayerRG10,
        PixelFormat.BayerRG12,
        PixelFormat.BayerRG14,
        PixelFormat.BayerRG16,
        PixelFormat.BayerGR8,
        PixelFormat.BayerGR10,
        PixelFormat.BayerGR12,
        PixelFormat.BayerGR14,
        PixelFormat.BayerGR16
    ];

    /// <summary>
    /// Converts a raw Bayer-patterned image to a 3-channel BGR image using the specified pixel format. The bit depth is preserved during the conversion.
    /// </summary>
    /// <remarks>This method allocates new data and returns a new <see cref="Mat"/> instance.</remarks>
    /// <param name="rawMat">The input image image containing raw pixel data in a Bayer format.</param>
    /// <param name="inputFormat">The pixel format of the input image, specifying the Bayer pattern to be converted.</param>
    /// <returns>Image containing the image data converted to BGR format.</returns>
    /// <exception cref="ArgumentNullException"></exception>"
    /// <exception cref="ArgumentException">Thrown if <paramref name="inputFormat"/> does not correspond to a recognized Bayer pattern.</exception>
    public static Mat ToBGR(Mat rawMat, PixelFormat inputFormat)
    {
        ArgumentNullException.ThrowIfNull(rawMat);

        if (rawMat.IsEmpty)
            throw new ArgumentException("Input Mat is empty.", nameof(rawMat));

        if (SupportedBayerFormats.Contains(inputFormat) == false)
            throw new ArgumentException($"Pixel format '{inputFormat}' is not a recognized Bayer pattern.");

        var bgrMat = new Mat(rawMat.Cols, rawMat.Rows, rawMat.Depth, 3);

        if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerBGGR)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerBggr2Bgr);
        else if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerGBRG)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerGbrg2Bgr);
        else if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerRGGB)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerRggb2Bgr);
        else if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerGRBG)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerGrbg2Bgr);

        return bgrMat;
    }

    /// <summary>
    /// Converts a raw Bayer-patterned image to a 3-channel RGB image using the specified pixel format. The bit depth is preserved during the conversion.
    /// </summary>
    /// <remarks>This method allocates new data and returns a new <see cref="Mat"/> instance.</remarks>
    /// <param name="rawMat">The input image image containing raw pixel data in a Bayer format.</param>
    /// <param name="inputFormat">The pixel format of the input image, specifying the Bayer pattern to be converted.</param>
    /// <returns>Image containing the image data converted to RGB format.</returns>
    /// <exception cref="ArgumentNullException"></exception>"
    /// <exception cref="ArgumentException">Thrown if <paramref name="inputFormat"/> does not correspond to a recognized Bayer pattern.</exception>
    public static Mat ToRGB(Mat rawMat, PixelFormat inputFormat)
    {
        ArgumentNullException.ThrowIfNull(rawMat);

        if (rawMat.IsEmpty)
            throw new ArgumentException("Input Mat is empty.", nameof(rawMat));

        if (SupportedBayerFormats.Contains(inputFormat) == false)
            throw new ArgumentException($"Pixel format '{inputFormat}' is not a recognized Bayer pattern.");

        var rgbMat = new Mat(rawMat.Cols, rawMat.Rows, rawMat.Depth, 3);

        if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerBGGR)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerBggr2Rgb);
        else if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerGBRG)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerGbrg2Rgb);
        else if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerRGGB)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerRggb2Rgb);
        else if (GenICamHelper.GetPixelColorFilter(inputFormat) == PixelColorFilter.BayerGRBG)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerGrbg2Rgb);

        return rgbMat;
    }
}