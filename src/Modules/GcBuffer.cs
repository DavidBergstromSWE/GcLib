using System;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;

namespace GcLib;

/// <summary>
/// Represents the buffer level in the GenICam/GenTL standard module hierarchy and is responsible for the storage and format conversion of an image buffer. Image data is stored as a managed byte array, with metadata such as frame ID, timestamp, width, height and pixel format stored as properties. 
/// </summary>
public sealed class GcBuffer
{
    // ToDo: Implement Guard for boundary checking?

    #region Indexers

    /// <summary>
    /// Getter/setter of pixel value at specified coordinate.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <returns>Pixel value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double[] this[uint row, uint col]
    {
        get => GetPixel(row, col);
        set => SetPixel(row, col, value);
    }

    /// <summary>
    /// Getter/setter of pixel value at specified coordinate.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel number, zero-based.</param>
    /// <returns>Pixel value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double this[uint row, uint col, uint channel]
    {
        get => GetPixel(row, col, channel);
        set => SetPixel(row, col, channel, value);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Managed byte array containing the image data.
    /// </summary>
    public byte[] ImageData { get; }

    /// <summary>
    /// Width of image (in pixels).
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Height of image (in pixels).
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Bit depth of stored image (in bits per pixel per channel).
    /// </summary>
    public uint BitDepth { get; }

    /// <summary>
    /// Minimum possible pixel value (DN).
    /// </summary>
    public uint PixelDynamicRangeMin { get; } = 0;

    /// <summary>
    /// Maximum possible pixel value (DN).
    /// </summary>
    public uint PixelDynamicRangeMax { get; }

    /// <summary>
    /// Pixel format of image (GenICam-standardized).
    /// </summary>
    public PixelFormat PixelFormat { get; }

    /// <summary>
    /// Frame ID of image.
    /// </summary>
    public long FrameID { get; }

    /// <summary>
    /// Timestamp of image (in PC ticks).
    /// </summary>
    public ulong TimeStamp { get; }

    /// <summary>
    /// Number of channels in image.
    /// </summary>
    public uint NumChannels { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new data container for the storage of an image buffer.
    /// </summary>
    /// <remarks>
    /// Note: Does not allocate new memory, as image data is shared with the input byte array.
    /// </remarks>
    /// <param name="imageData">Pre-allocated byte array containing the image data.</param>
    /// <param name="width">Width of image (in pixels).</param>
    /// <param name="height">Height of image (in pixels).</param>
    /// <param name="pixelFormat">Pixel format (GenICam-standardized).</param>
    /// <param name="pixelDynamicRangeMax">Maximum possible (saturation) pixel value.</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in DateTime ticks.</param>
    public GcBuffer(byte[] imageData, uint width, uint height, PixelFormat pixelFormat, uint pixelDynamicRangeMax, long frameID, ulong timeStamp)
    {
        // Validate array size.
        long requiredBits = (long)width * height * GenICamHelper.GetBitsPerPixel(pixelFormat); // Total bits required for the specified image dimensions and pixel format.
        long requiredBytes = (requiredBits + 7) / 8; // Round up to whole bytes (needed for packed formats).
        if (imageData.LongLength < requiredBytes)
            throw new ArgumentException($"Allocated image data ({imageData.LongLength} bytes) is too small to fit specified {nameof(width)} ({width}), {nameof(height)} ({height}) and {nameof(pixelFormat)} ({pixelFormat}), which requires {requiredBytes} bytes!");

        ImageData = imageData;
        Width = width;
        Height = height;
        NumChannels = GenICamHelper.GetNumChannels(pixelFormat);
        BitDepth = GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat);
        PixelFormat = pixelFormat;
        PixelDynamicRangeMax = pixelDynamicRangeMax;
        FrameID = frameID;
        TimeStamp = timeStamp;
    }

    /// <summary>
    /// Creates a new data container for the storage of an image buffer. The image data is copied from the provided EmguCV <see cref="Mat"/> object.
    /// </summary>
    /// <remarks>
    /// Note: Allocates new memory (to which image data is copied).
    /// </remarks>
    /// <param name="imageMat">Image in (EmguCV) Mat format.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="pixelDynamicRangeMax">Maximum possible (saturation) pixel value.</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in DateTime ticks.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcBuffer(Mat imageMat, PixelFormat pixelFormat, uint pixelDynamicRangeMax, long frameID, ulong timeStamp)
    {
        if (imageMat.IsEmpty)
            throw new ArgumentException($"Image data is empty!");

        // Validate that the specified pixel format is not packed, as packed pixel formats are not supported.
        if (GenICamHelper.IsPacked(pixelFormat))
            throw new ArgumentException($"Packed pixel formats are not supported!"); // ToDo: Implement support for packed pixel formats?

        // Validate that the number of channels in the Mat matches the expected number of channels for the specified pixel format.
        var expectedChannels = GenICamHelper.GetNumChannels(pixelFormat);
        if (imageMat.NumberOfChannels != expectedChannels)
            throw new ArgumentException($"Mat image is incompatible with requested pixel format: Number of channels in image is {imageMat.NumberOfChannels}, while pixel format uses {expectedChannels}!");

        // Validate that the bit depth of the Mat matches the expected bit depth for the specified pixel format.
        var expectedBitDepth = GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat);
        var actualBitDepth = EmguHelper.GetBitDepth(imageMat.Depth);
        if (actualBitDepth != expectedBitDepth)
            throw new ArgumentException($"Mat image is incompatible with requested pixel format: Mat storage depth is {actualBitDepth} bits, while pixel format requires {expectedBitDepth} bits.", nameof(imageMat));

        // Allocate new memory for image data.
        ImageData = new byte[imageMat.Total.ToInt32() * imageMat.ElementSize];

        // Copy image data.
        imageMat.CopyTo(ImageData);

        Width = (uint)imageMat.Width;
        Height = (uint)imageMat.Height;
        NumChannels = expectedChannels;
        BitDepth = expectedBitDepth;
        PixelFormat = pixelFormat;
        PixelDynamicRangeMax = pixelDynamicRangeMax;
        FrameID = frameID;
        TimeStamp = timeStamp;
    }

