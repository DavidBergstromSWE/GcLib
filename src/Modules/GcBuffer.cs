using System;
using System.Drawing;
using Emgu.CV;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;

namespace GcLib;

/// <summary>
/// Represents the buffer level in the GenICam/GenTL standard module hierarchy and is responsible for the storage and format conversion of an image buffer. Image data is stored as a managed byte array, with metadata such as frame ID, timestamp, width, height and pixel format stored as properties. 
/// </summary>
public sealed class GcBuffer : IEquatable<GcBuffer>
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

    #region Properties (Image Data)

    /// <summary>
    /// Managed byte array containing the image data.
    /// </summary>
    public byte[] ImageData { get; }

    #endregion

    #region Properties (Metadata)

    /// <summary>
    /// Width of image (in pixels).
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Height of image (in pixels).
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Pixel format of image (GenICam PFNC).
    /// </summary>
    public PixelFormat PixelFormat { get; }

    /// <summary>
    /// Frame ID of image.
    /// </summary>
    public long FrameID { get; }

    /// <summary>
    /// Timestamp of image, in <see cref="DateTime"/> ticks.
    /// </summary>
    public ulong TimeStamp { get; }

    /// <summary>
    /// Number of channels in the image.
    /// </summary>
    public uint NumChannels => GenICamHelper.GetNumChannels(PixelFormat);

    /// <summary>
    /// Maximum pixel value (e.g., 255 for 8-bit, 1023 for 10-bit, etc.).
    /// </summary>
    public uint PixelDynamicRangeMax => GenICamHelper.GetPixelDynamicRangeMax(PixelFormat);

    /// <summary>
    /// Horizontal/row padding added to the end of each image row (in bytes).
    /// </summary>
    /// <remarks>For planar formats this padding follows each row of each channel plane.</remarks>
    public uint PaddingX { get; }

    /// <summary>
    /// Vertical/frame padding (in bytes) added to the end of the image frame.
    /// </summary>
    public uint PaddingY { get; }

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
    /// <param name="pixelFormat">Pixel format (GenICam PFNC).</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in <see cref="DateTime"/> ticks.</param>
    /// <param name="paddingX">Bytes of padding after each row.</param>
    /// <param name="paddingY">Bytes of padding after the frame.</param>
    /// <remarks>Packed images without row padding retain the legacy continuous bit layout. With row padding, each row starts on a byte boundary.</remarks>
    /// <exception cref="ArgumentException">Thrown when the provided byte array is too small to fit the specified dimensions and pixel format.</exception>
    public GcBuffer(byte[] imageData, uint width, uint height, PixelFormat pixelFormat, long frameID, ulong timeStamp, uint paddingX = 0, uint paddingY = 0)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        long requiredBytes;
        checked
        {
            long bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
            if (paddingX == 0)
                requiredBytes = ((long)width * height * bitsPerPixel + 7) / 8 + paddingY;
            else if (GenICamHelper.IsPlanar(pixelFormat))
                requiredBytes = (((long)width * GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) + 7) / 8 + paddingX) * height * GenICamHelper.GetNumChannels(pixelFormat) + paddingY; // For planar formats, each channel plane has its own row padding, and the total size is the sum of all channel planes plus any frame padding.
            else
                requiredBytes = ((width * bitsPerPixel + 7) / 8 + paddingX) * height + paddingY; // For non-planar formats, each row has its own row padding, and the total size is the sum of all rows plus any frame padding.
        }
        if (imageData.LongLength < requiredBytes)
            throw new ArgumentException($"Allocated image data ({imageData.LongLength} bytes) is too small to fit the specified dimensions and pixel format, which requires {requiredBytes} bytes!", nameof(imageData));

        ImageData = imageData;
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        FrameID = frameID;
        TimeStamp = timeStamp;
        PaddingX = paddingX;
        PaddingY = paddingY;
    }

    /// <summary>
    /// Creates a new data container for the storage of an image buffer. The image data is copied from the provided <see cref="Mat"/>, where data is assumed to be stored in a pixel format specified in <paramref name="pixelFormat"/>.
    /// </summary>
    /// <remarks>
    /// Note: Allocates new memory (to which image data is copied).
    /// </remarks>
    /// <param name="imageMat">Image in <see cref="Mat"/> format.</param>
    /// <param name="pixelFormat">Pixel format (GenICam PFNC) used in <paramref name="imageMat"/>.</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in <see cref="DateTime"/> ticks.</param>
    /// <exception cref="ArgumentException">Thrown if the image data is empty or if the image is incompatible with the specified pixel format.</exception>
    public GcBuffer(Mat imageMat, PixelFormat pixelFormat, long frameID, ulong timeStamp)
        : this(imageMat, pixelFormat, frameID, timeStamp, outputBuffer: null)
    {
    }

    /// <summary>
    /// Creates a new data container for the storage of an image buffer. The image data is copied from the provided <see cref="Mat"/>, where data is assumed to be stored in a pixel format specified in <paramref name="pixelFormat"/>.
    /// </summary>
    /// <remarks>
    /// Uses provided <paramref name="outputBuffer"/> when it is large enough, otherwise allocates a new array.
    /// </remarks>
    /// <param name="imageMat">Image in <see cref="Mat"/> format.</param>
    /// <param name="pixelFormat">Pixel format (GenICam PFNC) used in <paramref name="imageMat"/>.</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in <see cref="DateTime"/> ticks.</param>
    /// <param name="outputBuffer">Optional pre-allocated byte array for image copy destination.</param>
    /// <exception cref="ArgumentException">Thrown if the image data is empty or if the image is incompatible with the specified pixel format.</exception>
    public GcBuffer(Mat imageMat, PixelFormat pixelFormat, long frameID, ulong timeStamp, byte[] outputBuffer)
    {
        int requiredBytes = ValidateMatAndGetRequiredBytes(imageMat, pixelFormat);

        // Allocate new memory for image data when no reusable output buffer is provided.
        ImageData = outputBuffer != null && outputBuffer.Length >= requiredBytes ? outputBuffer : new byte[requiredBytes];

        // Mat channels are interleaved, including when a planar output format is requested.
        if (GenICamHelper.IsPlanar(pixelFormat))
        {
            var interleaved = new byte[requiredBytes];
            imageMat.CopyTo(interleaved);
            int channels = imageMat.NumberOfChannels;
            int bytesPerChannel = imageMat.ElementSize / channels;
            int pixels = imageMat.Total.ToInt32();
            int planeSize = pixels * bytesPerChannel;
            for (int pixel = 0; pixel < pixels; pixel++)
                for (int channel = 0; channel < channels; channel++)
                    interleaved.AsSpan((pixel * channels + channel) * bytesPerChannel, bytesPerChannel)
                               .CopyTo(ImageData.AsSpan(channel * planeSize + pixel * bytesPerChannel, bytesPerChannel));
        }
        else
        {
            imageMat.CopyTo(ImageData);
        }

        Width = (uint)imageMat.Width;
        Height = (uint)imageMat.Height;
        PixelFormat = pixelFormat;
        FrameID = frameID;
        TimeStamp = timeStamp;
    }

    /// <summary>
    /// Creates a new data container for the storage of an image buffer. The image data is copied from the provided <see cref="Mat"/>, 
    /// where the pixel format and bit depth is inferred from the <see cref="Mat"/> properties. It is assumed that color ordering is BGR for color images, as this is the default in EmguCV. 
    /// To preserve a specific pixel format (e.g. RGB8), use overload <see cref="GcBuffer(Mat, PixelFormat, long, ulong)"/>.
    /// </summary>
    /// <remarks>
    /// Note: Allocates new memory (to which image data is copied).
    /// </remarks>
    /// <param name="imageMat">Image in <see cref="Mat"/> format.</param>
    /// <param name="frameID">Frame ID.</param>
    /// <param name="timeStamp">Image timestamp in <see cref="DateTime"/> ticks.</param>
    /// <exception cref="NotSupportedException">Thrown if the pixel format of the image is not supported.</exception>
    /// <exception cref="ArgumentException">Thrown if the image data is empty.</exception>
    public GcBuffer(Mat imageMat, long frameID, ulong timeStamp)
    {
        if (imageMat.IsEmpty)
            throw new ArgumentException($"Image data is empty!");

        var pixelFormat = EmguHelper.GetPixelFormat(imageMat.Depth, imageMat.NumberOfChannels);
        if (pixelFormat == PixelFormat.InvalidPixelFormat)
            throw new NotSupportedException($"Pixel format {pixelFormat} is not supported!");

        int requiredBytes = ValidateMatAndGetRequiredBytes(imageMat, pixelFormat);

        // Allocate new memory for image data.
        ImageData = new byte[requiredBytes];

        // Copy image data.
        imageMat.CopyTo(ImageData);

        Width = (uint)imageMat.Width;
        Height = (uint)imageMat.Height;
        PixelFormat = pixelFormat;
        FrameID = frameID;
        TimeStamp = timeStamp;
    }

    /// <summary>
    /// Validates that the image Mat is compatible with the specified pixel format and returns the required buffer size
    /// in bytes.
    /// </summary>
    /// <param name="imageMat">Image data to validate.</param>
    /// <param name="pixelFormat">Pixel format to validate against the image data.</param>
    /// <returns>The total number of bytes required to store the image data.</returns>
    /// <exception cref="ArgumentException">Thrown when image data is empty, when the pixel format is packed, when the channel count does not match the
    /// pixel format, or when the Mat bit depth does not match the pixel format.</exception>
    private static int ValidateMatAndGetRequiredBytes(Mat imageMat, PixelFormat pixelFormat)
    {
        // ToDo: Split validation into separate methods for each check, to provide more specific error messages?

        if (imageMat.IsEmpty)
            throw new ArgumentException($"Image data is empty!", nameof(imageMat));

        // Validate that the specified pixel format is not packed, as packed pixel formats are not supported.
        if (GenICamHelper.IsPacked(pixelFormat))
            throw new ArgumentException($"Packed pixel formats are not supported!", nameof(pixelFormat));

        // Validate that the number of channels in the Mat matches the expected number of channels for the specified pixel format.
        var expectedChannels = GenICamHelper.GetNumChannels(pixelFormat);
        if (imageMat.NumberOfChannels != expectedChannels)
            throw new ArgumentException($"Mat image is incompatible with the specified pixel format: Number of channels in image is {imageMat.NumberOfChannels}, while pixel format uses {expectedChannels}!");

        // Validate that the bit depth of the Mat matches the expected bit depth for the specified pixel format.
        var expectedBitDepth = GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat);
        var actualBitDepth = EmguHelper.GetBitDepth(imageMat.Depth);
        if (actualBitDepth != expectedBitDepth)
            throw new ArgumentException($"Mat image is incompatible with the specified pixel format: Mat storage depth is {actualBitDepth} bits, while pixel format requires {expectedBitDepth} bits.");

        return imageMat.Total.ToInt32() * imageMat.ElementSize;
    }

    /// <summary>
    /// Copy constructor that creates a deep copy of an existing <see cref="GcBuffer"/>. 
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
        PixelFormat = buffer.PixelFormat;
        FrameID = buffer.FrameID;
        TimeStamp = buffer.TimeStamp;
        PaddingX = buffer.PaddingX;
        PaddingY = buffer.PaddingY;
    }

    #endregion

    #region Public methods

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

        double[] values = new double[GenICamHelper.GetNumChannels(PixelFormat)];

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            int bitCount = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
            Span<byte> bits = stackalloc byte[(bitCount + 7) / ByteExtensions.BitsPerByte];

            // Packed format: extract each channel separately using bit offsets
            for (int i = 0; i < values.Length; i++)
            {
                long bitOffset = GetPixelBitOffset(row, col, (uint)i);
                GetBitSpan(ImageData, bitOffset, bitCount).GetBitRange((int)(bitOffset % 8), bitCount, bits);
                values[i] = NumericHelper.SpanToDouble(bits);
            }
        }
        else
        {
            int bytesPerChannel = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8;
            for (int i = 0; i < values.Length; i++)
                values[i] = NumericHelper.SpanToDouble(ImageData.AsSpan(checked((int)(GetPixelBitOffset(row, col, (uint)i) / 8)), bytesPerChannel));
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
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, GenICamHelper.GetNumChannels(PixelFormat), nameof(channel));

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            // Packed format: use bit-level extraction
            long bitOffset = GetPixelBitOffset(row, col, channel);
            int bitCount = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
            Span<byte> bits = stackalloc byte[(bitCount + 7) / ByteExtensions.BitsPerByte];
            GetBitSpan(ImageData, bitOffset, bitCount).GetBitRange((int)(bitOffset % 8), bitCount, bits);

            // Convert extracted bits to double
            return NumericHelper.SpanToDouble(bits);
        }
        else
        {
            int bytesPerChannel = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8;
            return NumericHelper.SpanToDouble(ImageData.AsSpan(checked((int)(GetPixelBitOffset(row, col, channel) / 8)), bytesPerChannel));
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
        if (pixelValues.Length != GenICamHelper.GetNumChannels(PixelFormat))
            throw new ArgumentOutOfRangeException(nameof(pixelValues), pixelValues.Length, $"Expected {GenICamHelper.GetNumChannels(PixelFormat)} values (one per channel).");

        // Check that pixel values fall within dynamic range.
        foreach (var value in pixelValues)
        {
            if (!(value >= 0 && value <= GenICamHelper.GetPixelDynamicRangeMax(PixelFormat))) // Negated comparison also rejects NaN.
                throw new ArgumentOutOfRangeException(nameof(pixelValues), value, $"Pixel values must be within [0, {GenICamHelper.GetPixelDynamicRangeMax(PixelFormat)}].");
        }

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            Span<byte> bytes = stackalloc byte[(int)((GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) + 7) / 8)]; // Temporary buffer for a single channel's pixel value

            // Packed format: set each channel separately using bit offsets
            for (int i = 0; i < GenICamHelper.GetNumChannels(PixelFormat); i++)
            {
                NumericHelper.DoubleToSpan(ref bytes, pixelValues[i]);
                long bitOffset = GetPixelBitOffset(row, col, (uint)i);
                int bitCount = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
                GetBitSpan(ImageData, bitOffset, bitCount).SetBitRange((int)(bitOffset % 8), bitCount, bytes);
            }
        }
        else
        {
            int bytesPerChannel = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8;
            Span<byte> bytes = stackalloc byte[bytesPerChannel];
            for (int i = 0; i < pixelValues.Length; i++)
            {
                NumericHelper.DoubleToSpan(ref bytes, pixelValues[i]);
                bytes.CopyTo(ImageData.AsSpan(checked((int)(GetPixelBitOffset(row, col, (uint)i) / 8)), bytesPerChannel));
            }
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
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, GenICamHelper.GetNumChannels(PixelFormat), nameof(channel));

        // Check that pixel value falls within dynamic range.
        if (!(pixelValue >= 0 && pixelValue <= GenICamHelper.GetPixelDynamicRangeMax(PixelFormat))) // Negated comparison also rejects NaN.
            throw new ArgumentOutOfRangeException(nameof(pixelValue), pixelValue, $"Pixel value must be within [0, {GenICamHelper.GetPixelDynamicRangeMax(PixelFormat)}].");

        // Allocate span of bytes large enough to hold a single-channel pixel of the buffer.   
        int byteCount = GenICamHelper.IsPacked(PixelFormat) ? (int)((GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) + 7) / 8) : (int)(GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8); // For packed formats, we need (BitDepth + 7) / 8 bytes; for byte-aligned, BitDepth / 8.
        Span<byte> bytes = stackalloc byte[byteCount];

        // Convert pixel value to integral numeric type and store it in the span.
        NumericHelper.DoubleToSpan(ref bytes, pixelValue);

        if (GenICamHelper.IsPacked(PixelFormat))
        {
            // Packed format: use bit-level insertion
            long bitOffset = GetPixelBitOffset(row, col, channel);
            int bitCount = (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
            GetBitSpan(ImageData, bitOffset, bitCount).SetBitRange((int)(bitOffset % 8), bitCount, bytes);
        }
        else
        {
            int offset = checked((int)(GetPixelBitOffset(row, col, channel) / 8));
            bytes.CopyTo(ImageData.AsSpan(offset, bytes.Length));
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

    /// <summary>
    /// Gets the row stride in bytes, including horizontal padding.
    /// </summary>
    /// <returns>The number of bytes in a row (per plane for planar formats).</returns>
    /// <exception cref="InvalidOperationException">A continuously packed row is not byte-aligned.</exception>
    public int GetStride()
    {
        long rowBits = (long)Width * (GenICamHelper.IsPlanar(PixelFormat) ? GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) : GenICamHelper.GetBitsPerPixel(PixelFormat));
        if (PaddingX == 0 && rowBits % 8 != 0)
            throw new InvalidOperationException("Continuously packed rows do not have a byte-aligned stride.");
        return checked((int)((rowBits + 7) / 8 + PaddingX));
    }

    #endregion

    #region Equality and Hashing

    /// <summary>
    /// Determines whether this buffer has metadata equality with another buffer.
    /// </summary>
    /// <remarks>Metadata equality compares width, height, pixel format, frame ID, timestamp and padding. Image payload (<see cref="ImageData"/>) is intentionally excluded.</remarks>
    /// <param name="other">Other buffer to compare with.</param>
    /// <returns><see langword="true"/> if metadata are equal; otherwise <see langword="false"/>.</returns>
    public bool Equals(GcBuffer other)
    {
        if (other is null)
            return false;

        return other.Width == Width
            && other.Height == Height
            && other.PixelFormat == PixelFormat
            && other.FrameID == FrameID
            && other.TimeStamp == TimeStamp
            && other.PaddingX == PaddingX
            && other.PaddingY == PaddingY;
    }

    /// <summary>
    /// Determines whether this buffer has payload/content equality with another buffer.
    /// </summary>
    /// <remarks>Content equality requires both metadata equality and byte-wise equality of <see cref="ImageData"/>.</remarks>
    /// <param name="other">Other buffer to compare with.</param>
    /// <returns><see langword="true"/> if metadata and image payload are equal; otherwise <see langword="false"/>.</returns>
    public bool ContentEquals(GcBuffer other)
    {
        return Equals(other) && ImageData.AsSpan().SequenceEqual(other.ImageData);
    }

    public override bool Equals(object obj)
    {
        return obj is GcBuffer otherBuffer && Equals(otherBuffer);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Width,
            Height,
            PixelFormat,
            FrameID,
            TimeStamp,
            PaddingX,
            PaddingY);
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
        long channelBits = GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
        if (GenICamHelper.IsPlanar(PixelFormat))
        {
            long rowBits = PaddingX == 0 ? Width * channelBits : (long)GetStride() * 8;
            return ((long)channel * Height + row) * rowBits + col * channelBits;
        }

        long pixelBits = GenICamHelper.GetBitsPerPixel(PixelFormat);
        long rowOffset = PaddingX == 0 ? (long)row * Width * pixelBits : (long)row * GetStride() * 8;
        return rowOffset + col * pixelBits + channel * channelBits;
    }

    private static Span<byte> GetBitSpan(byte[] data, long bitOffset, int bitCount)
    {
        int byteOffset = checked((int)(bitOffset / 8));
        int byteCount = (int)((bitOffset % 8 + bitCount + 7) / 8);
        return data.AsSpan(byteOffset, byteCount);
    }

    /// <summary>
    /// Gets the image data without row padding.
    /// </summary>
    /// <returns>A byte array containing the tightly packed image data.</returns>
    internal byte[] GetTightImageData()
    {
        // Calculate the expected length of the tightly packed image data based on width, height, and bits per pixel.
        int length = checked((int)(((long)Width * Height * GenICamHelper.GetBitsPerPixel(PixelFormat) + 7) / 8));
        if (PaddingX == 0 && ImageData.Length == length)
            return ImageData; // Already tightly packed, no need to copy.

        // Create a new byte array to hold the tightly packed image data.
        var result = new byte[length];

        // If there is no row padding, we can directly copy the image data to the result array.
        if (PaddingX == 0)
        {
            Buffer.BlockCopy(ImageData, 0, result, 0, length);
            return result;
        }

        // Handle the case where there is row padding. We need to copy each row of the image data to the result array, skipping the padding bytes.
        bool planar = GenICamHelper.IsPlanar(PixelFormat);
        int bits = checked((int)(planar ? GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) : GenICamHelper.GetBitsPerPixel(PixelFormat)));
        int planes = planar ? checked((int)NumChannels) : 1;
        long rowBits = Width * bits;

        // If the row is byte-aligned, we can use Buffer.BlockCopy for efficiency. Otherwise, we need to copy bit by bit.
        if (rowBits % 8 == 0)
        {
            int rowBytes = checked((int)(rowBits / 8));
            int stride = GetStride();
            for (int plane = 0; plane < planes; plane++)
                for (uint row = 0; row < Height; row++)
                    Buffer.BlockCopy(ImageData, checked((int)(((long)plane * Height + row) * stride)), result, checked((int)(((long)plane * Height + row) * rowBytes)), rowBytes);
        }
        else // Bit-level copy for non-byte-aligned rows
        {
            Span<byte> sample = stackalloc byte[(bits + 7) / 8];
            for (int plane = 0; plane < planes; plane++)
                for (uint row = 0; row < Height; row++)
                    for (uint col = 0; col < Width; col++)
                    {
                        long sourceBit = GetPixelBitOffset(row, col, (uint)plane);
                        long targetBit = (((long)plane * Height + row) * Width + col) * bits;
                        GetBitSpan(ImageData, sourceBit, bits).GetBitRange((int)(sourceBit % 8), bits, sample);
                        GetBitSpan(result, targetBit, bits).SetBitRange((int)(targetBit % 8), bits, sample);
                    }
        }
        return result;
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
        long bitDepth = GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
        if (bitDepth <= 8)
            return new byte[GenICamHelper.GetNumChannels(PixelFormat)];
        if (bitDepth <= 16)
            return new ushort[GenICamHelper.GetNumChannels(PixelFormat)];
        if (bitDepth <= 32)
            return new uint[GenICamHelper.GetNumChannels(PixelFormat)];
        return new ulong[GenICamHelper.GetNumChannels(PixelFormat)];
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
        long bitDepth = GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
        if (bitDepth <= 8)
            return Convert.ToByte(value);
        if (bitDepth <= 16)
            return Convert.ToUInt16(value);
        if (bitDepth <= 32)
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
        if (PaddingX != 0)
            return GetPixel(row, col);

        // Check boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Create storage for a single element (pixel), where type and length will be determined at runtime.
        dynamic element = CreateElement();

        // Copy pixel bytes from buffer.
        long bitDepth = GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat);
        Buffer.BlockCopy(src: ImageData,
                         srcOffset: (int)((row * Width + col) * bitDepth / 8 * GenICamHelper.GetNumChannels(PixelFormat)),
                         dst: element,
                         dstOffset: 0,
                         count: (int)(bitDepth / 8 * GenICamHelper.GetNumChannels(PixelFormat)));

        // Array to hold pixel values for all image channels.
        double[] values = new double[GenICamHelper.GetNumChannels(PixelFormat)];

        // Extract channel values.
        for (int i = 0; i < GenICamHelper.GetNumChannels(PixelFormat); i++)
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
        if (PaddingX != 0)
            return GetPixel(row, col, channel);

        // Check boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, GenICamHelper.GetNumChannels(PixelFormat), nameof(channel));

        // Array to hold pixel value.
        dynamic element = CreateElement();

        // Copy pixel bytes from buffer.
        Buffer.BlockCopy(src: ImageData,
                         srcOffset: (int)((row * Width + col) * GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8 * GenICamHelper.GetNumChannels(PixelFormat)),
                         dst: element,
                         dstOffset: 0,
                         count: (int)(GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8 * GenICamHelper.GetNumChannels(PixelFormat)));

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
        if (PaddingX != 0)
        {
            SetPixel(row, col, pixelValues);
            return;
        }

        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that pixel value has valid number of elements.
        if (pixelValues.Length > GenICamHelper.GetNumChannels(PixelFormat))
            throw new ArgumentOutOfRangeException(nameof(pixelValues));

        // Create storage for a single element (pixel), where type and length will be determined at runtime.
        dynamic element = CreateElement();

        // Copy pixel values to element.
        for (int i = 0; i < GenICamHelper.GetNumChannels(PixelFormat); i++)
        {
            if (pixelValues[i] >= 0 && pixelValues[i] <= GenICamHelper.GetPixelDynamicRangeMax(PixelFormat))
                element[i] = ConvertType(pixelValues[i]);
            else throw new ArgumentOutOfRangeException(nameof(pixelValues));
        }

        // Copy element to buffer.
        Buffer.BlockCopy(src: element,
                         srcOffset: 0,
                         dst: ImageData,
                         dstOffset: (int)((row * (int)Width + col) * (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8 * (int)GenICamHelper.GetNumChannels(PixelFormat)),
                         count: (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8 * (int)GenICamHelper.GetNumChannels(PixelFormat));
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
        if (PaddingX != 0)
        {
            SetPixel(row, col, channel, pixelValue);
            return;
        }

        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, GenICamHelper.GetNumChannels(PixelFormat), nameof(channel));

        // Check that pixel value falls within dynamic range.
        if (pixelValue < 0 || pixelValue > GenICamHelper.GetPixelDynamicRangeMax(PixelFormat))
            throw new ArgumentOutOfRangeException(nameof(pixelValue));

        // Array to hold pixel value.
        dynamic element = CreateElement();

        // Convert pixel value to correct type.
        element[channel] = ConvertType(pixelValue);

        // Copy pixel bytes to buffer.
        Buffer.BlockCopy(src: element,
                         srcOffset: (int)(channel * (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8),
                         dst: ImageData,
                         dstOffset: (int)((row * (int)Width + col) * (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8 * (int)GenICamHelper.GetNumChannels(PixelFormat) + channel * GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8),
                         count: (int)GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat) / 8);
    }

    #endregion
}