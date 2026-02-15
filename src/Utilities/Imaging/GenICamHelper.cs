using System;
using System.Text.RegularExpressions;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Utility class for handling the pixel formats defined in GenICam Pixel Format Naming Convention (PFNC).
/// </summary>
public static partial class GenICamHelper
{
    /// <summary>
    /// Retrieves the total number of bits of storage needed for a pixel in a specified <paramref name="pixelFormat"/>, summed over all of its channels.
    /// </summary>
    /// <remarks>
    /// The number of bits returned by this method reflects how pixel data is structured and stored in computer memory, 
    /// where unpacked pixel formats aligns data with byte boundaries (using padding zeros if necessary) 
    /// while packed formats minimize memory usage by tightly grouping the bits.
    /// To retrieve the number of bits represented by the pixel data (e.g. bit depth), see <see cref="GetPixelSize(PixelFormat)"/>.
    /// </remarks>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <returns>Bits per pixel (summed over all channels).</returns>
    /// <exception cref="NotSupportedException">Thrown if the pixel format is not supported.</exception>"
    public static uint GetBitsPerPixel(PixelFormat pixelFormat)
    {
        return GetBitsPerPixelPerChannel(pixelFormat) * GetNumChannels(pixelFormat);
    }

    /// <summary>
    /// Retrieves the number of bits of storage needed per channel for a specified <paramref name="pixelFormat"/>.
    /// </summary>
    /// <remarks>
    /// The number of bits returned by this method reflects how pixel data is structured and stored in computer memory, 
    /// where unpacked pixel formats aligns data with byte boundaries (using padding zeros if necessary) 
    /// while packed formats minimize memory usage by tightly grouping the bits.
    /// To retrieve the number of bits represented by the pixel data (e.g. bit depth), see <see cref="GetPixelSize(PixelFormat)"/>.
    /// </remarks>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <returns>Bits per pixel per channel.</returns>
    public static uint GetBitsPerPixelPerChannel(PixelFormat pixelFormat)
    {
        var nBits = byte.Parse(IntegerGeneratedRegex().Match(pixelFormat.ToString()).Value);
        if (pixelFormat.ToString().Contains('p')) // packed format (without byte alignment)
            return nBits;
        else return (uint)((nBits + 7) / 8 * 8); // unpacked formats (with byte-alignment)
    }

    /// <summary>
    /// Retrieves the pixel size corresponding to a specified <paramref name="pixelFormat"/>.
    /// </summary>
    /// <remarks>
    /// The pixel size returned by this method reflects how many bits are needed to represent the pixel data (e.g. bit depth).
    /// To retrieve the number of bits needed to store the pixel data in memory, see <see cref="GetBitsPerPixel(PixelFormat)"/> and <see cref="GetBitsPerPixelPerChannel(PixelFormat)"/> methods.
    /// </remarks>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <returns>Pixel size.</returns>
    public static PixelSize GetPixelSize(PixelFormat pixelFormat)
    {
        return (PixelSize)byte.Parse(IntegerGeneratedRegex().Match(pixelFormat.ToString()).Value);
    }

    /// <summary>
    /// Retrieves the maximum possible pixel value of a specified <paramref name="pixelFormat"/>.
    /// </summary>
    /// <remarks>
    /// The maximum pixel value returned by this method is calculated from the pixel size (e.g. bit depth) of the pixel format. 
    /// This is not the same as the true dynamic range of a pixel, which is determined by the saturation value and noise floor of the detector.
    /// </remarks>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <returns>Maximum pixel value.</returns>
    public static uint GetPixelDynamicRangeMax(PixelFormat pixelFormat)
    {
        var pixelSize = GetPixelSize(pixelFormat);
        return (uint)Math.Pow(2, (int)pixelSize) - 1;
    }