    /// <summary>
    /// Creates a new data container for the storage of an image buffer. The image data is copied from the provided EmguCV <see cref="Mat"/> object.
    /// Pixel format and bit depth are inferred from the <see cref="Mat"/> properties, with e.g. BGR color ordering for color images. 
    /// To preserve a specific pixel format (e.g. RGB8), please use the overload that accepts a <see cref="PixelFormat"/> parameter.
    /// </summary>
    /// <remarks>
    /// Note: Allocates new memory (to which image data is copied).
    /// </remarks>
    /// <param name="imageMat">Image in (EmguCV) Mat format.</param>
    /// <param name="pixelDynamicRangeMax">Maximum possible (saturation) pixel value.</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in DateTime ticks.</param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public GcBuffer(Mat imageMat, uint pixelDynamicRangeMax, long frameID, ulong timeStamp)
    {
        if (EmguHelper.GetPixelFormat(imageMat.Depth, imageMat.NumberOfChannels) == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Pixel format is not supported!");

        if (imageMat.IsEmpty)
            throw new ArgumentException($"Image data is empty!");

        // Allocate new memory for image data.
        ImageData = new byte[imageMat.Total.ToInt32() * imageMat.ElementSize];

        // Copy image data.
        imageMat.CopyTo(ImageData);

        Width = (uint)imageMat.Width;
        Height = (uint)imageMat.Height;
        NumChannels = (uint)imageMat.NumberOfChannels;
        BitDepth = (uint)EmguHelper.GetBitDepth(imageMat.Depth);
        PixelFormat = EmguHelper.GetPixelFormat(imageMat.Depth, imageMat.NumberOfChannels);
        PixelDynamicRangeMax = pixelDynamicRangeMax;
        FrameID = frameID;
        TimeStamp = timeStamp;
    }

    /// <summary>
    /// Copy constructor that creates a deep copy of an existing <see cref="GcBuffer"/> source. 
    /// </summary>
    /// <remarks>
    /// Note: Allocates new memory (to which image data is copied).
    /// </remarks>
    /// <param name="buffer">Source buffer.</param>
    public GcBuffer(GcBuffer buffer)
    {
        // Allocate new memory for image data. 
        ImageData = new byte[buffer.ImageData.Length];

        // Copy image data.
        Buffer.BlockCopy(buffer.ImageData, 0, ImageData, 0, buffer.ImageData.Length);

        Width = buffer.Width;
        Height = buffer.Height;
        NumChannels = buffer.NumChannels;
        BitDepth = buffer.BitDepth;
        PixelFormat = buffer.PixelFormat;
        PixelDynamicRangeMax = buffer.PixelDynamicRangeMax;
        PixelDynamicRangeMin = buffer.PixelDynamicRangeMin;
        FrameID = buffer.FrameID;
        TimeStamp = buffer.TimeStamp;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Converts image data in buffer to Emgu CV <see cref="Mat"/> format. 
    /// Conversion allocates new memory to store the image data.
    /// </summary>
    /// <remarks>
    /// Note: As Emgu CV assumes image data is stored in BGR(A) channel order, color conversion may be necessary (e.g. using <see cref="CvInvoke.CvtColor(Mat, Mat, Emgu.CV.CvEnum.ColorConversion)"/>).
    /// </remarks>
    /// <returns><see cref="Mat"/> image.</returns>
    public Mat ToMat()
    {
        var mat = new Mat(rows: (int)Height, cols: (int)Width, EmguHelper.GetDepthType((PixelSize)BitDepth), (int)NumChannels);
        mat.SetTo(ImageData);
        return mat;
    }

    /// <summary>
    /// Get pixel value(s) from specified image coordinate.
    /// </summary>
    /// <remarks>
    /// This method supports both packed and byte-aligned pixel formats.
    /// For packed formats (e.g., Mono1p, RGB10p), pixel data is extracted using bit-level operations.
    /// For byte-aligned formats, a more optimized byte-level access is used.
    /// </remarks>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <returns>Pixel value(s) from all image channels.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double[] GetPixel(uint row, uint col)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        double[] values = new double[NumChannels];

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            // Packed format: extract each channel separately using bit offsets
            for (int i = 0; i < NumChannels; i++)
            {
                long bitOffset = GetPixelBitOffset(row, col, (uint)i);
                var bits = ImageData.GetBitRange(start: (int)bitOffset, length: (int)BitDepth, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);
                values[i] = NumericHelper.SpanToDouble(new ReadOnlySpan<byte>(bits));
            }
        }
        else
        {
            // Byte-aligned format: use fast path
            var bytes = new ReadOnlySpan<byte>(array: ImageData,
                                               start: (int)((row * Width + col) * BitDepth / 8 * NumChannels),
                                               length: (int)BitDepth / 8 * (int)NumChannels);

            for (int i = 0; i < NumChannels; i++)
                values[i] = NumericHelper.SpanToDouble(bytes.Slice(start: i * (int)BitDepth / 8, length: (int)BitDepth / 8));
        }

        return values;
    }

