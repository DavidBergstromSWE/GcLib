using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Utility class for <see cref="Emgu.CV"/> library (.NET wrapper for OpenCV).
/// </summary>
public static class EmguHelper
{
    /// <summary>
    /// Converts GenICam pixel size to EmguCV depth type.
    /// </summary>
    /// <param name="pixelSize">Pixel size (Bpp8, Bpp16, etc.).</param>
    /// <returns>EmguCV depth type.</returns>
    public static DepthType GetDepthType(PixelSize pixelSize)
    {
        if ((int)pixelSize <= 8)
            return DepthType.Cv8U;
        else if ((int)pixelSize <= 16)
            return DepthType.Cv16U;
        else if ((int)pixelSize <= 32)
            return DepthType.Cv32S;
        else if ((int)pixelSize <= 64)
            return DepthType.Cv64F;
        else return DepthType.Default;
    }

    /// <summary>
    /// Converts GeniCam <see cref="PixelFormat"/> to EmguCV depth type.
    /// </summary>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>EmguCV depth type.</returns>
    public static DepthType GetDepthType(PixelFormat pixelFormat)
    {
        return GetDepthType(GenICamHelper.GetPixelSize(pixelFormat));
    }

    /// <summary>
    /// Converts System.Type to EmguCV depth type.
    /// </summary>
    /// <param name="type">System.Type.</param>
    /// <returns>EmguCV depth type.</returns>
    public static DepthType GetDepthType(Type type)
    {
        if (!NumericHelper.IsNumericType(type))
            throw new ArgumentException("Type is not a numeric type!");

        return type.Name switch
        {
            "Byte" => DepthType.Cv8U,
            "SByte" => DepthType.Cv8S,
            "UInt16" => DepthType.Cv16U,
            "Int16" => DepthType.Cv16S,
            "Int32" => DepthType.Cv32S,
            "Single" => DepthType.Cv32F,
            "Double" => DepthType.Cv64F,
            _ => DepthType.Default
        };
    }


    /// <summary>
    /// Converts bit depth to EmguCV depth type.
    /// </summary>
    /// <param name="bitDepth">Bit depth (number of bits).</param>
    /// <returns>EmguCV depth type.</returns>
    public static DepthType GetDepthType(int bitDepth)
    {
        if (bitDepth <= 8)
            return DepthType.Cv8U;
        else if (bitDepth <= 16)
            return DepthType.Cv16U;
        else if (bitDepth <= 32)
            return DepthType.Cv32S;
        else if (bitDepth <= 64)
            return DepthType.Cv64F;
        else return DepthType.Default;
    }

    /// <summary>
    /// Converts EmguCV depth type to bit depth (number of bits).
    /// </summary>
    /// <param name="depthType">EmguCV depth type.</param>
    /// <returns>Number of bits required to store depth type.</returns>
    public static int GetBitDepth(DepthType depthType)
    {
        return depthType switch
        {
            DepthType.CvBool => 1,
            DepthType.Cv8U or DepthType.Cv8S => 8,
            DepthType.Cv16S or DepthType.Cv16U or DepthType.Cv16F => 16,
            DepthType.Cv32S or DepthType.Cv32U or DepthType.Cv32F => 32,
            DepthType.Cv64S or DepthType.Cv64U or DepthType.Cv64F => 64,
            _ => throw new ArgumentException($"Depth type {depthType} does not have a defined bit depth!")
        };
    }

    /// <summary>
    /// Retrieves maximum pixel value of DepthType.
    /// </summary>
    /// <param name="depthType">DepthType (EmguCV enum type).</param>
    /// <returns>Maximum pixel value.</returns>
    public static double GetMax(DepthType depthType)
    {
        return depthType switch
        {
            DepthType.CvBool => 1,
            DepthType.Cv8U => byte.MaxValue,
            DepthType.Cv8S => sbyte.MaxValue,
            DepthType.Cv16U => ushort.MaxValue,
            DepthType.Cv16S => short.MaxValue,
            DepthType.Cv16F => (double)Half.MaxValue,
            DepthType.Cv32U => uint.MaxValue,
            DepthType.Cv32S => int.MaxValue,
            DepthType.Cv32F => float.MaxValue,
            DepthType.Cv64S => long.MaxValue,
            DepthType.Cv64U => ulong.MaxValue,
            DepthType.Cv64F => double.MaxValue,
            _ => throw new ArgumentException($"Depth type {depthType} does not have a defined maximum value!")
        };
    }

