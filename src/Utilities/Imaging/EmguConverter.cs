using System;
using Emgu.CV.CvEnum;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Utility class for converting between EmguCV depth types and GenICam pixel sizes and formats.
/// </summary>
public static class EmguConverter
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
        return GetDepthType(GenICamConverter.GetPixelSize(pixelFormat));
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
            DepthType.Cv8U or DepthType.Cv8S => 8,
            DepthType.Cv16U or DepthType.Cv16S => 16,
            DepthType.Cv32S or DepthType.Cv32F => 32,
            DepthType.Cv64F => 64,
            _ => throw new NotSupportedException(),
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
            DepthType.Cv8U => byte.MaxValue,
            DepthType.Cv8S => sbyte.MaxValue,
            DepthType.Cv16U => ushort.MaxValue,
            DepthType.Cv16S => short.MaxValue,
            DepthType.Cv32S => int.MaxValue,
            DepthType.Cv32F => float.MaxValue,
            DepthType.Cv64F => double.MaxValue,
            _ => byte.MaxValue,
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
            DepthType.Cv8U => byte.MinValue,
            DepthType.Cv8S => sbyte.MinValue,
            DepthType.Cv16U => ushort.MinValue,
            DepthType.Cv16S => short.MinValue,
            DepthType.Cv32S => int.MinValue,
            DepthType.Cv32F => float.MinValue,
            DepthType.Cv64F => double.MinValue,
            _ => byte.MinValue,
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
                return PixelFormat.RGB8;
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.RGB16;
            else return PixelFormat.InvalidPixelFormat;
        }
        else if (channels == 4)
        {
            if (depthType == DepthType.Cv8U)
                return PixelFormat.RGBa8;
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.RGBa16;
            else return PixelFormat.InvalidPixelFormat;
        }
        else return PixelFormat.InvalidPixelFormat;
    }
}