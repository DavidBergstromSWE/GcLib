using System;

namespace GcLib;

/// <summary>
/// Interface for a stream of buffers.
/// </summary>
public interface IBufferStream
{
    /// <summary>
    /// Unique ID of stream.
    /// </summary>
    string StreamID { get; }

    /// <summary>
    /// Current streaming status.
    /// </summary>
    bool IsStreaming { get; }

    /// <summary>
    /// Starts streaming buffers continuously until manually stopped. If <paramref name="numImages"/> is specified the acquisition stops automatically after the specified number of images have been acquired.
    /// </summary>
    /// <param name="numImages">(Optional) Number of images to acquire.</param>
    public void Start(ulong numImages = Constants.GENTL_INFINITE);

    /// <summary>
    /// Stop streaming buffers.
    /// </summary>
    public void Stop();

    /// <summary>
    /// Event announcing that streaming has started.
    /// </summary>
    event EventHandler StreamingStarted;

    /// <summary>
    /// Event announcing that streaming has stopped.
    /// </summary>
    event EventHandler StreamingStopped;

    /// <summary>
    /// Event announcing that an buffer has been successfully transferred, containing the buffer in the event arguments.
    /// </summary>
    event EventHandler<BufferTransferredEventArgs> BufferTransferred;
}