    /// <summary>
    /// Retrieves minimum pixel value of DepthType.
    /// </summary>
    /// <param name="depthType">DepthType (EmguCV enum type).</param>
    /// <returns>Minimum pixel value.</returns>
    public static double GetMin(DepthType depthType)
    {
        return depthType switch
        {
            DepthType.CvBool => 0,
            DepthType.Cv8U => byte.MinValue,
            DepthType.Cv8S => sbyte.MinValue,
            DepthType.Cv16U => ushort.MinValue,
            DepthType.Cv16S => short.MinValue,
            DepthType.Cv16F => (double)Half.MinValue,
            DepthType.Cv32U => uint.MinValue,
            DepthType.Cv32S => int.MinValue,
            DepthType.Cv32F => float.MinValue,
            DepthType.Cv64S => long.MinValue,
            DepthType.Cv64U => ulong.MinValue,
            DepthType.Cv64F => double.MinValue,
            _ => throw new ArgumentException($"Depth type {depthType} does not have a defined minimum value!")
        };
    }

    /// <summary>
    /// Converts EmguCV depth type and number of channels to GenICam pixel format.
    /// </summary>
    /// <param name="depthType">EmguCV depth type.</param>
    /// <param name="channels">Number of channels.</param>
    /// <returns>GenICam pixel format.</returns>
    public static PixelFormat GetPixelFormat(DepthType depthType, int channels)
    {
        if (channels == 1)
        {
            if (depthType == DepthType.Cv8U)
                return PixelFormat.Mono8;
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.Mono16;
            else return PixelFormat.InvalidPixelFormat;
        }
        else if (channels == 3)
        {
            if (depthType == DepthType.Cv8U)
                return PixelFormat.BGR8; // OpenCV uses BGR by default
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.BGR16; // OpenCV uses BGR by default
            else return PixelFormat.InvalidPixelFormat;
        }
        else if (channels == 4)
        {
            if (depthType == DepthType.Cv8U)
                return PixelFormat.BGRa8;
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.BGRa16;
            else return PixelFormat.InvalidPixelFormat;
        }
        else return PixelFormat.InvalidPixelFormat;
    }

