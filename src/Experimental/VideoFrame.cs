using System;
using System.Buffers;
using GcLib.Utilities.Imaging;

namespace GcLib;

/// <summary>
/// Represents a video frame, including its image data and metadata.
/// </summary>
/// <remarks>The image data is stored in a pooled buffer. Dispose the frame when it is no longer needed to return
/// the buffer to its pool.</remarks>
public class VideoFrame : IDisposable
{
    #region Private fields

    /// <summary>
    /// The byte array pool used to rent and return buffers.
    /// </summary>
    private readonly ArrayPool<byte> _pool;

    /// <summary>
    /// The rented buffer used to store video frame data.
    /// </summary>
    private byte[] _rentedBuffer;

    /// <summary>
    /// Indicates whether the object has been disposed.
    /// </summary>
    private bool _isDisposed;
    private readonly object _lifecycleLock = new();

    #endregion

    #region Public properties

    /// <summary>
    /// View of image data.
    /// </summary>
    public Memory<byte> ImageData { get; private set; }

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
    /// Timestamp of image, in <see cref="DateTime"/> ticks.
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// Frame ID of image.
    /// </summary>
    public long FrameID { get; }

    /// <summary>
    /// Number of channels in the image.
    /// </summary>
    public uint NumChannels => GenICamHelper.GetNumChannels(PixelFormat);

    /// <summary>
    /// Maximum pixel value (e.g., 255 for 8-bit, 1023 for 10-bit, etc.).
    /// </summary>
    public uint PixelDynamicRangeMax => GenICamHelper.GetPixelDynamicRangeMax(PixelFormat);

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a video frame and rents storage for its image data.
    /// </summary>
    /// <param name="width">The frame width in pixels.</param>
    /// <param name="height">The frame height in pixels.</param>
    /// <param name="pixelFormat">The pixel format of the frame.</param>
    /// <param name="frameID">The identifier of the frame.</param>
    /// <param name="timestamp">The frame timestamp.</param>
    /// <param name="pool">The byte array pool used to rent storage for the image data.</param>
    public VideoFrame(uint width, uint height, PixelFormat pixelFormat, long frameID, ulong timestamp, ArrayPool<byte> pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);

        ulong requiredBytes;
        try
        {
            requiredBytes = checked(((ulong)width * height * GenICamHelper.GetBitsPerPixel(pixelFormat) + 7UL) / 8UL);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Frame dimensions and pixel format require an unsupported buffer size.");
        }

        if (requiredBytes > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(width), "Frame dimensions and pixel format require a buffer larger than the maximum supported array size.");

        _pool = pool;
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Timestamp = timestamp;
        FrameID = frameID;

        // Rent a byte array from the pool that is large enough to hold the frame data.
        int requiredLength = (int)requiredBytes;
        _rentedBuffer = _pool.Rent(requiredLength);

        // Create a Memory<byte> that represents the rented buffer. This allows for safe access to the buffer without exposing the underlying array directly.
        ImageData = new Memory<byte>(_rentedBuffer, 0, requiredLength);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Copies the data from native memory into the rented buffer.
    /// </summary>
    /// <remarks>This method can be called to copy raw data from device memory.</remarks>
    /// <param name="nativePtr">A pointer to the source memory, which must contain at least as many bytes as the data.</param>
    public unsafe void CopyFromNativeMemory(IntPtr nativePtr)
    {
        if (nativePtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(nativePtr));

        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            byte[] rentedBuffer = _rentedBuffer;
            fixed (byte* destPtr = rentedBuffer)
                Buffer.MemoryCopy((void*)nativePtr, destPtr, rentedBuffer.Length, ImageData.Length);
        }
    }

    /// <summary>
    /// Returns the rented buffer to its pool.
    /// </summary>
    /// <remarks>Subsequent calls have no effect.</remarks>
    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_isDisposed == false)
            {
                _isDisposed = true;
                byte[] rentedBuffer = _rentedBuffer;
                _rentedBuffer = null!;
                ImageData = Memory<byte>.Empty;
                _pool.Return(rentedBuffer, clearArray: false);
            }
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}