using System;
using System.Collections.Generic;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Provides extension methods for converting <see cref="GcBuffer"/> instances between packed and unpacked PFNC pixel formats and debayering.
/// </summary>
public static partial class GcBufferExtensions
{
    /// <summary>
    /// Gets the list of packed pixel formats supported by <see cref="Pack(GcBuffer, ByteExtensions.Endianness)"/> and <see cref="Unpack(GcBuffer, ByteExtensions.Endianness)"/>.
    /// </summary>
    public static readonly List<PixelFormat> SupportedPackedPixelFormats =
    [
        PixelFormat.Mono2p,
        PixelFormat.Mono4p,
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
        PixelFormat.BayerBG4p,
        PixelFormat.BayerBG10p,
        PixelFormat.BayerBG12p,
        PixelFormat.BayerGB4p,
        PixelFormat.BayerGB10p,
        PixelFormat.BayerGB12p,
        PixelFormat.BayerRG4p,
        PixelFormat.BayerRG10p,
        PixelFormat.BayerRG12p,
        PixelFormat.BayerGR4p,
        PixelFormat.BayerGR10p,
        PixelFormat.BayerGR12p,
    ];

    /// <summary>
    /// Gets the list of unpacked pixel formats that can be converted to packed formats.
    /// </summary>
    public static readonly List<PixelFormat> SupportedUnpackedPixelFormats =
    [
        PixelFormat.Mono8,
        PixelFormat.Mono8s,
        PixelFormat.Mono10,
        PixelFormat.Mono12,
        PixelFormat.Mono14,
        PixelFormat.Mono16,
        PixelFormat.BGR8,
        PixelFormat.BGR10,
        PixelFormat.BGR12,
        PixelFormat.BGR14,
        PixelFormat.BGR16,
        PixelFormat.RGB8,
        PixelFormat.RGB10,
        PixelFormat.RGB12,
        PixelFormat.RGB14,
        PixelFormat.RGB16,
        PixelFormat.BGRa8,
        PixelFormat.BGRa10,
        PixelFormat.BGRa12,
        PixelFormat.BGRa14,
        PixelFormat.BGRa16,
        PixelFormat.RGBa8,
        PixelFormat.RGBa10,
        PixelFormat.RGBa12,
        PixelFormat.RGBa14,
        PixelFormat.RGBa16,
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
    /// Converts packed image data to a byte-aligned representation.
    /// </summary>
    /// <remarks>
    /// Each packed sample is expanded to its byte-aligned storage width for the corresponding unpacked PFNC format.
    /// A new <see cref="GcBuffer"/> is allocated for the result.
    /// </remarks>
    /// <param name="packedBuffer">Source buffer in a supported packed PFNC format.</param>
    /// <param name="endianness">
    /// Byte-order interpretation used during bit extraction. <see cref="ByteExtensions.Endianness.LittleEndian"/>
    /// uses forward byte indexing; <see cref="ByteExtensions.Endianness.BigEndian"/> reverses byte indices across the packed span.
    /// </param>
    /// <returns>A new <see cref="GcBuffer"/> containing unpacked image data (or the original buffer if it is not packed).</returns>
    /// <exception cref="ArgumentException">The pixel format of <paramref name="packedBuffer"/> is not supported for unpacking.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="endianness"/> value is invalid.</exception>
    public static GcBuffer Unpack(this GcBuffer packedBuffer, ByteExtensions.Endianness endianness = ByteExtensions.Endianness.LittleEndian)
    {
        ArgumentNullException.ThrowIfNull(packedBuffer, nameof(packedBuffer));

        // If the buffer is not packed, return it as-is.
        if (packedBuffer.IsPacked() == false)
            return packedBuffer;

        // Validate that the pixel format is supported.
        if (!SupportedPackedPixelFormats.Contains(packedBuffer.PixelFormat))
            throw new ArgumentException($"Pixel format {packedBuffer.PixelFormat} is not supported for unpacking.");

        // Validate endianness parameter.
        if (endianness is not (ByteExtensions.Endianness.BigEndian or ByteExtensions.Endianness.LittleEndian))
            throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Unsupported endianness.");

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
    /// Converts byte-aligned image data to packed PFNC representation.
    /// </summary>
    /// <remarks>
    /// Each sample is compacted to the packed bit depth for the corresponding PFNC packed format.
    /// A new <see cref="GcBuffer"/> is allocated for the result.
    /// </remarks>
    /// <param name="unpackedBuffer">Source buffer in an unpacked PFNC format that can be packed.</param>
    /// <param name="endianness">
    /// Byte-order interpretation used during bit insertion. <see cref="ByteExtensions.Endianness.LittleEndian"/>
    /// uses forward byte indexing; <see cref="ByteExtensions.Endianness.BigEndian"/> reverses byte indices across the packed span.
    /// </param>
    /// <returns>A new <see cref="GcBuffer"/> containing packed image data (or the original buffer if it is already packed).</returns>
    /// <exception cref="ArgumentException">The pixel format of <paramref name="unpackedBuffer"/> cannot be converted to a supported packed format.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="endianness"/> value is invalid.</exception>
    public static GcBuffer Pack(this GcBuffer unpackedBuffer, ByteExtensions.Endianness endianness = ByteExtensions.Endianness.LittleEndian)
    {
        ArgumentNullException.ThrowIfNull(unpackedBuffer, nameof(unpackedBuffer));

        // If the buffer is already packed, return it as-is.
        if (unpackedBuffer.IsPacked())
            return unpackedBuffer;

        // Validate that the pixel format is supported.
        if (!SupportedUnpackedPixelFormats.Contains(unpackedBuffer.PixelFormat))
            throw new ArgumentException($"Pixel format {unpackedBuffer.PixelFormat} is not supported for packing.");

        // Validate endianness parameter.
        if (endianness is not (ByteExtensions.Endianness.BigEndian or ByteExtensions.Endianness.LittleEndian))
            throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Unsupported endianness.");

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

    /// <summary>
    /// Determines whether the buffer's pixel format is packed.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <returns>true if the buffer's pixel format is packed; otherwise, false.</returns>
    public static bool IsPacked(this GcBuffer buffer) => GenICamHelper.IsPacked(buffer.PixelFormat);
}