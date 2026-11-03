using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Provides conversion helpers between GenICam pixel metadata and <see cref="Emgu.CV"/> matrix representations.
/// </summary>
public static class EmguHelper
{
    /// <summary>
    /// Gets the <see cref="DepthType"/> large enough to fit a GenICam PFNC <see cref="PixelSize"/>.
    /// </summary>
    /// <param name="pixelSize">The GenICam PFNC pixel size to convert.</param>
    /// <returns>A depth type large enough to fit the pixel size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The pixel size is less than 1 or greater than 64.</exception>
    public static DepthType GetDepthType(PixelSize pixelSize)
    {
        // Validate that the pixel size is within the supported range.
        if ((int)pixelSize < 1 || (int)pixelSize > 64)
            throw new ArgumentOutOfRangeException(nameof(pixelSize), pixelSize, "Pixel size must be between 1 and 64 bits.");

        // Map pixel size to the appropriate depth type, based on the number of bits required to represent the pixel size.
        if ((int)pixelSize <= 8)
            return DepthType.Cv8U;
        else if ((int)pixelSize <= 16)
            return DepthType.Cv16U;
        else if ((int)pixelSize <= 32)
            return DepthType.Cv32U;
        else if ((int)pixelSize <= 64)
            return DepthType.Cv64U;
        else throw new ArgumentOutOfRangeException(nameof(pixelSize), pixelSize, "Pixel size is out of range for supported depth types.");
    }

    /// <summary>
    /// Maps a GenICam PFNC <see cref="PixelFormat"/> to the corresponding <see cref="DepthType"/>.
    /// </summary>
    /// <param name="pixelFormat">GenICam PFNC pixel format.</param>
    /// <returns>The corresponding <see cref="DepthType"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the pixel size of <paramref name="pixelFormat"/> is greater than 64.</exception>
    public static DepthType GetDepthType(PixelFormat pixelFormat)
    {
        return GetDepthType(GenICamHelper.GetPixelSize(pixelFormat));
    }

    /// <summary>
    /// Maps a numeric CLR <see cref="Type"/> to the corresponding <see cref="DepthType"/>.
    /// </summary>
    /// <param name="type">CLR type to map.</param>
    /// <returns>The mapped <see cref="DepthType"/>; unsupported numeric types return <see cref="DepthType.Default"/>.</returns>
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
    /// Gets the nominal bit depth (number of bits needed to represent a value) for a <see cref="DepthType"/>.
    /// </summary>
    /// <param name="depthType">Depth type to evaluate.</param>
    /// <returns>The number of bits used by <paramref name="depthType"/>.</returns>
    public static int GetBitDepth(DepthType depthType)
    {
        return depthType switch
        {
            DepthType.CvBool => 1,
            DepthType.Cv8U or DepthType.Cv8S => 8,
            DepthType.Cv16S or DepthType.Cv16U or DepthType.Cv16F => 16,
            DepthType.Cv32S or DepthType.Cv32U or DepthType.Cv32F => 32,
            DepthType.Cv64S or DepthType.Cv64U or DepthType.Cv64F => 64,
            _ => throw new ArgumentException($"Depth type {depthType} does not have a defined bit depth!")
        };
    }

    /// <summary>
    /// Gets the maximum representable value for a given <see cref="DepthType"/>.
    /// </summary>
    /// <param name="depthType">Depth type to evaluate.</param>
    /// <returns>The maximum representable value.</returns>
    public static double GetMax(DepthType depthType)
    {
        return depthType switch
        {
            DepthType.CvBool => 1,
            DepthType.Cv8U => byte.MaxValue,
            DepthType.Cv8S => sbyte.MaxValue,
            DepthType.Cv16U => ushort.MaxValue,
            DepthType.Cv16S => short.MaxValue,
            DepthType.Cv16F => (double)Half.MaxValue,
            DepthType.Cv32U => uint.MaxValue,
            DepthType.Cv32S => int.MaxValue,
            DepthType.Cv32F => float.MaxValue,
            DepthType.Cv64S => long.MaxValue,
            DepthType.Cv64U => ulong.MaxValue,
            DepthType.Cv64F => double.MaxValue,
            _ => throw new ArgumentException($"Depth type {depthType} does not have a defined maximum value!")
        };
    }

