using System;
using System.Collections.Generic;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Provides extension methods for converting buffers between unpacked and packed pixel formats.
/// </summary>
public static class GcBufferExtensions
{
    /// <summary>
    /// Supported packed pixel formats.
    /// </summary>
    public static readonly List<PixelFormat> SupportedPackedPixelFormats =
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
    /// Unpacks an image buffer where the image data is stored in a packed pixel format into an unpacked format with image data byte aligned.
    /// </summary>
    /// <remarks>This method converts packed image data into an unpacked format based on the specified pixel
    /// format and bit depth. Ensure that the <paramref name="packedBuffer"/>'s pixel format is valid and supported before calling this
    /// method. The method allocates a new buffer to store the unpacked image data.</remarks>
    /// <param name="packedBuffer">The buffer containing the packed image data.</param>
    /// <param name="endianness">The byte order used when storing the pixel data.</param>
    /// <returns>A new <see cref="GcBuffer"/> instance containing the unpacked image data.</returns>
    /// <exception cref="ArgumentException">Thrown when the pixel format of the <paramref name="packedBuffer"/> is not supported for unpacking.</exception>
    public static GcBuffer Unpack(this GcBuffer packedBuffer, ByteExtensions.Endianness endianness = ByteExtensions.Endianness.LittleEndian)
    {
        // Validate that the pixel format is supported.
        if (!SupportedPackedPixelFormats.Contains(packedBuffer.PixelFormat))
            throw new ArgumentException($"Pixel format {packedBuffer.PixelFormat} is not supported for unpacking.");

        // Determine the corresponding unpacked pixel format.
        var unpackedPixelFormat = Enum.Parse<PixelFormat>(packedBuffer.PixelFormat.ToString().Replace("p", string.Empty));

        // Bit count per pixel per channel for unpacked pixel format.
        int unpackedPixelBitCount = (int)GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(unpackedPixelFormat);

        // Allocate array for unpacked image data.
        var unpackedImageData = new byte[packedBuffer.Width * packedBuffer.Height * packedBuffer.NumChannels * unpackedPixelBitCount / ByteExtensions.BitsPerByte];

        // Iterate through each pixel in the packed image data.
        int byteIndex = 0;
        for (int bitNumber = 0; bitNumber < packedBuffer.Width * packedBuffer.Height * packedBuffer.NumChannels * packedBuffer.BitDepth; bitNumber += (int)packedBuffer.BitDepth)
        {
            // Extract bits for each packed pixel.
            var bits = ByteExtensions.GetBitRange(source: packedBuffer.ImageData,
                                                  start: bitNumber,
                                                  length: (int)packedBuffer.BitDepth,
                                                  endianness);

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
    /// Packs an image buffer where the image data is stored in an unpacked pixel format into a compressed format suitable for storage or transmission.
    /// </summary>
    /// <remarks>This method converts unpacked image data into a packed format based on the specified pixel
    /// format and bit depth. Ensure that the pixel format of the input data can be converted into a supported packed pixel format before calling this method.
    /// The method allocates a new buffer to store the packed image data.</remarks>
    /// <param name="unpackedBuffer">The buffer containing the unpacked image data.</param>
    /// <param name="endianness">The byte order used when storing the pixel data.</param>
    /// <returns>A new <see cref="GcBuffer"/> instance containing the packed image data.</returns>
    /// <exception cref="ArgumentException">Thrown if the pixel format of the provided buffer can not be converted into a supported packed pixel format.</exception>
    public static GcBuffer Pack(this GcBuffer unpackedBuffer, ByteExtensions.Endianness endianness = ByteExtensions.Endianness.LittleEndian)
    {
        // Determine the corresponding packed pixel format.
        var packedPixelFormat = Enum.Parse<PixelFormat>(unpackedBuffer.PixelFormat.ToString() + "p");

        // Validate that the packed pixel format is supported.
        if (!SupportedPackedPixelFormats.Contains(packedPixelFormat))
            throw new ArgumentException($"Pixel format {unpackedBuffer.PixelFormat} is not supported for packing.");

        // Bit count per pixel per channel for packed pixel format.
        int packedPixelBitCount = (int)GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(packedPixelFormat);

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
            ByteExtensions.SetBitRange(target: packedImageData,
                                       start: bitStartIndex,
                                       length: packedPixelBitCount,
                                       value: bytes,
                                       endianness);
        }

        // Return new buffer instance with packed image data.
        return new GcBuffer(packedImageData, unpackedBuffer.Width, unpackedBuffer.Height, packedPixelFormat, unpackedBuffer.PixelDynamicRangeMax, unpackedBuffer.FrameID, unpackedBuffer.TimeStamp);
    }
}