    /// <summary>
    /// Retrieves the number of channels contained in a specified <paramref name="pixelFormat"/>.
    /// </summary>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <returns>Number of channels.</returns>
    /// <exception cref="NotSupportedException">Thrown if the pixel format is not supported.</exception>"
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
            case PixelFormat.B8:
            case PixelFormat.B10:
            case PixelFormat.B12:
            case PixelFormat.B16:
            case PixelFormat.G8:
            case PixelFormat.G10:
            case PixelFormat.G12:
            case PixelFormat.G16:
            case PixelFormat.R8:
            case PixelFormat.R10:
            case PixelFormat.R12:
            case PixelFormat.R16:
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
            case PixelFormat.BayerBG4p:
            case PixelFormat.BayerBG10p:
            case PixelFormat.BayerBG12p:
            case PixelFormat.BayerBG14p:
            case PixelFormat.BayerGB4p:
            case PixelFormat.BayerGB10p:
            case PixelFormat.BayerGB12p:
            case PixelFormat.BayerGB14p:
            case PixelFormat.BayerGR4p:
            case PixelFormat.BayerGR10p:
            case PixelFormat.BayerGR12p:
            case PixelFormat.BayerGR14p:
            case PixelFormat.BayerRG4p:
            case PixelFormat.BayerRG10p:
            case PixelFormat.BayerRG12p:
            case PixelFormat.BayerRG14p:
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
            case PixelFormat.RGB565p:
            case PixelFormat.BGR565p:
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
            case PixelFormat.RGBa10p:
            case PixelFormat.RGBa12p:
            case PixelFormat.BGRa10p:
            case PixelFormat.BGRa12p:
                return 4;
            default:
                throw new NotSupportedException("Pixel format is not supported!");
        }
#pragma warning restore IDE0066 // Convert switch statement to expression
    }

    /// <summary>
    /// Retrieves the pixel color filter applied in a specified <paramref name="pixelFormat"/>.
    /// </summary>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <returns>Pixel color filter.</returns>
    public static PixelColorFilter GetPixelColorFilter(PixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PixelFormat.BayerBG8:
            case PixelFormat.BayerBG10:
            case PixelFormat.BayerBG12:
            case PixelFormat.BayerBG14:
            case PixelFormat.BayerBG16:
            case PixelFormat.BayerBG4p:
            case PixelFormat.BayerBG10p:
            case PixelFormat.BayerBG12p:
            case PixelFormat.BayerBG14p:
                return PixelColorFilter.BayerBGGR;
            case PixelFormat.BayerGB8:
            case PixelFormat.BayerGB10:
            case PixelFormat.BayerGB12:
            case PixelFormat.BayerGB14:
            case PixelFormat.BayerGB16:
            case PixelFormat.BayerGB4p:
            case PixelFormat.BayerGB10p:
            case PixelFormat.BayerGB12p:
            case PixelFormat.BayerGB14p:
                return PixelColorFilter.BayerGBRG;
            case PixelFormat.BayerGR8:
            case PixelFormat.BayerGR10:
            case PixelFormat.BayerGR12:
            case PixelFormat.BayerGR14:
            case PixelFormat.BayerGR16:
            case PixelFormat.BayerGR4p:
            case PixelFormat.BayerGR10p:
            case PixelFormat.BayerGR12p:
            case PixelFormat.BayerGR14p:
                return PixelColorFilter.BayerGRBG;
            case PixelFormat.BayerRG8:
            case PixelFormat.BayerRG10:
            case PixelFormat.BayerRG12:
            case PixelFormat.BayerRG14:
            case PixelFormat.BayerRG16:
            case PixelFormat.BayerRG4p:
            case PixelFormat.BayerRG10p:
            case PixelFormat.BayerRG12p:
            case PixelFormat.BayerRG14p:
                return PixelColorFilter.BayerRGGB;
            default:
                return PixelColorFilter.None;
        }
    }

    [GeneratedRegex("\\d+")]
    private static partial Regex IntegerGeneratedRegex();
}