    /// <summary>
    /// Gets the minimum representable value for a given <see cref="DepthType"/>.
    /// </summary>
    /// <param name="depthType">Depth type to evaluate.</param>
    /// <returns>The minimum representable value.</returns>
    public static double GetMin(DepthType depthType)
    {
        return depthType switch
        {
            DepthType.CvBool => 0,
            DepthType.Cv8U => byte.MinValue,
            DepthType.Cv8S => sbyte.MinValue,
            DepthType.Cv16U => ushort.MinValue,
            DepthType.Cv16S => short.MinValue,
            DepthType.Cv16F => (double)Half.MinValue,
            DepthType.Cv32U => uint.MinValue,
            DepthType.Cv32S => int.MinValue,
            DepthType.Cv32F => float.MinValue,
            DepthType.Cv64S => long.MinValue,
            DepthType.Cv64U => ulong.MinValue,
            DepthType.Cv64F => double.MinValue,
            _ => throw new ArgumentException($"Depth type {depthType} does not have a defined minimum value!")
        };
    }

    /// <summary>
    /// Converts EmguCV depth type and number of channels to GenICam PFNC pixel format.
    /// </summary>
    /// <param name="depthType">EmguCV depth type.</param>
    /// <param name="channels">Number of channels.</param>
    /// <returns>GenICam PFNC pixel format.</returns>
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
                return PixelFormat.BGR8; // OpenCV uses BGR by default
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.BGR16; // OpenCV uses BGR by default
            else return PixelFormat.InvalidPixelFormat;
        }
        else if (channels == 4)
        {
            if (depthType == DepthType.Cv8U)
                return PixelFormat.BGRa8;
            else if (depthType == DepthType.Cv16U)
                return PixelFormat.BGRa16;
            else return PixelFormat.InvalidPixelFormat;
        }
        else return PixelFormat.InvalidPixelFormat;
    }

    /// <summary>
    /// Converts a matrix from one GenICam PFNC pixel format to another.
    /// </summary>
    /// <param name="mat">The matrix to convert.</param>
    /// <param name="sourcePixelFormat">The original GenICam PFNC pixel format.</param>
    /// <param name="targetPixelFormat">The targeted GenICam PFNC pixel format.</param>
    /// <returns>The converted matrix. If the pixel formats are the same, returns <paramref name="mat"/> without conversion.</returns>
    /// <exception cref="NotSupportedException">A pixel format is invalid, packed or Bayer-patterned, or the requested color conversion is unsupported.</exception>
    internal static Mat ConvertMat(Mat mat, PixelFormat sourcePixelFormat, PixelFormat targetPixelFormat)
    {
        // ToDo: Add support for packed pixel formats and Bayer-patterned pixel formats, if needed.

        if (sourcePixelFormat == PixelFormat.InvalidPixelFormat || targetPixelFormat == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Conversion of invalid pixel format is not supported.");

        if (GenICamHelper.IsPacked(sourcePixelFormat) || GenICamHelper.IsPacked(targetPixelFormat))
            throw new NotSupportedException($"Conversion of packed pixel formats is not supported.");

        if (GenICamHelper.GetPixelColorFilter(sourcePixelFormat) != PixelColorFilter.None ||
            GenICamHelper.GetPixelColorFilter(targetPixelFormat) != PixelColorFilter.None)
            throw new NotSupportedException("Conversion of Bayer pixel formats is not supported.");

        // If the pixel formats are the same, no conversion is needed.
        if (sourcePixelFormat == targetPixelFormat)
            return mat;

        // Convert the matrix to the target depth type.
        mat.ConvertTo(m: mat, rtype: GetDepthType(targetPixelFormat), alpha: (double)GenICamHelper.GetPixelDynamicRangeMax(targetPixelFormat) / GenICamHelper.GetPixelDynamicRangeMax(sourcePixelFormat), beta: 0); // Scale the pixel values to match the new depth type

        // Determine appropriate color conversion code based on the source and target pixel formats.

        if (!GenICamHelper.IsColor(sourcePixelFormat) && !GenICamHelper.IsColor(targetPixelFormat))
            return mat; // No color conversion needed for grayscale formats

        if (GenICamHelper.IsColor(sourcePixelFormat) && GenICamHelper.IsColor(targetPixelFormat) && GenICamHelper.GetColorOrder(sourcePixelFormat) == GenICamHelper.GetColorOrder(targetPixelFormat))
            return mat; // No color conversion needed for same color order

        // Determine the appropriate color conversion code based on the source and target pixel formats.
        var conversionCode = GetColorConversionCode(sourcePixelFormat, targetPixelFormat);

        // Perform color conversion using determined conversion code.
        CvInvoke.CvtColor(mat, mat, conversionCode);

        // Emgu initializes new alpha to the storage maximum; correct it only for reduced-range formats.
        if (GenICamHelper.GetNumChannels(sourcePixelFormat) < 4 && mat.NumberOfChannels == 4 && GenICamHelper.GetPixelDynamicRangeMax(targetPixelFormat) != GetMax(mat.Depth))
        {
            using var alpha = new Mat(mat.Rows, mat.Cols, mat.Depth, 1); // Create a single-channel matrix for the alpha channel
            alpha.SetTo(new Emgu.CV.Structure.MCvScalar(GenICamHelper.GetPixelDynamicRangeMax(targetPixelFormat))); // Set the alpha channel to the maximum value for the target pixel format
            CvInvoke.InsertChannel(alpha, mat, 3); // Insert the alpha channel into the 4th channel of the converted matrix
        }

        return mat;
    }

    /// <summary>
    /// Unpacks tightly bit-packed image data into a 16-bit-per-channel matrix.
    /// </summary>
    /// <param name="packedData">The packed image data, with each row padded to a byte boundary.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="bitDepth">Bits per channel; supported values are 1, 2, 4, 10, 12, and 14.</param>
    /// <param name="channels">Channels per pixel; supported values are 1, 3, and 4.</param>
    /// <param name="endianness">Byte ordering for the packed data. LittleEndian traverses the packed array forward; BigEndian reverses byte indices across the entire packed array without reversing bits.</param>
    /// <returns>A matrix containing the unpacked image data.</returns>
    /// <exception cref="ArgumentException">The packed data is too short for the specified image dimensions.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The dimensions, bit depth, channel count, or endianness are invalid.</exception>
    public static Mat Unpack(byte[] packedData, int width, int height, int bitDepth, int channels, ByteExtensions.Endianness endianness = ByteExtensions.Endianness.LittleEndian)
    {
        ArgumentNullException.ThrowIfNull(packedData);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        // Validate endianness parameter.
        if (endianness is not (ByteExtensions.Endianness.BigEndian or ByteExtensions.Endianness.LittleEndian))
            throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Unsupported endianness.");

        // Validate bit depth and channel count parameters.
        if (bitDepth is not (1 or 2 or 4 or 10 or 12 or 14))
            throw new ArgumentOutOfRangeException(nameof(bitDepth), bitDepth, "Bit depth must be 1, 2, 4, 10, 12, or 14.");
        if (channels is not (1 or 3 or 4))
            throw new ArgumentOutOfRangeException(nameof(channels), channels, "Channel count must be 1, 3, or 4.");

        // Calculate the required length of the packed data based on the image dimensions, bit depth, and channel count.
        int samplesPerRow = checked(width * channels);
        int rowBytes = checked((int)(((long)samplesPerRow * bitDepth + 7) / 8));
        int requiredLength = checked(rowBytes * height);
        if (packedData.Length < requiredLength)
            throw new ArgumentException($"Packed data must contain at least {requiredLength} bytes.", nameof(packedData));

        // Create a new matrix to hold the unpacked image data, with 16 bits per channel.
        var dst = new Mat(height, width, DepthType.Cv16U, channels);
        uint valueMask = (1u << bitDepth) - 1; // Mask to extract the valid bits for the specified bit depth.

        // Unpack the packed data into the destination matrix.
        unsafe
        {
            for (int y = 0; y < height; y++)
            {
                int sourceRowOffset = y * rowBytes; // Offset in the packed data for the current row.
                ushort* dstRow = (ushort*)((byte*)dst.DataPointer + (long)y * dst.Step); // Pointer to the current row in the destination matrix.

                // Iterate through each sample in the current row and extract the packed bits into the destination matrix.
                for (int sample = 0; sample < samplesPerRow; sample++)
                {
                    long bitOffset = (long)sample * bitDepth; // Bit offset in the packed data for the current sample.
                    int byteOffset = (int)(bitOffset / 8); // Byte offset in the packed data for the current sample.
                    int bitShift = (int)(bitOffset % 8); // Bit shift to align the packed value for extraction.
                    uint packedValue = 0; // Variable to hold the packed value for the current sample.

                    for (int byteIndex = 0; byteIndex < 3 && byteOffset + byteIndex < rowBytes; byteIndex++)
                    {
                        int sourceByteIndex = sourceRowOffset + byteOffset + byteIndex; // Index in the packed data for the current byte of the packed value.
                        if (endianness is ByteExtensions.Endianness.BigEndian)
                            sourceByteIndex = packedData.Length - 1 - sourceByteIndex; // Adjust index for big-endian byte order.

                        packedValue |= (uint)packedData[sourceByteIndex] << (byteIndex * 8); // Combine the bytes into a single packed value.
                    }

                    dstRow[sample] = (ushort)((packedValue >> bitShift) & valueMask); // Extract the valid bits for the current sample and store it in the destination matrix.
                }
            }
        }

        return dst;
    }

    /// <summary>
    /// Packs an 8- or 16-bit unsigned-per-channel matrix into tightly bit-packed image data.
    /// Each row is padded to a byte boundary.
    /// </summary>
    /// <param name="mat">The matrix containing the unpacked image data. Non-contiguous rows are supported.</param>
    /// <param name="bitDepth">Bits per channel; supported values are 1, 2, 4, 10, 12, and 14.</param>
    /// <param name="endianness">Byte ordering for the packed data. LittleEndian traverses the packed array forward; BigEndian reverses byte indices across the entire packed array without reversing bits.</param>
    /// <returns>The packed image data.</returns>
    /// <exception cref="ArgumentException">The matrix has an unsupported depth, channel count, or sample value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The bit depth or endianness is invalid.</exception>
    public static byte[] Pack(Mat mat, int bitDepth, ByteExtensions.Endianness endianness = ByteExtensions.Endianness.LittleEndian)
    {
        ArgumentNullException.ThrowIfNull(mat);

        // Validate endianness parameter.
        if (endianness is not (ByteExtensions.Endianness.BigEndian or ByteExtensions.Endianness.LittleEndian))
            throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Unsupported endianness.");

        // Validate bit depth and matrix properties.
        if (bitDepth is not (1 or 2 or 4 or 10 or 12 or 14))
            throw new ArgumentOutOfRangeException(nameof(bitDepth), bitDepth, "Bit depth must be 1, 2, 4, 10, 12, or 14.");
        if (mat.NumberOfChannels is not (1 or 3 or 4))
            throw new ArgumentException("Matrix must have 1, 3, or 4 channels.", nameof(mat));

        if (mat.Depth != DepthType.Cv16U && mat.Depth != DepthType.Cv8U)
            throw new ArgumentException("Matrix depth must be 8 or 16-bit unsigned.", nameof(mat));

        if (mat.Cols <= 0 || mat.Rows <= 0)
            throw new ArgumentException("Matrix must have non-zero dimensions.", nameof(mat));

        int samplesPerRow = checked(mat.Cols * mat.NumberOfChannels);
        int rowBytes = checked((int)(((long)samplesPerRow * bitDepth + 7) / 8));
        var packedData = new byte[checked(rowBytes * mat.Rows)];
        ushort valueMask = (ushort)((1 << bitDepth) - 1);

        unsafe
        {
            for (int y = 0; y < mat.Rows; y++)
            {
                byte* sourceRow = (byte*)mat.DataPointer + (long)y * mat.Step;
                int destinationRowOffset = y * rowBytes;

                for (int sample = 0; sample < samplesPerRow; sample++)
                {
                    ushort value = mat.Depth == DepthType.Cv8U ? sourceRow[sample] : ((ushort*)sourceRow)[sample];
                    if (value > valueMask)
                        throw new ArgumentException($"Sample at row {y}, index {sample} exceeds the {bitDepth}-bit range.", nameof(mat));

                    long bitOffset = (long)sample * bitDepth;
                    for (int bit = 0; bit < bitDepth; bit++)
                    {
                        if ((value & (1 << bit)) != 0)
                        {
                            long destinationBit = bitOffset + bit;
                            int destinationByteIndex = destinationRowOffset + (int)(destinationBit / 8);
                            if (endianness is ByteExtensions.Endianness.BigEndian)
                                destinationByteIndex = packedData.Length - 1 - destinationByteIndex;

                            packedData[destinationByteIndex] |= (byte)(1 << (int)(destinationBit % 8));
                        }
                    }
                }
            }
        }

        return packedData;
    }

    /// <summary>
    /// Gets the conversion code for grayscale or RGB/BGR pixel formats, with or without alpha.
    /// </summary>
    /// <param name="sourcePixelFormat">The source pixel format.</param>
    /// <param name="targetPixelFormat">The target pixel format.</param>
    /// <returns>The color conversion code for the specified pixel formats.</returns>
    /// <exception cref="NotSupportedException">The conversion from the source pixel format to the target pixel format is not supported.</exception>
    private static ColorConversion GetColorConversionCode(PixelFormat sourcePixelFormat, PixelFormat targetPixelFormat)
    {
        // If the source pixel format is not a color format, determine the appropriate conversion code based on the target pixel format's color order.
        if (GenICamHelper.IsColor(sourcePixelFormat) == false)
        {
            return GenICamHelper.GetColorOrder(targetPixelFormat) switch
            {
                ColorOrder.BGR => ColorConversion.Gray2Bgr,
                ColorOrder.RGB => ColorConversion.Gray2Rgb,
                ColorOrder.BGRa => ColorConversion.Gray2Bgra,
                ColorOrder.RGBa => ColorConversion.Gray2Rgba,
                _ => throw new NotSupportedException($"Conversion from {sourcePixelFormat} to {targetPixelFormat} is not supported.")
            };
        }

        // If the target pixel format is not a color format, determine the appropriate conversion code based on the source pixel format's color order.
        if (GenICamHelper.IsColor(targetPixelFormat) == false)
        {
            return GenICamHelper.GetColorOrder(sourcePixelFormat) switch
            {
                ColorOrder.BGR => ColorConversion.Bgr2Gray,
                ColorOrder.RGB => ColorConversion.Rgb2Gray,
                ColorOrder.BGRa => ColorConversion.Bgra2Gray,
                ColorOrder.RGBa => ColorConversion.Rgba2Gray,
                _ => throw new NotSupportedException($"Conversion from {sourcePixelFormat} to {targetPixelFormat} is not supported.")
            };
        }

        // Determine the appropriate conversion code based on the source and target pixel formats' color orders.
        return GenICamHelper.GetColorOrder(sourcePixelFormat) switch
        {
            ColorOrder.BGR when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGB => ColorConversion.Bgr2Rgb,
            ColorOrder.RGB when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGR => ColorConversion.Rgb2Bgr,
            ColorOrder.BGR when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGRa => ColorConversion.Bgr2Bgra,
            ColorOrder.BGRa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGR => ColorConversion.Bgra2Bgr,
            ColorOrder.RGB when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGBa => ColorConversion.Rgb2Rgba,
            ColorOrder.RGBa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGB => ColorConversion.Rgba2Rgb,
            ColorOrder.BGRa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGBa => ColorConversion.Bgra2Rgba,
            ColorOrder.RGBa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGRa => ColorConversion.Rgba2Bgra,
            ColorOrder.BGR when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGBa => ColorConversion.Bgr2Rgba,
            ColorOrder.RGBa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGR => ColorConversion.Rgba2Bgr,
            ColorOrder.RGB when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.BGRa => ColorConversion.Rgb2Bgra,
            ColorOrder.BGRa when GenICamHelper.GetColorOrder(targetPixelFormat) == ColorOrder.RGB => ColorConversion.Bgra2Rgb,
            _ => throw new NotSupportedException($"Conversion from {sourcePixelFormat} to {targetPixelFormat} is not supported.")
        };
    }
}