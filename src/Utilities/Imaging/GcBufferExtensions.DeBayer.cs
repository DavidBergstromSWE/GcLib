using System;
using System.Collections.Generic;

namespace GcLib.Utilities.Imaging;

public static partial class GcBufferExtensions
{
    public static readonly List<PixelFormat> SupportedBayerFormats =
    [
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
    /// Determines whether the specified buffer contains Bayer-formatted image data.
    /// </summary>
    /// <param name="buffer">The buffer to evaluate.</param>
    /// <returns><see langword="true"/> if the buffer contains Bayer-formatted image data; otherwise, <see langword="false"/>.</returns>
    public static bool IsBayer(this GcBuffer buffer)
    {
        return GenICamHelper.GetPixelColorFilter(buffer.PixelFormat) != PixelColorFilter.None;
    }

    /// <summary>
    /// Converts Bayer-formatted image data to a demosaiced 3-channel color buffer in RGB or BGR order.
    /// </summary>
    /// <remarks>Supports BayerBGGR, BayerGBRG, BayerRGGB, and BayerGRBG input patterns.</remarks>
    /// <param name="bayerBuffer">Buffer containing Bayer-pattern image data.</param>
    /// <param name="colorOrder">Requested output channel order. Supported values are RGB and BGR.</param>
    /// <returns>A new <see cref="GcBuffer"/> containing the converted color image with matching metadata (or the original buffer if it is not Bayer-formatted).</returns>
    /// <exception cref="ArgumentException">The provided buffer does not contain Bayer-formatted image data.</exception>
    /// <exception cref="NotSupportedException">The requested color order is not supported, or the Bayer pattern in the pixel format is not supported for
    /// conversion.</exception>
    public static GcBuffer DeBayer(this GcBuffer bayerBuffer, ColorOrder colorOrder = ColorOrder.BGR)
    {
        ArgumentNullException.ThrowIfNull(bayerBuffer);

        // If the buffer is not Bayer-formatted, return it as-is.
        if (bayerBuffer.IsBayer() == false)
            return bayerBuffer;

        if (colorOrder is not (ColorOrder.RGB or ColorOrder.BGR))
            throw new NotSupportedException("Only RGB and BGR color orders are supported.");

        bayerBuffer = bayerBuffer.Unpack();
        var bitsPerChannel = GenICamHelper.GetBitsPerPixelPerChannel(bayerBuffer.PixelFormat);
        int bytesPerSample = (int)((bitsPerChannel + 7) / 8);

        // Allocate a new byte array for the debayered image data.
        var bytes = new byte[bayerBuffer.Width * bayerBuffer.Height * 3 * bytesPerSample];

        var pixelColorFilter = GenICamHelper.GetPixelColorFilter(bayerBuffer.PixelFormat);
        if (pixelColorFilter is PixelColorFilter.None)
            throw new NotSupportedException($"Bayer pattern '{pixelColorFilter}' is not supported for deBayering.");

        DemosaicToColor(rawData: bayerBuffer.GetTightImageData(),
                        colorData: bytes,
                        width: (int)bayerBuffer.Width,
                        height: (int)bayerBuffer.Height,
                        bytesPerSample: bytesPerSample,
                        pattern: pixelColorFilter,
                        colorOrder: colorOrder);

        return new GcBuffer(imageData: bytes, width: bayerBuffer.Width, height: bayerBuffer.Height, pixelFormat: GetDeBayerFormat(bayerBuffer.PixelFormat, colorOrder), bayerBuffer.FrameID, bayerBuffer.TimeStamp);

    }

    /// <summary>
    /// Gets the RGB or BGR pixel format that matches the specified Bayer format bit depth and output color order.
    /// </summary>
    /// <param name="bayerFormat">Bayer pixel format used to determine the per-channel bit depth.</param>
    /// <param name="colorOrder">Output channel order to use when selecting the destination pixel format.</param>
    /// <returns>A color pixel format in the requested channel order with the same per-channel bit depth as the Bayer format.</returns>
    /// <exception cref="NotSupportedException">Thrown when the color order is not supported or when the Bayer bit depth does not map to a supported RGB or BGR
    /// pixel format.</exception>
    private static PixelFormat GetDeBayerFormat(PixelFormat bayerFormat, ColorOrder colorOrder)
    {
        // Return pixelformat based on color order and bit depth of the bayer format. For example, if the bayer format is Mono8, return RGB8 or BGR8 based on color order.
        var bitDepth = GenICamHelper.GetBitsPerPixelPerChannel(bayerFormat);

        return colorOrder switch
        {
            ColorOrder.RGB => bitDepth switch
            {
                8 => PixelFormat.RGB8,
                10 => PixelFormat.RGB10,
                12 => PixelFormat.RGB12,
                14 => PixelFormat.RGB14,
                16 => PixelFormat.RGB16,
                _ => throw new NotSupportedException($"Bit depth {bitDepth} is not supported for RGB conversion.")
            },
            ColorOrder.BGR => bitDepth switch
            {
                8 => PixelFormat.BGR8,
                10 => PixelFormat.BGR10,
                12 => PixelFormat.BGR12,
                14 => PixelFormat.BGR14,
                16 => PixelFormat.BGR16,
                _ => throw new NotSupportedException($"Bit depth {bitDepth} is not supported for BGR conversion.")
            },
            _ => throw new NotSupportedException($"Color order {colorOrder} is not supported.")
        };
    }

    private static void DemosaicToColor(ReadOnlySpan<byte> rawData, Span<byte> colorData, int width, int height, int bytesPerSample, PixelColorFilter pattern, ColorOrder colorOrder)
    {
        for (int y = 0; y < height; y++)
        {
            int prevY = y > 0 ? y - 1 : y;
            int nextY = y < height - 1 ? y + 1 : y;
            int currRowOffset = y * width;
            int prevRowOffset = prevY * width;
            int nextRowOffset = nextY * width;
            bool isEvenRow = (y % 2 == 0);

            for (int x = 0; x < width; x++)
            {
                int prevX = x > 0 ? x - 1 : x;
                int nextX = x < width - 1 ? x + 1 : x;
                int rawIdx = currRowOffset + x;
                bool isEvenCol = (x % 2 == 0);

                int up = ReadRawSample(rawData, prevRowOffset + x, bytesPerSample);
                int down = ReadRawSample(rawData, nextRowOffset + x, bytesPerSample);
                int left = ReadRawSample(rawData, currRowOffset + prevX, bytesPerSample);
                int right = ReadRawSample(rawData, currRowOffset + nextX, bytesPerSample);
                int upLeft = ReadRawSample(rawData, prevRowOffset + prevX, bytesPerSample);
                int upRight = ReadRawSample(rawData, prevRowOffset + nextX, bytesPerSample);
                int downLeft = ReadRawSample(rawData, nextRowOffset + prevX, bytesPerSample);
                int downRight = ReadRawSample(rawData, nextRowOffset + nextX, bytesPerSample);
                int center = ReadRawSample(rawData, rawIdx, bytesPerSample);

                int b, g, r;

                switch (pattern)
                {
                    case PixelColorFilter.BayerBGGR:
                        if (isEvenRow && isEvenCol)
                        {
                            b = center; g = (up + down + left + right) >> 2; r = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        else if (isEvenRow)
                        {
                            g = center; b = (left + right) >> 1; r = (up + down) >> 1;
                        }
                        else if (isEvenCol)
                        {
                            g = center; b = (up + down) >> 1; r = (left + right) >> 1;
                        }
                        else
                        {
                            r = center; g = (up + down + left + right) >> 2; b = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        break;

                    case PixelColorFilter.BayerGBRG:
                        if (isEvenRow && isEvenCol)
                        {
                            g = center; b = (left + right) >> 1; r = (up + down) >> 1;
                        }
                        else if (isEvenRow)
                        {
                            b = center; g = (up + down + left + right) >> 2; r = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        else if (isEvenCol)
                        {
                            r = center; g = (up + down + left + right) >> 2; b = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        else
                        {
                            g = center; b = (up + down) >> 1; r = (left + right) >> 1;
                        }
                        break;

                    case PixelColorFilter.BayerRGGB:
                        if (isEvenRow && isEvenCol)
                        {
                            r = center; g = (up + down + left + right) >> 2; b = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        else if (isEvenRow)
                        {
                            g = center; b = (up + down) >> 1; r = (left + right) >> 1;
                        }
                        else if (isEvenCol)
                        {
                            g = center; b = (left + right) >> 1; r = (up + down) >> 1;
                        }
                        else
                        {
                            b = center; g = (up + down + left + right) >> 2; r = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        break;

                    case PixelColorFilter.BayerGRBG:
                        if (isEvenRow && isEvenCol)
                        {
                            g = center; b = (up + down) >> 1; r = (left + right) >> 1;
                        }
                        else if (isEvenRow)
                        {
                            r = center; g = (up + down + left + right) >> 2; b = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        else if (isEvenCol)
                        {
                            b = center; g = (up + down + left + right) >> 2; r = (upLeft + upRight + downLeft + downRight) >> 2;
                        }
                        else
                        {
                            g = center; b = (left + right) >> 1; r = (up + down) >> 1;
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Bayer pattern '{pattern}' is not supported for deBayering.");
                }

                if (colorOrder == ColorOrder.BGR)
                {
                    WriteColorSample(colorData, rawIdx * 3, bytesPerSample, b);
                    WriteColorSample(colorData, rawIdx * 3 + 1, bytesPerSample, g);
                    WriteColorSample(colorData, rawIdx * 3 + 2, bytesPerSample, r);
                }
                else
                {
                    WriteColorSample(colorData, rawIdx * 3, bytesPerSample, r);
                    WriteColorSample(colorData, rawIdx * 3 + 1, bytesPerSample, g);
                    WriteColorSample(colorData, rawIdx * 3 + 2, bytesPerSample, b);
                }
            }
        }
    }

    private static int ReadRawSample(ReadOnlySpan<byte> rawData, int pixelIndex, int bytesPerSample)
    {
        int byteIndex = pixelIndex * bytesPerSample;
        return bytesPerSample == 1
            ? rawData[byteIndex]
            : rawData[byteIndex] | (rawData[byteIndex + 1] << 8);
    }

    private static void WriteColorSample(Span<byte> output, int sampleIndex, int bytesPerSample, int value)
    {
        int byteIndex = sampleIndex * bytesPerSample;
        if (bytesPerSample == 1)
        {
            output[byteIndex] = (byte)value;
            return;
        }

        output[byteIndex] = (byte)(value & 0xFF);
        output[byteIndex + 1] = (byte)((value >> 8) & 0xFF);
    }


}