using System;
using System.Buffers.Binary;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Provides extension methods for converting <see cref="GcBuffer"/> instances between packed and unpacked PFNC pixel formats and debayering.
/// </summary>
public static partial class GcBufferExtensions
{
    /// <summary>
    /// Calculates image histogram data from a <see cref="GcBuffer"/>.
    /// </summary>
    /// <remarks>The method uses the buffer's pixel dynamic range to determine the number of histogram bins if not specified.</remarks>
    /// <param name="buffer">Input image buffer.</param>
    /// <param name="histogramData">Output histogram data, where first dimension is channel and second dimension is bin index.</param>
    /// <param name="decimate">Decimate input data for improved performance on large images.</param>
    public static void CalculateHistogram(this GcBuffer buffer, ref double[,] histogramData, bool decimate = true)
    {
        CalculateHistogram(buffer, (int)buffer.PixelDynamicRangeMax, ref histogramData, decimate);
    }

    /// <summary>
    /// Calculates image histogram data from a <see cref="GcBuffer"/>.
    /// </summary>
    /// <param name="buffer">Input image buffer.</param>
    /// <param name="bins">Number of histogram bins.</param>
    /// <param name="histogramData">Output histogram data, where first dimension is channel and second dimension is bin index.</param>
    /// <param name="decimate">Decimate input data for improved performance on large images.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bins"/> is less than 2 or greater than the buffer's pixel dynamic range.</exception>
    /// <exception cref="NotSupportedException">Thrown when the buffer channel count is not 1, 3 or 4.</exception>
    /// <remarks>For 4-channel images, the alpha channel is used as a mask and only pixels with non-zero alpha are included in the histogram.</remarks>
    public static void CalculateHistogram(this GcBuffer buffer, int bins, ref double[,] histogramData, bool decimate = true)
    {
        ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));

        // Validate histogram bin count against buffer's pixel dynamic range.
        if (bins < 2 || bins > (int)buffer.PixelDynamicRangeMax + 1)
            throw new ArgumentOutOfRangeException(nameof(bins), bins, $"Histogram bin count must be greater than one and less than or equal to the buffer's pixel dynamic range + 1 ({buffer.PixelDynamicRangeMax + 1}).");

        // Number of histogram bins.
        var histogramBins = bins;

        // Ensure byte-aligned source data for packed PFNC pixel formats.
        var sourceBuffer = buffer.IsPacked() ? buffer.Unpack() : buffer;

        // Allocate histogram data array if not already allocated or if dimensions do not match the number of channels and bins.
        var sourceChannels = (int)sourceBuffer.NumChannels; // Get the number of channels in the source buffer.
        var histogramChannels = sourceChannels == 4 ? 3 : sourceChannels; // For 4-channel images, only 3 channels are used for histogram calculation (alpha channel is used as a mask).
        if (histogramData == null || histogramData.GetLength(0) != histogramChannels || histogramData.GetLength(1) != histogramBins)
            histogramData = new double[histogramChannels, histogramBins]; // Initialize histogram data array with the appropriate dimensions.
        else Array.Clear(histogramData, 0, histogramData.Length); // Clear existing histogram data if the array is already allocated.

        // Calculate the stride between pixels (in bytes) based on the source buffer's pixel format.
        var bytesPerChannel = (int)(GenICamHelper.GetBitsPerPixelPerChannel(sourceBuffer.PixelFormat) / 8); // Calculate the number of bytes per channel based on bits per channel. Only correct for unpacked PFNC pixel formats.
        var pixelStride = sourceChannels * bytesPerChannel; // Stride between pixels in bytes for interleaved formats.

        // Total number of pixels in the image.
        var pixelCount = (int)(sourceBuffer.Width * sourceBuffer.Height);

        // Calculate planar size for planar formats (separate planes for each channel).
        var planar = GenICamHelper.IsPlanar(sourceBuffer.PixelFormat); // Check if the pixel format is planar (separate planes for each channel).
        var planeSize = planar ? pixelCount * bytesPerChannel : 0; // Size of each color plane in bytes for planar formats.

        // Calculate the histogram range based on the buffer's pixel dynamic range. The range is used to scale pixel values into histogram bins.
        ulong histogramRange = sourceBuffer.PixelDynamicRangeMax + 1UL; // (0 to dynamic range maximum inclusive)

        // Decimate input data for improved performance on large images.
        var sampleStride = 1; // Decimate input data for improved performance on large images.
        if (decimate && pixelCount > 100000)
            sampleStride = (pixelCount + 99999) / 100000; // Calculate stride to sample approximately 100,000 pixels.

        // Get a span over the image data for efficient access.
        var data = sourceBuffer.ImageData.AsSpan(); 

        // Iterate over each pixel in the image, applying decimation if necessary.
        for (int pixel = 0; pixel < pixelCount; pixel += sampleStride)
        {
            // For single-channel images, update the histogram for the single channel.
            if (sourceChannels == 1)
            {
                int offset = pixel * bytesPerChannel;
                UpdateBin(source: data, offset: offset, bytesPerChannel: bytesPerChannel, channel: 0, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
                continue;
            }

            // For multi-channel images, determine the source channel order and update the histogram for each channel.
            var colorOrder = GenICamHelper.GetColorOrder(sourceBuffer.PixelFormat);
            int sourceBlueIndex = colorOrder is ColorOrder.RGB or ColorOrder.RGBa ? 2 : 0;
            int sourceGreenIndex = 1;
            int sourceRedIndex = colorOrder is ColorOrder.RGB or ColorOrder.RGBa ? 0 : 2;

            // For 4-channel images, use the alpha channel as a mask to include only pixels with non-zero alpha in the histogram.
            if (sourceChannels == 4)
            {
                int alphaOffset = planar ? pixel * bytesPerChannel + 3 * planeSize : pixel * pixelStride + 3 * bytesPerChannel; // Calculate the offset for the alpha channel based on whether the format is planar or interleaved.
                if (ReadValue(data, alphaOffset, bytesPerChannel) == 0)
                    continue;
            }

            // For planar formats, update the histogram for each channel using the appropriate offsets for each color plane.
            if (planar)
            {
                // Planar RGB/BGR channel storage; alpha plane is ignored.
                UpdateBin(source: data, offset: pixel * bytesPerChannel + sourceBlueIndex * planeSize, bytesPerChannel: bytesPerChannel, channel: 0, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
                UpdateBin(source: data, offset: pixel * bytesPerChannel + sourceGreenIndex * planeSize, bytesPerChannel: bytesPerChannel, channel: 1, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
                UpdateBin(source: data, offset: pixel * bytesPerChannel + sourceRedIndex * planeSize, bytesPerChannel: bytesPerChannel, channel: 2, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
                continue;
            }

            // For interleaved formats, calculate the pixel offset and update the histogram for each channel based on the source channel order.
            int pixelOffset = pixel * pixelStride;
            UpdateBin(source: data, offset: pixelOffset + sourceBlueIndex * bytesPerChannel, bytesPerChannel: bytesPerChannel, channel: 0, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
            UpdateBin(source: data, offset: pixelOffset + sourceGreenIndex * bytesPerChannel, bytesPerChannel: bytesPerChannel, channel: 1, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
            UpdateBin(source: data, offset: pixelOffset + sourceRedIndex * bytesPerChannel, bytesPerChannel: bytesPerChannel, channel: 2, bins: histogramBins, histogramRange: histogramRange, maxValue: sourceBuffer.PixelDynamicRangeMax, histogramData: histogramData);
        }
    }

    /// <summary>
    /// Reads a channel sample from the source data, maps it to a histogram bin, and increments the corresponding bin
    /// count.
    /// </summary>
    /// <remarks>Values greater than maxValue are clamped before scaling, and the computed bin index is capped
    /// to the last valid bin.</remarks>
    /// <param name="source">Source bytes containing packed channel sample data.</param>
    /// <param name="offset">Zero-based position in source where the sample value begins.</param>
    /// <param name="bytesPerChannel">Number of bytes used to read the sample value for one channel.</param>
    /// <param name="channel">Zero-based channel index used for the first dimension of histogramData.</param>
    /// <param name="bins">Total number of histogram bins available per channel.</param>
    /// <param name="histogramRange">Range divisor used to scale the sample value into a bin index.</param>
    /// <param name="maxValue">Maximum allowed sample value before bin calculation.</param>
    /// <param name="histogramData">Two-dimensional histogram array indexed by channel and bin, updated in place.</param>
    private static void UpdateBin(ReadOnlySpan<byte> source, int offset, int bytesPerChannel, int channel, int bins, ulong histogramRange, ulong maxValue, double[,] histogramData)
    {
        // Read the channel sample value from the source data at the specified offset and convert it to ulong.
        ulong value = ReadValue(source, offset, bytesPerChannel);

        // Clamp the value to maxValue if it exceeds the allowed range.
        if (value > maxValue)
            value = maxValue;

        // Scale the value to a histogram bin index based on the histogram range and total number of bins.
        var bin = (int)(value * (ulong)bins / histogramRange);
        if (bin >= bins)
            bin = bins - 1; // Ensure the bin index does not exceed the last valid bin.

        // Increment the corresponding histogram bin count for the specified channel.
        histogramData[channel, bin]++;
    }

    /// <summary>
    /// Reads an unsigned channel value from a byte span at the specified offset using little-endian byte order.
    /// </summary>
    /// <remarks>Uses direct byte access for 1-byte values and <see
    /// cref="BinaryPrimitives"/> little-endian readers for multi-byte values.</remarks>
    /// <param name="source">The source bytes containing channel data.</param>
    /// <param name="offset">The zero-based index of the first byte to read.</param>
    /// <param name="bytesPerChannel">The channel width in bytes. Supported values are 1, 2, 4, and 8.</param>
    /// <returns>The channel value converted to <see cref="ulong"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="bytesPerChannel"/> is not 1, 2, 4, or 8.</exception>
    private static ulong ReadValue(ReadOnlySpan<byte> source, int offset, int bytesPerChannel)
    {
        return bytesPerChannel switch
        {
            1 => source[offset],
            2 => BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2)),
            4 => BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset, 4)),
            8 => BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset, 8)),
            _ => throw new NotSupportedException($"Pixel channel width of {bytesPerChannel} bytes is not supported.")
        };
    }
}
