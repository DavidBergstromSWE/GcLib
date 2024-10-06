using System;
using System.Text.RegularExpressions;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Utility class for handling GenICam pixel formats.
/// </summary>
public static partial class GenICamConverter
{
    /// <summary>
    /// Retrieves the total number of bits of storage needed for a pixel format (for all channels).
    /// </summary>
    /// <param name="pixelFormat">Pixel format in GenICam PFNC.</param>
    /// <returns>Bits per pixel.</returns>
    public static uint GetBitsPerPixel(PixelFormat pixelFormat)
    {
        return (uint)pixelFormat >> 16 & 0xff;
    }

    /// <summary>
    /// Retrieves the number of bits of storage needed per channel for a pixel format.
    /// </summary>
    /// <param name="pixelFormat">Pixel format in GenICam PFNC.</param>
    /// <returns>Bits per pixel per channel.</returns>
    public static uint GetBitsPerPixelPerChannel(PixelFormat pixelFormat)
    {
        return GetBitsPerPixel(pixelFormat) / GetNumChannels(pixelFormat);
    }

    /// <summary>
    /// Retrieves the pixel size corresponding to a pixel format.
    /// </summary>
    /// <param name="pixelFormat">Pixel format in GenICam PFNC.</param>
    /// <returns>Pixel size.</returns>
    public static PixelSize GetPixelSize(PixelFormat pixelFormat)
    {
        // Number of bits.
        byte nBits = byte.Parse(IntegerGeneratedRegex().Match(pixelFormat.ToString()).Value);
        return (PixelSize)nBits;
    }

    /// <summary>
    /// Retrieves the maximum possible pixel value in the dynamic range of a pixel format.
    /// </summary>
    /// <param name="pixelFormat">Pixel format in GenICam PFNC.</param>
    /// <returns>Maximum pixel value.</returns>
    public static uint GetDynamicRangeMax(PixelFormat pixelFormat)
    {
        var pixelSize = GetPixelSize(pixelFormat);
        return (uint)Math.Pow(2, (int)pixelSize) - 1;
    }

    /// <summary>
    /// Retrieves the number of channels contained in a pixel format.
    /// </summary>
    /// <param name="pixelFormat">Pixel format in GenICam PFNC.</param>
    /// <returns>Number of channels.</returns>
    public static uint GetNumChannels(PixelFormat pixelFormat)
    {
#pragma warning disable IDE0066 // Convert switch statement to expression
        switch (pixelFormat)
        {
            case PixelFormat.Mono1p:
            case PixelFormat.Mono2p:
            case PixelFormat.Mono4p:
            case PixelFormat.Mono8:
            case PixelFormat.Mono8s:
            case PixelFormat.Mono10:
            case PixelFormat.Mono10p:
            case PixelFormat.Mono12:
            case PixelFormat.Mono12p:
            case PixelFormat.Mono14:
            case PixelFormat.Mono14p:
            case PixelFormat.Mono16:
            case PixelFormat.Mono32:
            case PixelFormat.BayerBG8:
            case PixelFormat.BayerBG10:
            case PixelFormat.BayerBG12:
            case PixelFormat.BayerBG14:
            case PixelFormat.BayerBG16:
            case PixelFormat.BayerGB8:
            case PixelFormat.BayerGB10:
            case PixelFormat.BayerGB12:
            case PixelFormat.BayerGB14:
            case PixelFormat.BayerGB16:
            case PixelFormat.BayerGR8:
            case PixelFormat.BayerGR10:
            case PixelFormat.BayerGR12:
            case PixelFormat.BayerGR14:
            case PixelFormat.BayerGR16:
            case PixelFormat.BayerRG8:
            case PixelFormat.BayerRG10:
            case PixelFormat.BayerRG12:
            case PixelFormat.BayerRG14:
            case PixelFormat.BayerRG16:
                return 1;
            case PixelFormat.RGB8:
            case PixelFormat.RGB10:
            case PixelFormat.RGB10p:
            case PixelFormat.RGB10p32:
            case PixelFormat.RGB12:
            case PixelFormat.RGB12p:
            case PixelFormat.RGB14:
            case PixelFormat.RGB16:
            case PixelFormat.BGR8:
            case PixelFormat.BGR10:
            case PixelFormat.BGR10p:
            case PixelFormat.BGR12:
            case PixelFormat.BGR12p:
            case PixelFormat.BGR14:
            case PixelFormat.BGR16:
            case PixelFormat.RGB8_Planar:
            case PixelFormat.RGB10_Planar:
            case PixelFormat.RGB12_Planar:
            case PixelFormat.RGB16_Planar:
                return 3;
            case PixelFormat.RGBa8:
            case PixelFormat.RGBa10:
            case PixelFormat.RGBa12:
            case PixelFormat.RGBa14:
            case PixelFormat.RGBa16:
            case PixelFormat.BGRa8:
            case PixelFormat.BGRa10:
            case PixelFormat.BGRa12:
            case PixelFormat.BGRa14:
            case PixelFormat.BGRa16:
                return 4;
            default:
                throw new NotSupportedException("Pixel format is not supported!");
        }
#pragma warning restore IDE0066 // Convert switch statement to expression
    }

    [GeneratedRegex("\\d+")]
    private static partial Regex IntegerGeneratedRegex();
}