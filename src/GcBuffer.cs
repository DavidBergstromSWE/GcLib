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
        // ToDo: Add check of valid pixel formats? Which ones are supported?

        // Validate array size.
        if (imageData.LongLength < width * height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8)
            throw new ArgumentException($"Allocated image data ({imageData.LongLength} bytes) is too small to fit specified {nameof(width)} ({width}), {nameof(height)} ({height}) and {nameof(pixelFormat)} ({pixelFormat})!");

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

        if (imageMat.NumberOfChannels != GenICamHelper.GetNumChannels(pixelFormat))
            throw new ArgumentException($"Mat image is incompatible with requested pixel format: Number of channels in image is {imageMat.NumberOfChannels}, while pixel format uses {GenICamHelper.GetNumChannels(pixelFormat)}!");

        if (EmguConverter.GetBitDepth(imageMat.Depth) > (int)GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat))
            throw new ArgumentException($"Mat image is incompatible with requested pixel format: Bit depth of image is {EmguConverter.GetBitDepth(imageMat.Depth)} (bits), while pixel format uses {GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat)}!");

        // Allocate new memory for image data.
        ImageData = new byte[imageMat.Total.ToInt32() * imageMat.ElementSize];

        // Copy image data.
        imageMat.CopyTo(ImageData);

        Width = (uint)imageMat.Width;
        Height = (uint)imageMat.Height;
        NumChannels = (uint)imageMat.NumberOfChannels;
        BitDepth = (uint)EmguConverter.GetBitDepth(imageMat.Depth);
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
        if (EmguConverter.GetPixelFormat(imageMat.Depth, imageMat.NumberOfChannels) == PixelFormat.InvalidPixelFormat)
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
        BitDepth = (uint)EmguConverter.GetBitDepth(imageMat.Depth);
        PixelFormat = EmguConverter.GetPixelFormat(imageMat.Depth, imageMat.NumberOfChannels);
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
        var mat = new Mat(rows: (int)Height, cols: (int)Width, EmguConverter.GetDepthType((PixelSize)BitDepth), (int)NumChannels);
        mat.SetTo(ImageData);
        return mat;

        // Alternative (probably same as above):
        //System.Runtime.InteropServices.GCHandle pinnedArray = System.Runtime.InteropServices.GCHandle.Alloc(ImageData, System.Runtime.InteropServices.GCHandleType.Pinned);
        //nint pointer = pinnedArray.AddrOfPinnedObject();
        //Mat mat = new Mat(rows: (int)Height, cols: (int)Width, EmguConverter.GetDepthType((PixelSize)BitDepth), (int)NumChannels, data: pointer, step: (int)Width * (int)BitDepth * (int)NumChannels / 8);
        //pinnedArray.Free();
        //return mat;
    }

    /// <summary>
    /// Get pixel value(s) from specified image coordinate.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <returns>Pixel value(s) from all image channels.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double[] GetPixel(uint row, uint col)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Create read-only span of bytes containing pixel values of all channels.
        var bytes = new ReadOnlySpan<byte>(array: ImageData,
                                           start: (int)((row * Width + col) * BitDepth / 8 * NumChannels),
                                           length: (int)BitDepth / 8 * (int)NumChannels);

        // Convert span to double array representing the pixel values of all channels.
        double[] values = new double[NumChannels];
        for (int i = 0; i < NumChannels; i++)
            values[i] = NumericHelper.SpanToDouble(bytes.Slice(start: i * (int)BitDepth / 8, length: (int)BitDepth / 8));

        return values;
    }

    /// <summary>
    /// Get pixel value from specified image coordinate and image channel.
    /// </summary>
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

        // Create read-only span of bytes containing pixel value of the requested channel.
        var bytes = new ReadOnlySpan<byte>(array: ImageData,
                                           start: (int)((row * Width + col) * BitDepth / 8 * NumChannels + BitDepth / 8 * channel),
                                           length: (int)BitDepth / 8);

        // Return pixel value casted as a double.
        return NumericHelper.SpanToDouble(bytes);
    }

    /// <summary>
    /// Set pixel values for multi-channeled image.
    /// </summary>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="pixelValues">Pixel values.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetPixel(uint row, uint col, double[] pixelValues)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Width, nameof(col));

        // Check that pixel value has valid number of elements.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pixelValues.Length, (int)NumChannels, nameof(pixelValues));

        // Check that pixel values fall within dynamic range.
        foreach (var value in pixelValues)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, PixelDynamicRangeMin, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, PixelDynamicRangeMax, nameof(value));
        }

        // Allocate span of bytes large enough to hold a pixel of the buffer.
        Span<byte> bytes = stackalloc byte[(int)(BitDepth / 8 * NumChannels)];

        // Convert pixel values to integral numeric type and store it in the span.
        NumericHelper.DoubleToSpan(ref bytes, pixelValues);

        // Update buffer with the new data.
        for (int i = 0; i < bytes.Length; i++)
            ImageData[(row * (int)Width + col) * (int)BitDepth / 8 * (int)NumChannels + i] = bytes[i];
    }

    /// <summary>
    /// Set pixel value for a specific channel.
    /// </summary>
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
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelValue, PixelDynamicRangeMin, nameof(pixelValue));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pixelValue, PixelDynamicRangeMax, nameof(pixelValue));

        // Allocate span of bytes large enough to hold a single-channel pixel of the buffer.
        Span<byte> bytes = stackalloc byte[(int)(BitDepth / 8)];

        // Convert pixel value to integral numeric type and store it in the span.
        NumericHelper.DoubleToSpan(ref bytes, pixelValue);

        // Update buffer with the new data.
        for (int i = 0; i < bytes.Length; i++)
            ImageData[(row * (int)Width + col) * (int)BitDepth / 8 * (int)NumChannels + channel * BitDepth / 8 + i] = bytes[i];
    }

    /// <summary>
    /// Retrieve size of image.
    /// </summary>
    /// <returns>Image size.</returns>
    public Size GetSize()
    {
        return new Size((int)Width, (int)Height);
    }

    public override bool Equals(object obj)
    {
        if ((obj is GcBuffer otherBuffer) == false)
            return false;

        if (otherBuffer.Width != Width || otherBuffer.Height != Height || otherBuffer.PixelFormat != PixelFormat || otherBuffer.FrameID != FrameID || otherBuffer.TimeStamp != TimeStamp || Enumerable.SequenceEqual(otherBuffer.ImageData, ImageData) == false)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
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