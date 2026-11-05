using System;

namespace GcLib;

/// <summary>
/// Provides data for an event announcing a new buffer.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the event.
/// </remarks>
/// <param name="streamID">Unique ID of datastream that produced the buffer.</param>
/// <param name="receptionTime">Reception time at host (PC).</param>
/// <param name="width">Image width in pixels.</param>
/// <param name="height">Image height in pixels.</param>
/// <param name="pixelFormat">Image pixel format.</param>
/// <param name="frameID">Frame ID of image.</param>
/// <param name="timeStamp">Image timestamp in <see cref="DateTime"/> ticks.</param>
/// <param name="payloadSize">Image payload size in bytes.</param>
/// <param name="copyImageData">Callback that copies image payload into destination byte array.</param>
public sealed class NewBufferEventArgs(string streamID,
                                       DateTime receptionTime,
                                       uint width,
                                       uint height,
                                       PixelFormat pixelFormat,
                                       long frameID,
                                       ulong timeStamp,
                                       int payloadSize,
                                       Action<byte[]> copyImageData) : EventArgs
{
    /// <summary>
    /// Unique ID of the datastream that produced the buffer.
    /// </summary>
    public string StreamID { get; } = streamID;

    /// <summary>
    /// Reception time of new buffer at host (PC).
    /// </summary>
    public DateTime ReceptionTime { get; } = receptionTime;

    /// <summary>
    /// Width of image in pixels.
    /// </summary>
    public uint Width { get; } = width;

    /// <summary>
    /// Height of image in pixels.
    /// </summary>
    public uint Height { get; } = height;

    /// <summary>
    /// Pixel format of image.
    /// </summary>
    public PixelFormat PixelFormat { get; } = pixelFormat;

    /// <summary>
    /// Frame ID of image.
    /// </summary>
    public long FrameID { get; } = frameID;

    /// <summary>
    /// Timestamp of image in <see cref="DateTime"/> ticks.
    /// </summary>
    public ulong TimeStamp { get; } = timeStamp;

    /// <summary>
    /// Payload size in bytes.
    /// </summary>
    public int PayloadSize { get; } = payloadSize;

    /// <summary>
    /// Callback used to copy image payload into a destination byte array.
    /// </summary>
    public Action<byte[]> CopyImageData { get; } = copyImageData;
}
