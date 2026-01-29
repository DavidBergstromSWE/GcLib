using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Provides static methods for converting raw Bayer-patterned images to standard color image formats such as BGR and
/// RGB.
/// </summary>
/// <remarks>The DeBayer class is intended for use with images captured from cameras or sensors that output
/// Bayer-encoded data. It offers utility methods to perform demosaicing, enabling further image processing or display
/// in common color formats. All methods are static and thread-safe.</remarks>
public static class DeBayer
{
    /// <summary>
    /// Specifies the byte order of color channels in pixel data.
    /// </summary>
    /// <remarks>Use this enumeration to indicate whether color data is stored in blue-green-red (BGR) or
    /// red-green-blue (RGB) order.</remarks>
    private enum ColorOrder
    {
        BGR,
        RGB
    }

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
    /// <remarks>This method modifies the input <paramref name="rawMat"/> in place.</remarks>
    /// <param name="rawMat">The input image image containing raw pixel data in a Bayer format. This matrix will be modified in place.</param>
    /// <param name="inputFormat">The pixel format of the input image, specifying the Bayer pattern to be converted.</param>
    /// <returns>Image containing the image data converted to BGR format. The returned matrix is the same instance as the
    /// input <paramref name="rawMat"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="inputFormat"/> does not correspond to a recognized Bayer pattern with three channels.</exception>
    public static Mat Transform2BGR(Mat rawMat, PixelFormat inputFormat)
    {
        ArgumentNullException.ThrowIfNull(rawMat);

        if (rawMat.IsEmpty)
            throw new ArgumentException("Input Mat is empty.", nameof(rawMat));

        if (SupportedBayerFormats.Contains(inputFormat) == false)
            throw new ArgumentException($"Pixel format '{inputFormat}' is not a recognized Bayer pattern.");

        var bgrMat = new Mat();

        if (inputFormat == PixelFormat.BayerBG8 || inputFormat == PixelFormat.BayerBG10 || inputFormat == PixelFormat.BayerBG12 || inputFormat == PixelFormat.BayerBG14 || inputFormat == PixelFormat.BayerBG16)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerBg2Bgr, dstCn: 3, hint: AlgorithmHint.Default);
        else if (inputFormat == PixelFormat.BayerGB8 || inputFormat == PixelFormat.BayerGB10 || inputFormat == PixelFormat.BayerGB12 || inputFormat == PixelFormat.BayerGB14 || inputFormat == PixelFormat.BayerGB16)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerGb2Bgr, dstCn: 3, hint: AlgorithmHint.Default);
        else if (inputFormat == PixelFormat.BayerRG8 || inputFormat == PixelFormat.BayerRG10 || inputFormat == PixelFormat.BayerRG12 || inputFormat == PixelFormat.BayerRG14 || inputFormat == PixelFormat.BayerRG16)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerRg2Bgr, dstCn: 3, hint: AlgorithmHint.Default);
        else if (PixelFormat.BayerGR8 == inputFormat || inputFormat == PixelFormat.BayerGR10 || inputFormat == PixelFormat.BayerGR12 || inputFormat == PixelFormat.BayerGR14 || inputFormat == PixelFormat.BayerGR16)
            CvInvoke.CvtColor(src: rawMat, dst: bgrMat, code: ColorConversion.BayerGr2Bgr, dstCn: 3, hint: AlgorithmHint.Default);

        return bgrMat;
    }

    /// <summary>
    /// Converts a raw Bayer-patterned image to a 3-channel RGB image using the specified pixel format. The bit depth is preserved during the conversion.
    /// </summary>
    /// <remarks>This method modifies the input <paramref name="rawMat"/> in place.</remarks>
    /// <param name="rawMat">The input image image containing raw pixel data in a Bayer format. This matrix will be modified in place.</param>
    /// <param name="inputFormat">The pixel format of the input image, specifying the Bayer pattern to be converted.</param>
    /// <returns>Image containing the image data converted to RGB format. The returned matrix is the same instance as the
    /// input <paramref name="rawMat"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="inputFormat"/> does not correspond to a recognized Bayer pattern with three channels.</exception>
    public static Mat Transform2RGB(Mat rawMat, PixelFormat inputFormat)
    {
        ArgumentNullException.ThrowIfNull(rawMat);

        if (rawMat.IsEmpty)
            throw new ArgumentException("Input Mat is empty.", nameof(rawMat));

        if (SupportedBayerFormats.Contains(inputFormat) == false)
            throw new ArgumentException($"Pixel format '{inputFormat}' is not a recognized Bayer pattern.");

        var rgbMat = new Mat();

        if (inputFormat == PixelFormat.BayerBG8 || inputFormat == PixelFormat.BayerBG10 || inputFormat == PixelFormat.BayerBG12 || inputFormat == PixelFormat.BayerBG14 || inputFormat == PixelFormat.BayerBG16)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerBg2Rgb, dstCn: 3, hint: AlgorithmHint.Default);
        else if (inputFormat == PixelFormat.BayerGB8 || inputFormat == PixelFormat.BayerGB10 || inputFormat == PixelFormat.BayerGB12 || inputFormat == PixelFormat.BayerGB14 || inputFormat == PixelFormat.BayerGB16)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerGb2Rgb, dstCn: 3, hint: AlgorithmHint.Default);
        else if (inputFormat == PixelFormat.BayerRG8 || inputFormat == PixelFormat.BayerRG10 || inputFormat == PixelFormat.BayerRG12 || inputFormat == PixelFormat.BayerRG14 || inputFormat == PixelFormat.BayerRG16)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerRg2Rgb, dstCn: 3, hint: AlgorithmHint.Default);
        else if (PixelFormat.BayerGR8 == inputFormat || inputFormat == PixelFormat.BayerGR10 || inputFormat == PixelFormat.BayerGR12 || inputFormat == PixelFormat.BayerGR14 || inputFormat == PixelFormat.BayerGR16)
            CvInvoke.CvtColor(src: rawMat, dst: rgbMat, code: ColorConversion.BayerGr2Rgb, dstCn: 3, hint: AlgorithmHint.Default);
        
        return rgbMat;
    }

    /// <summary>
    /// Converts a raw Bayer-patterned image to a 3-channel BGR image using the specified pixel format. The bit depth is preserved during the conversion.
    /// </summary>
    /// <remarks>This method modifies the contents of <paramref name="rawBuffer"/> directly.</remarks>
    /// <param name="rawBuffer">Buffer containing image data in a supported Bayer pixel format. The buffer will be updated with the converted
    /// BGR data.</param>
    /// <returns>Buffer with image data converted to BGR format.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException">Thrown if the pixel format of <paramref name="rawBuffer"/> is not a recognized Bayer pattern.</exception>
    public static GcBuffer Transform2BGR(GcBuffer rawBuffer)
    {
        ArgumentNullException.ThrowIfNull(rawBuffer);

        if (SupportedBayerFormats.Contains(rawBuffer.PixelFormat) == false)
            throw new ArgumentException($"Pixel format '{rawBuffer.PixelFormat}' is not a recognized Bayer pattern.");

        return TransformToColorChannels(rawBuffer, ColorOrder.BGR);
    }

    /// <summary>
    /// Converts a raw Bayer-patterned image to a 3-channel RGB image using the specified pixel format. The bit depth is preserved during the conversion.
    /// </summary>
    /// <remarks>This method modifies the contents of <paramref name="rawBuffer"/> directly.</remarks>
    /// <param name="rawBuffer">Buffer containing image data in a supported Bayer pixel format. The buffer will be updated with the converted
    /// BGR data.</param>
    /// <returns>Buffer with image data converted to RGB format.</returns>
    /// /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException">Thrown if the pixel format of <paramref name="rawBuffer"/> is not a recognized Bayer pattern.</exception>
    public static GcBuffer Transform2RGB(GcBuffer rawBuffer)
    {
        ArgumentNullException.ThrowIfNull(rawBuffer);

        if (SupportedBayerFormats.Contains(rawBuffer.PixelFormat) == false)
            throw new ArgumentException($"Pixel format '{rawBuffer.PixelFormat}' is not a recognized Bayer pattern.");

        return TransformToColorChannels(rawBuffer, ColorOrder.RGB);
    }

    /// <summary>
    /// Converts raw image buffer to a color channel representation using the specified color space and bit depth.
    /// </summary>
    /// <param name="rawBuffer">The input buffer containing the raw image data to be transformed. Must not be null.</param>
    /// <param name="colorOrder">Target color space ("RGB" or "BGR") to use for the output format.</param>
    /// <returns>A new GcBuffer containing the image data transformed into the specified color channel format and bit depth.</returns>
    private static GcBuffer TransformToColorChannels(GcBuffer rawBuffer, ColorOrder colorOrder)
    {
        var rawMat = new Mat(rows: (int)rawBuffer.Height, cols: (int)rawBuffer.Width, EmguConverter.GetDepthType(rawBuffer.PixelFormat), 1);
        rawMat.SetTo(rawBuffer.ImageData);
        var outputMat = Transform2RGB(rawMat, rawBuffer.PixelFormat);
        var outputFormat = Enum.Parse<PixelFormat>(colorOrder.ToString() + Regex.Match(rawBuffer.PixelFormat.ToString(), @"\d+").Value);
        var outputBuffer = new GcBuffer(outputMat, outputFormat, GenICamConverter.GetDynamicRangeMax(outputFormat), rawBuffer.FrameID, rawBuffer.TimeStamp);

        return outputBuffer;
    }
}