    /// <summary>
    /// Converts a matrix from one pixel format to another.
    /// </summary>
    /// <param name="mat">The matrix to convert.</param>
    /// <param name="sourcePixelFormat">The current pixel format of <paramref name="mat"/>.</param>
    /// <param name="targetPixelFormat">The pixel format to convert <paramref name="mat"/> to.</param>
    /// <returns>The converted matrix. If the pixel formats are the same, returns <paramref name="mat"/> without conversion.</returns>
    /// <exception cref="NotSupportedException">A pixel format is invalid, packed or Bayer-patterned, or the requested color conversion is unsupported.</exception>
    internal static Mat ConvertMat(Mat mat, PixelFormat sourcePixelFormat, PixelFormat targetPixelFormat)
    {
        if (sourcePixelFormat == PixelFormat.InvalidPixelFormat || targetPixelFormat == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Conversion to/from an invalid pixel format is not supported.");

        if (GenICamHelper.IsPacked(sourcePixelFormat) || GenICamHelper.IsPacked(targetPixelFormat))
            throw new NotSupportedException($"Conversion of packed pixel formats is not supported.");

        if (GenICamHelper.GetPixelColorFilter(sourcePixelFormat) != PixelColorFilter.None ||
            GenICamHelper.GetPixelColorFilter(targetPixelFormat) != PixelColorFilter.None)
            throw new NotSupportedException("Conversion to/from Bayer pixel formats is not supported.");

        // If the pixel formats are the same, no conversion is needed.
        if (sourcePixelFormat == targetPixelFormat)
            return mat;

        // Convert the matrix to the target depth type.
        var targetMax = GenICamHelper.GetPixelDynamicRangeMax(targetPixelFormat);
        mat.ConvertTo(mat, GetDepthType(targetPixelFormat), (double)targetMax / GenICamHelper.GetPixelDynamicRangeMax(sourcePixelFormat)); // Scale the pixel values to match the new depth type

        // Determine appropriate conversion code based on the source and target pixel formats.
        if (!GenICamHelper.IsColor(sourcePixelFormat) && !GenICamHelper.IsColor(targetPixelFormat))
            return mat; // No color conversion needed for grayscale formats

        // If both source and target are color formats with the same color order, no conversion is needed.
        if (GenICamHelper.IsColor(sourcePixelFormat) && GenICamHelper.IsColor(targetPixelFormat) && GenICamHelper.GetColorOrder(sourcePixelFormat) == GenICamHelper.GetColorOrder(targetPixelFormat))
            return mat;

        // Determine the appropriate color conversion code based on the source and target pixel formats.
        var conversionCode = GetConversionCode(sourcePixelFormat, targetPixelFormat);

        // Perform color conversion using determined conversion code.
        CvInvoke.CvtColor(mat, mat, conversionCode);

        // OpenCV initializes new alpha to the storage maximum; correct it only for reduced-range formats.
        if (GenICamHelper.GetNumChannels(sourcePixelFormat) < 4 && mat.NumberOfChannels == 4 && targetMax != GetMax(mat.Depth))
        {
            using var alpha = new Mat(mat.Rows, mat.Cols, mat.Depth, 1);
            alpha.SetTo(new Emgu.CV.Structure.MCvScalar(targetMax));
            CvInvoke.InsertChannel(alpha, mat, 3);
        }

        return mat;
    }


    /// <summary>
    /// Gets the conversion code for grayscale or RGB/BGR pixel formats, with or without alpha.
    /// </summary>
    /// <param name="sourcePixelFormat">The source pixel format.</param>
    /// <param name="targetPixelFormat">The target pixel format.</param>
    /// <returns>The color conversion code for the specified pixel formats.</returns>
    /// <exception cref="NotSupportedException">The conversion from the source pixel format to the target pixel format is not supported.</exception>
    private static ColorConversion GetConversionCode(PixelFormat sourcePixelFormat, PixelFormat targetPixelFormat)
    {
        // If the source pixel format is not a color format, determine the appropriate conversion code based on the target pixel format's color order.
        if (GenICamHelper.IsColor(sourcePixelFormat) == false)
        {
            return GenICamHelper.GetColorOrder(targetPixelFormat) switch
            {
                ColorOrder.BGR => ColorConversion.Gray2Bgr,
                ColorOrder.RGB => ColorConversion.Gray2Rgb,
                ColorOrder.BGRa => ColorConversion.Gray2Bgra,
                ColorOrder.RGBa => ColorConversion.Gray2Rgba,
                _ => throw new NotSupportedException($"Conversion from {sourcePixelFormat} to {targetPixelFormat} is not supported.")
            };
        }

        // If the target pixel format is not a color format, determine the appropriate conversion code based on the source pixel format's color order.
        if (GenICamHelper.IsColor(targetPixelFormat) == false)
        {
            return GenICamHelper.GetColorOrder(sourcePixelFormat) switch
            {
                ColorOrder.BGR => ColorConversion.Bgr2Gray,
                ColorOrder.RGB => ColorConversion.Rgb2Gray,
                ColorOrder.BGRa => ColorConversion.Bgra2Gray,
                ColorOrder.RGBa => ColorConversion.Rgba2Gray,
                _ => throw new NotSupportedException($"Conversion from {sourcePixelFormat} to {targetPixelFormat} is not supported.")
            };
        }

        // Determine the appropriate conversion code based on the source and target pixel formats' color orders.
        return GenICamHelper.GetColorOrder(sourcePixelFormat) switch
        {
            ColorOrder.BGR when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGB => ColorConversion.Bgr2Rgb,
            ColorOrder.RGB when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGR => ColorConversion.Rgb2Bgr,
            ColorOrder.BGR when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGRa => ColorConversion.Bgr2Bgra,
            ColorOrder.BGRa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGR => ColorConversion.Bgra2Bgr,
            ColorOrder.RGB when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGBa => ColorConversion.Rgb2Rgba,
            ColorOrder.RGBa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGB => ColorConversion.Rgba2Rgb,
            ColorOrder.BGRa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGBa => ColorConversion.Bgra2Rgba,
            ColorOrder.RGBa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGRa => ColorConversion.Rgba2Bgra,
            ColorOrder.BGR when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGBa => ColorConversion.Bgr2Rgba,
            ColorOrder.RGBa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGR => ColorConversion.Rgba2Bgr,
            ColorOrder.RGB when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGRa => ColorConversion.Rgb2Bgra,
            ColorOrder.BGRa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGB => ColorConversion.Bgra2Rgb,
            _ => throw new NotSupportedException($"Conversion from {sourcePixelFormat} to {targetPixelFormat} is not supported.")
        };
    }
}