    /// <summary>
    /// Get pixel value from specified image coordinate and image channel.
    /// </summary>
    /// <remarks>
    /// This method supports both packed and byte-aligned pixel formats.
    /// For packed formats (e.g., Mono1p, Mono10p), pixel data is extracted using bit-level operations.
    /// For byte-aligned formats, a more optimized byte-level access is used.
    /// </remarks>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <returns>Pixel value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double GetPixel(uint row, uint col, uint channel)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, NumChannels, nameof(channel));

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            // Packed format: use bit-level extraction
            long bitOffset = GetPixelBitOffset(row, col, channel);
            var bits = ImageData.GetBitRange(start: (int)bitOffset, length: (int)BitDepth, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

            // Convert extracted bits to double
            return NumericHelper.SpanToDouble(new ReadOnlySpan<byte>(bits));
        }
        else
        {
            // Byte-aligned format: use fast path
            var bytes = new ReadOnlySpan<byte>(array: ImageData,
                                               start: (int)((row * Width + col) * BitDepth / 8 * NumChannels + BitDepth / 8 * channel),
                                               length: (int)BitDepth / 8);

            return NumericHelper.SpanToDouble(bytes);
        }
    }

    /// <summary>
    /// Set pixel values for multi-channeled image.
    /// </summary>
    /// <remarks>
    /// This method supports both packed and byte-aligned pixel formats.
    /// For packed formats (e.g., Mono1p, RGB10p), pixel data is inserted using bit-level operations.
    /// For byte-aligned formats, a more optimized byte-level access is used.
    /// </remarks>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="pixelValues">Pixel values.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetPixel(uint row, uint col, double[] pixelValues)
    {
        ArgumentNullException.ThrowIfNull(pixelValues);

        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that exactly one value is provided per channel.
        if (pixelValues.Length != NumChannels)
            throw new ArgumentOutOfRangeException(nameof(pixelValues), pixelValues.Length, $"Expected {NumChannels} values (one per channel).");

        // Check that pixel values fall within dynamic range.
        foreach (var value in pixelValues)
        {
            if (!(value >= PixelDynamicRangeMin && value <= PixelDynamicRangeMax)) // Negated comparison also rejects NaN.
                throw new ArgumentOutOfRangeException(nameof(pixelValues), value, $"Pixel values must be within [{PixelDynamicRangeMin}, {PixelDynamicRangeMax}].");
        }

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            Span<byte> bytes = stackalloc byte[(int)((BitDepth + 7) / 8)]; // Temporary buffer for a single channel's pixel value

            // Packed format: set each channel separately using bit offsets
            for (int i = 0; i < NumChannels; i++)
            {
                NumericHelper.DoubleToSpan(ref bytes, pixelValues[i]);
                long bitOffset = GetPixelBitOffset(row, col, (uint)i);
                ImageData.SetBitRange(start: (int)bitOffset, length: (int)BitDepth, value: bytes, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);
            }
        }
        else
        {
            // Byte-aligned format: use fast path
            Span<byte> bytes = stackalloc byte[(int)(BitDepth / 8 * NumChannels)];
            NumericHelper.DoubleToSpan(ref bytes, pixelValues);

            long offset = ((long)row * Width + col) * (BitDepth / 8) * NumChannels;
            for (int i = 0; i < bytes.Length; i++)
                ImageData[offset + i] = bytes[i];
        }
    }

    /// <summary>
    /// Set pixel value for a specific channel.
    /// </summary>
    /// <remarks>
    /// This method supports both packed and byte-aligned pixel formats.
    /// For packed formats (e.g., Mono1p, Mono10p), pixel data is inserted using bit-level operations.
    /// For byte-aligned formats, a more optimized byte-level access is used.
    /// </remarks>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <param name="pixelValue">Pixel value.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetPixel(uint row, uint col, uint channel, double pixelValue)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, NumChannels, nameof(channel));

        // Check that pixel value falls within dynamic range.
        if (!(pixelValue >= PixelDynamicRangeMin && pixelValue <= PixelDynamicRangeMax)) // Negated comparison also rejects NaN.
            throw new ArgumentOutOfRangeException(nameof(pixelValue), pixelValue, $"Pixel value must be within [{PixelDynamicRangeMin}, {PixelDynamicRangeMax}].");

        // Allocate span of bytes large enough to hold a single-channel pixel of the buffer.   
        int byteCount = GenICamHelper.IsPacked(PixelFormat) ? (int)((BitDepth + 7) / 8) : (int)(BitDepth / 8); // For packed formats, we need (BitDepth + 7) / 8 bytes; for byte-aligned, BitDepth / 8.
        Span<byte> bytes = stackalloc byte[byteCount];

        // Convert pixel value to integral numeric type and store it in the span.
        NumericHelper.DoubleToSpan(ref bytes, pixelValue);

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            // Packed format: use bit-level insertion
            long bitOffset = GetPixelBitOffset(row, col, channel);
            ImageData.SetBitRange(start: (int)bitOffset, length: (int)BitDepth, value: bytes, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);
        }
        else
        {
            // Byte-aligned format: use fast path
            long offset = ((long)row * Width + col) * (BitDepth / 8) * NumChannels + (long)channel * (BitDepth / 8);
            for (int i = 0; i < bytes.Length; i++)
                ImageData[offset + i] = bytes[i];
        }
    }

    /// <summary>
    /// Retrieve size of image.
    /// </summary>
    /// <returns>Image size.</returns>
    public Size GetSize()
    {
        return new Size((int)Width, (int)Height);
    }

    #endregion

    #region Object overrides

    public override bool Equals(object obj)
    {
        if ((obj is GcBuffer otherBuffer) == false)
            return false; // Not the same type.

        if (otherBuffer.Width != Width || otherBuffer.Height != Height || otherBuffer.PixelFormat != PixelFormat || otherBuffer.FrameID != FrameID || otherBuffer.TimeStamp != TimeStamp)
            return false; // Metadata are not equal.

        if (ImageData.AsSpan().SequenceEqual(otherBuffer.ImageData) == false)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var imageHash = new HashCode();

        // Add the hash code of the image data to the hash code of the metadata.
        imageHash.AddBytes(ImageData.AsSpan());

        // Combine the hash codes of the metadata and the image data.
        return HashCode.Combine(
            Width,
            Height,
            PixelFormat,
            FrameID,
            TimeStamp,
            imageHash.ToHashCode());
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Calculates the bit offset (in bits, not bytes) for a pixel at the specified coordinate and channel.
    /// </summary>
    /// <remarks>
    /// This helper method works for both packed and byte-aligned formats.
    /// For packed formats, the offset may not be byte-aligned.
    /// </remarks>
    /// <param name="row">Row index.</param>
    /// <param name="col">Column index.</param>
    /// <param name="channel">Channel index (default 0).</param>
    /// <returns>Bit offset from the start of ImageData array.</returns>
    private long GetPixelBitOffset(uint row, uint col, uint channel = 0)
    {
        long bitDepth = BitDepth;
        long numChannels = NumChannels;
        long pixelIndex = (long)row * Width + col;

        // Total bits per pixel (across all channels)
        long bitsPerPixel = bitDepth * numChannels;

        // Bit offset to the start of the pixel
        long pixelBitOffset = pixelIndex * bitsPerPixel;

        // Add channel offset
        long channelBitOffset = channel * bitDepth;

        return pixelBitOffset + channelBitOffset;
    }

    #endregion

    #region Obsolete members

    /// <summary>
    /// Dynamically creates an array to hold a single element (pixel) of the buffer. The array type will be determined by the bit depth and the array size by the number of image channels.
    /// </summary>
    /// <returns>Array fit to hold a single buffer element (pixel).</returns>
    [Obsolete]
    private dynamic CreateElement()
    {
        if (BitDepth <= 8)
            return new byte[NumChannels];
        if (BitDepth <= 16)
            return new ushort[NumChannels];
        if (BitDepth <= 32)
            return new uint[NumChannels];
        return new ulong[NumChannels];
    }

    /// <summary>
    /// Converts double value to an integer type according to buffer bit depth.
    /// </summary>
    /// <param name="value">Double value.</param>
    /// <returns>Value represented in appropriate integer type.</returns>
    /// <exception cref="OverflowException"></exception>
    [Obsolete]
    private dynamic ConvertType(double value)
    {
        if (BitDepth <= 8)
            return Convert.ToByte(value);
        if (BitDepth <= 16)
            return Convert.ToUInt16(value);
        if (BitDepth <= 32)
            return Convert.ToUInt32(value);
        return Convert.ToUInt64(value);
    }

    ///<summary>
    /// Get pixel value(s) from specified image coordinate.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <returns>Pixel value(s) from all image channels (or null if coordinate is out-of-bounds).</returns>
    /// <remarks>Note: This method is obsolete. Please use <see cref="GetPixel(int, int)"/> instead (it is about 5x faster).</remarks>
    [Obsolete("Older inefficient version of GetPixel method")]
    public double[] GetPixelUsingBlockCopy(uint row, uint col)
    {
        // Check boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Create storage for a single element (pixel), where type and length will be determined at runtime.
        dynamic element = CreateElement();

        // Copy pixel bytes from buffer.
        Buffer.BlockCopy(src: ImageData,
                         srcOffset: (int)((row * Width + col) * BitDepth / 8 * NumChannels),
                         dst: element,
                         dstOffset: 0,
                         count: (int)(BitDepth / 8 * NumChannels));

        // Array to hold pixel values for all image channels.
        double[] values = new double[NumChannels];

        // Extract channel values.
        for (int i = 0; i < NumChannels; i++)
            values[i] = element[i];

        return values;
    }

    /// <summary>
    /// Get pixel value for a specific image channel.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <returns>Pixel value (or null if coordinate is out-of-bounds).</returns>
    /// <remarks>Note: This method is obsolete. Please use <see cref="GetPixel(int, int, int)"/> instead (it is about 5x faster).</remarks>
    [Obsolete("Older inefficient version of GetPixel method")]
    public double? GetPixelUsingBlockCopy(uint row, uint col, uint channel)
    {
        // Check boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, NumChannels, nameof(channel));

        // Array to hold pixel value.
        dynamic element = CreateElement();

        // Copy pixel bytes from buffer.
        Buffer.BlockCopy(src: ImageData,
                         srcOffset: (int)((row * Width + col) * BitDepth / 8 * NumChannels),
                         dst: element,
                         dstOffset: 0,
                         count: (int)(BitDepth / 8 * NumChannels));

        return element[channel];
    }

    /// <summary>
    /// Set pixel values for multi-channeled image.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="pixelValues">Pixel values.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Obsolete("Older inefficient version of SetPixel method")]
    public void SetPixelUsingBlockCopy(uint row, uint col, double[] pixelValues)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that pixel value has valid number of elements.
        if (pixelValues.Length > NumChannels)
            throw new ArgumentOutOfRangeException(nameof(pixelValues));

        // Create storage for a single element (pixel), where type and length will be determined at runtime.
        dynamic element = CreateElement();

        // Copy pixel values to element.
        for (int i = 0; i < NumChannels; i++)
        {
            if (pixelValues[i] >= 0 && pixelValues[i] <= PixelDynamicRangeMax)
                element[i] = ConvertType(pixelValues[i]);
            else throw new ArgumentOutOfRangeException(nameof(pixelValues));
        }

        // Copy element to buffer.
        Buffer.BlockCopy(src: element,
                         srcOffset: 0,
                         dst: ImageData,
                         dstOffset: (int)((row * (int)Width + col) * (int)BitDepth / 8 * (int)NumChannels),
                         count: (int)BitDepth / 8 * (int)NumChannels);
    }

    /// <summary>
    /// Set pixel value for a specific channel.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <param name="pixelValue">Pixel value.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Obsolete("Older inefficient version of SetPixel method")]
    public void SetPixelUsingBlockCopy(uint row, uint col, uint channel, double pixelValue)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, NumChannels, nameof(channel));

        // Check that pixel value falls within dynamic range.
        if (pixelValue < 0 || pixelValue > PixelDynamicRangeMax)
            throw new ArgumentOutOfRangeException(nameof(pixelValue));

        // Array to hold pixel value.
        dynamic element = CreateElement();

        // Convert pixel value to correct type.
        element[channel] = ConvertType(pixelValue);

        // Copy pixel bytes to buffer.
        Buffer.BlockCopy(src: element,
                         srcOffset: (int)(channel * (int)BitDepth / 8),
                         dst: ImageData,
                         dstOffset: (int)((row * (int)Width + col) * (int)BitDepth / 8 * (int)NumChannels + channel * BitDepth / 8),
                         count: (int)BitDepth / 8);
    }

    #endregion
}