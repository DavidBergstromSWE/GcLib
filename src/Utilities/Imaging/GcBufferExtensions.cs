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
        PixelFormat.BayerBG10p,
        PixelFormat.BayerBG12p,
        PixelFormat.BayerGB10p,
        PixelFormat.BayerGB12p,
        PixelFormat.BayerRG10p,
        PixelFormat.BayerRG12p,
        PixelFormat.BayerGR10p,
        PixelFormat.BayerGR12p,
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

        // Number of pixels in the image buffer.
        var numberOfPixels = packedBuffer.Width * packedBuffer.Height * packedBuffer.NumChannels;

        // Number of bytes per pixel for the unpacked format.
        int unpackedBytesPerPixel = (int)GenICamHelper.GetBitsPerPixelPerChannel(unpackedPixelFormat) / ByteExtensions.BitsPerByte;

        // Allocate array for unpacked image data.
        var unpackedImageData = new byte[numberOfPixels * unpackedBytesPerPixel];

        // Bit count per pixel per channel for packed pixel format.
        int packedBitsPerPixel = (int)GenICamHelper.GetBitsPerPixelPerChannel(packedBuffer.PixelFormat);

        // Extract each packed pixel directly into the unpacked image data.
        for (int pixel = 0; pixel < numberOfPixels; pixel++)
        {
            ByteExtensions.GetBitRange(source: packedBuffer.ImageData.AsSpan(), // Source span to read the packed pixel data
                                       start: pixel * packedBitsPerPixel,       // Starting bit index for the current pixel
                                       length: packedBitsPerPixel,              // Number of bits to extract for the current pixel
                                       destination: unpackedImageData.AsSpan(pixel * unpackedBytesPerPixel, unpackedBytesPerPixel), // Destination span for the unpacked pixel data
                                       endianness: endianness);
        }

        // Return new buffer instance with unpacked image data.
        return new GcBuffer(unpackedImageData, packedBuffer.Width, packedBuffer.Height, unpackedPixelFormat, packedBuffer.FrameID, packedBuffer.TimeStamp);
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

        // Number of pixels in the image buffer.
        var numberOfPixels = unpackedBuffer.Width * unpackedBuffer.Height * unpackedBuffer.NumChannels;

        // Bit count per pixel per channel for packed pixel format.
        int packedBitsPerPixel = (int)GenICamHelper.GetBitsPerPixelPerChannel(packedPixelFormat);

        // Allocate array for packed image data.
        var packedImageData = new byte[(numberOfPixels * packedBitsPerPixel + 7) / ByteExtensions.BitsPerByte]; // Round up to the nearest byte to ensure enough space for all bits.

        // Number of bytes per pixel for the unpacked format.
        int unpackedBytesPerPixel = (int)GenICamHelper.GetBitsPerPixelPerChannel(unpackedBuffer.PixelFormat) / ByteExtensions.BitsPerByte;

        // Extract each unpacked pixel directly into the packed image data.
        for (int pixel = 0; pixel < numberOfPixels; pixel++)
        {
            ByteExtensions.SetBitRange(target: packedImageData.AsSpan(),       // Target span to write the packed pixel data
                                       start: pixel * packedBitsPerPixel,      // Starting bit index for the current pixel
                                       length: packedBitsPerPixel,             // Number of bits to set for the current pixel
                                       value: unpackedBuffer.ImageData.AsSpan().Slice(pixel * unpackedBytesPerPixel, unpackedBytesPerPixel), // Source span for the unpacked pixel data
                                       endianness: endianness);
        }

        // Return new buffer instance with packed image data.
        return new GcBuffer(packedImageData, unpackedBuffer.Width, unpackedBuffer.Height, packedPixelFormat, unpackedBuffer.FrameID, unpackedBuffer.TimeStamp);
    }
}