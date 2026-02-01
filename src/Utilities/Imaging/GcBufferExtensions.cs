using System;
using System.Collections.Generic;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Contains methods for converting buffers between unpacked and packed pixel formats.
/// </summary>
public static class GcBufferExtensions
{
    /// <summary>
    /// Supported packed pixel formats.
    /// </summary>
    public static readonly List<PixelFormat> SupportedPixelFormats =
    [
        PixelFormat.Mono10p,
        PixelFormat.Mono12p,
        PixelFormat.Mono14p,
        PixelFormat.BGR10p,
        PixelFormat.BGR12p,
        PixelFormat.RGB10p,
        PixelFormat.RGB12p,
        PixelFormat.BGR10p,
        PixelFormat.BGR12p,
        PixelFormat.BGRa10p,
        PixelFormat.BGRa12p,
        PixelFormat.RGBa10p,
        PixelFormat.RGBa12p,
        //PixelFormat.BayerBG10p,
        //PixelFormat.BayerBG12p,
        //PixelFormat.BayerGB10p,
        //PixelFormat.BayerGB12p,
        //PixelFormat.BayerRG10p,
        //PixelFormat.BayerRG12p,
        //PixelFormat.BayerGR10p,
        //PixelFormat.BayerGR12p,
    ];

    /// <summary>
    /// Unpacks a packed image buffer into its original pixel format and image data.
    /// </summary>
    /// <remarks>This method converts packed image data into an unpacked format based on the specified pixel
    /// format and bit depth. Ensure that the packedBuffer's pixel format is valid and supported before calling this
    /// method.</remarks>
    /// <param name="packedBuffer">The packed buffer containing image data, dimensions, pixel format, and associated metadata to be unpacked. The
    /// buffer must use a supported pixel format.</param>
    /// <returns>A new <see cref="GcBuffer"/> instance containing the unpacked image data.</returns>
    /// <exception cref="ArgumentException">Thrown when the pixel format of the packedBuffer is not supported for unpacking.</exception>
    public static GcBuffer UnpackBuffer(GcBuffer packedBuffer)
    {
        // Validate that the pixel format is supported.
        if (!SupportedPixelFormats.Contains(packedBuffer.PixelFormat))
            throw new ArgumentException($"Pixel format {packedBuffer.PixelFormat} is not supported for unpacking.");

        // Determine the corresponding unpacked pixel format.
        var unpackedPixelFormat = Enum.Parse<PixelFormat>(packedBuffer.PixelFormat.ToString().Replace("p", string.Empty));

        // Bit count per pixel per channel for unpacked pixel format.
        int unpackedPixelBitCount = (int)GenICamConverter.GetBitsPerPixelPerChannel(unpackedPixelFormat);

        // Allocate array for unpacked image data.
        var unpackedImageData = new byte[packedBuffer.Width * packedBuffer.Height * packedBuffer.NumChannels * unpackedPixelBitCount / ByteExtensions.BitsPerByte];

        // Iterate through each pixel in the packed image data.
        int byteIndex = 0;
        for (int bitNumber = 0; bitNumber < packedBuffer.Width * packedBuffer.Height * packedBuffer.NumChannels * packedBuffer.BitDepth; bitNumber += (int)packedBuffer.BitDepth)
        {
            // Extract bits for each packed pixel.
            var bits = ByteExtensions.GetBitRange(bytes: packedBuffer.ImageData,
                                                  start: bitNumber,
                                                  length: (int)packedBuffer.BitDepth);
            
            // Copy bits to unpacked image data.
            foreach (var b in bits)
            {
                unpackedImageData[byteIndex] = b;
                byteIndex++;
            }
        }

        // Return new buffer instance with unpacked image data.
        return new GcBuffer(unpackedImageData, packedBuffer.Width, packedBuffer.Height, unpackedPixelFormat, packedBuffer.PixelDynamicRangeMax, packedBuffer.FrameID, packedBuffer.TimeStamp);
    }

    /// <summary>
    /// Packs the specified unpacked image buffer into a compressed format suitable for storage or transmission.
    /// </summary>
    /// <remarks>The method converts the unpacked image data into a packed format based on the specified pixel
    /// format. Ensure that the pixel format of the input buffer is supported before calling this method.</remarks>
    /// <param name="unpackedBuffer">The buffer containing unpacked image data, including pixel format, dimensions, and image data to be packed.</param>
    /// <returns>A new <see cref="GcBuffer"/> instance containing the packed image data.</returns>
    /// <exception cref="ArgumentException">Thrown if the pixel format of the provided buffer is not supported for packing.</exception>
    public static GcBuffer PackBuffer(GcBuffer unpackedBuffer)
    {
        // Determine the corresponding packed pixel format.
        var packedPixelFormat = Enum.Parse<PixelFormat>(unpackedBuffer.PixelFormat.ToString() + "p");

        // Validate that the packed pixel format is supported.
        if (!SupportedPixelFormats.Contains(packedPixelFormat))
            throw new ArgumentException($"Pixel format {unpackedBuffer.PixelFormat} is not supported for packing.");

        // Bit count per pixel per channel for packed pixel format.
        int packedPixelBitCount = (int)GenICamConverter.GetBitsPerPixelPerChannel(packedPixelFormat);

        // Allocate array for packed image data.
        var packedImageData = new byte[(unpackedBuffer.Width * unpackedBuffer.Height * unpackedBuffer.NumChannels * packedPixelBitCount + 7) / ByteExtensions.BitsPerByte];

        // Iterate through each pixel in the unpacked image data.
        for (int pixel = 0; pixel < unpackedBuffer.Width * unpackedBuffer.Height * unpackedBuffer.NumChannels; pixel++)
        {
            // Extract bytes for each unpacked pixel.
            var bytes = unpackedBuffer.ImageData.AsSpan(pixel * ((packedPixelBitCount + 7) / ByteExtensions.BitsPerByte), (packedPixelBitCount + 7) / ByteExtensions.BitsPerByte);

            // Calculate the start index for the current pixel in bits.
            int bitStartIndex = pixel * packedPixelBitCount;

            // Set bits in the packed image data.
            ByteExtensions.SetBitRange(bytes: packedImageData,
                                       start: bitStartIndex,
                                       length: packedPixelBitCount,
                                       value: bytes);
        }

        // Return new buffer instance with packed image data.
        return new GcBuffer(packedImageData, unpackedBuffer.Width, unpackedBuffer.Height, packedPixelFormat, unpackedBuffer.PixelDynamicRangeMax, unpackedBuffer.FrameID, unpackedBuffer.TimeStamp);
    }
}