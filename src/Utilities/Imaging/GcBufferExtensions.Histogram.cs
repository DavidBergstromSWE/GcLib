using System;
using System.Buffers;
using System.Runtime.CompilerServices;

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
    public static unsafe void CalculateHistogram(this GcBuffer buffer, int bins, ref double[,] histogramData, bool decimate = true)
    {
        // ToDo: Compare performance with OpenCV's calcHist function (MatExtensions.CalculateHistogram) for various image sizes and formats to identify potential optimizations.
        // ToDo: Consider a parallelized version using Parallel.For or PLINQ for large images.
        // ToDo: Consider using a more efficient memory access pattern (e.g., blocking or tiling) to improve cache locality and reduce cache misses when processing large images.
        // ToDo: Consider using SIMD intrinsics (System.Numerics.Vector) for further performance improvements on supported hardware.
        // ToDo: Consider using a more efficient histogram calculation algorithm, such as a two-pass approach or a histogram equalization technique, for better performance on large images.
        // ToDo: Consider using a more efficient data structure for storing histogram data, such as a sparse representation or a compressed format, to reduce memory usage and improve performance on large images.
        // ToDo: Consider using a more efficient algorithm for calculating histograms of color images, such as a color quantization technique or a color space transformation, to reduce the number of bins and improve performance on large images.
        
        ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));

        // Validate histogram bin count.
        if (bins < 2 || bins > (int)buffer.PixelDynamicRangeMax + 1)
            throw new ArgumentOutOfRangeException(nameof(bins), bins, $"Histogram bin count must be greater than one and less than or equal to the buffer's pixel dynamic range + 1 ({buffer.PixelDynamicRangeMax + 1}).");

        // ToDo: Implement support for packed buffers (modify strides?).
        if (buffer.IsPacked())
            throw new ArgumentException("Packed buffers are not supported for histogram calculation. Please unpack the buffer before calling this method.", nameof(buffer));

        var sourceChannels = (int)buffer.NumChannels;
        var histogramChannels = sourceChannels == 1 ? 1 : 3; // For color images, ignore alpha channel if present.

        // Initialize histogram data array if null or size mismatch.
        var histogramBins = bins;
        if (histogramData == null || histogramData.GetLength(0) != histogramChannels || histogramData.GetLength(1) != histogramBins)
            histogramData = new double[histogramChannels, histogramBins];

        var bytesPerChannel = (int)(GenICamHelper.GetBitsPerPixelPerChannel(buffer.PixelFormat) / 8);
        var pixelStride = sourceChannels * bytesPerChannel; // Total bytes per pixel.
        var pixelCount = (int)(buffer.Width * buffer.Height); // Total number of pixels in the image.
        var planar = GenICamHelper.IsPlanar(buffer.PixelFormat);
        var planeSize = planar ? pixelCount * bytesPerChannel : 0; // Size of each channel plane in bytes for planar formats.
        var maxValue = buffer.PixelDynamicRangeMax;
        var histogramRange = maxValue + 1UL;
        var directBinLookup = histogramBins == (int)histogramRange;

        // Determine sampling stride for large images to improve performance.
        var sampleStride = 1;
        if (decimate && pixelCount > 400000)
            sampleStride = (pixelCount + 399999) / 400000;

        // Determine channel indices based on pixel format and color order.
        int sourceBlueIndex = 0;
        int sourceGreenIndex = 1;
        int sourceRedIndex = 2;
        int sourceAlphaIndex = sourceChannels == 4 ? 3 : -1;
        if (sourceChannels > 1)
        {
            var colorOrder = GenICamHelper.GetColorOrder(buffer.PixelFormat);
            var rgbOrder = colorOrder is ColorOrder.RGB or ColorOrder.RGBa;
            sourceBlueIndex = rgbOrder ? 2 : 0;
            sourceRedIndex = rgbOrder ? 0 : 2;
        }

        // Rent pooled histogram arrays for each channel.
        var histogramPool = ArrayPool<uint>.Shared;
        var histogram0 = histogramPool.Rent(histogramBins);
        uint[]? histogram1 = null;
        uint[]? histogram2 = null;

        try
        {
            histogram0.AsSpan(0, histogramBins).Clear();

            if (histogramChannels == 3)
            {
                histogram1 = histogramPool.Rent(histogramBins);
                histogram2 = histogramPool.Rent(histogramBins);
                histogram1.AsSpan(0, histogramBins).Clear();
                histogram2.AsSpan(0, histogramBins).Clear();
            }

            // Process image data to populate histogram arrays.
            fixed (byte* basePointer = buffer.ImageData)
            {
                if (histogramChannels == 1) // For single-channel images, process all pixels in one loop.
                {
                    for (int pixel = 0; pixel < pixelCount; pixel += sampleStride)
                    {
                        var samplePointer = basePointer + pixel * bytesPerChannel;
                        var value = ReadValue(samplePointer, bytesPerChannel);
                        var bin = GetBin(value, maxValue, histogramBins, histogramRange, directBinLookup);
                        histogram0[bin]++;
                    }
                }
                else if (planar) // For planar images, process each channel plane separately.
                {
                    var bluePlane = basePointer + sourceBlueIndex * planeSize;
                    var greenPlane = basePointer + sourceGreenIndex * planeSize;
                    var redPlane = basePointer + sourceRedIndex * planeSize;
                    var alphaPlane = sourceAlphaIndex >= 0 ? basePointer + sourceAlphaIndex * planeSize : null;

                    for (int pixel = 0; pixel < pixelCount; pixel += sampleStride)
                    {
                        var channelOffset = pixel * bytesPerChannel;

                        if (alphaPlane != null && ReadValue(alphaPlane + channelOffset, bytesPerChannel) == 0)
                            continue;

                        histogram0[GetBin(ReadValue(bluePlane + channelOffset, bytesPerChannel), maxValue, histogramBins, histogramRange, directBinLookup)]++;
                        histogram1![GetBin(ReadValue(greenPlane + channelOffset, bytesPerChannel), maxValue, histogramBins, histogramRange, directBinLookup)]++;
                        histogram2![GetBin(ReadValue(redPlane + channelOffset, bytesPerChannel), maxValue, histogramBins, histogramRange, directBinLookup)]++;
                    }
                }
                else // For interleaved images, process all channels in one loop.
                {
                    for (int pixel = 0; pixel < pixelCount; pixel += sampleStride)
                    {
                        var pixelPointer = basePointer + pixel * pixelStride;

                        if (sourceAlphaIndex >= 0 && ReadValue(pixelPointer + sourceAlphaIndex * bytesPerChannel, bytesPerChannel) == 0)
                            continue;

                        histogram0[GetBin(ReadValue(pixelPointer + sourceBlueIndex * bytesPerChannel, bytesPerChannel), maxValue, histogramBins, histogramRange, directBinLookup)]++;
                        histogram1![GetBin(ReadValue(pixelPointer + sourceGreenIndex * bytesPerChannel, bytesPerChannel), maxValue, histogramBins, histogramRange, directBinLookup)]++;
                        histogram2![GetBin(ReadValue(pixelPointer + sourceRedIndex * bytesPerChannel, bytesPerChannel), maxValue, histogramBins, histogramRange, directBinLookup)]++;
                    }
                }
            }

            // Copy histogram arrays to output histogram data.
            for (int i = 0; i < histogramBins; i++)
                histogramData[0, i] = histogram0[i];

            // For color images, copy green and red channel histograms.
            if (histogramChannels == 3)
            {
                for (int i = 0; i < histogramBins; i++)
                    histogramData[1, i] = histogram1![i];

                for (int i = 0; i < histogramBins; i++)
                    histogramData[2, i] = histogram2![i];
            }
        }
        finally
        {
            histogramPool.Return(histogram0, clearArray: false);

            if (histogram1 != null)
                histogramPool.Return(histogram1, clearArray: false);

            if (histogram2 != null)
                histogramPool.Return(histogram2, clearArray: false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetBin(ulong value, ulong maxValue, int bins, ulong histogramRange, bool directBinLookup)
    {
        if (value > maxValue)
            value = maxValue;

        if (directBinLookup)
            return (int)value;

        var bin = (int)(value * (ulong)bins / histogramRange);
        return bin < bins ? bin : bins - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong ReadValue(byte* pointer, int bytesPerChannel)
    {
        return bytesPerChannel switch
        {
            1 => pointer[0],
            2 => Unsafe.ReadUnaligned<ushort>(pointer),
            4 => Unsafe.ReadUnaligned<uint>(pointer),
            8 => Unsafe.ReadUnaligned<ulong>(pointer),
            _ => throw new NotSupportedException($"Pixel channel width of {bytesPerChannel} bytes is not supported.")
        };
    }
}

