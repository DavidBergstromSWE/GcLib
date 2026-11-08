using System;
using GcLib.Utilities.Numbers;

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
    /// Starts streaming buffers continuously until manually stopped. If <paramref name="numBuffers"/> is specified the acquisition stops automatically after the specified number of buffers have been acquired.
    /// </summary>
    /// <param name="numBuffers">(Optional) Number of buffers to acquire.</param>
    public void Start(ulong numBuffers = Constants.GENTL_INFINITE);

    /// <summary>
    /// Stop streaming buffers.
    /// </summary>
    public void Stop();

    /// <summary>
    /// Retrieves a buffer from the stream.
    /// </summary>
    /// <param name="buffer">Buffer (or null if none is available).</param>
    /// <param name="timeout">Timeout in milliseconds to wait for a buffer. Use zero for non-blocking retrieval.</param>
    /// <returns>True if buffer retrieval was successful, false if not.</returns>
    bool RetrieveBuffer(out GcBuffer buffer, int timeout = 1000);

    /// <summary>
    /// Event announcing that streaming has started.
    /// </summary>
    event EventHandler StreamingStarted;

    /// <summary>
    /// Event announcing that streaming has stopped.
    /// </summary>
    event EventHandler StreamingStopped;

    /// <summary>
    /// Event announcing that a buffer has been successfully transferred to the stream, containing the buffer in the event arguments.
    /// </summary>
    event EventHandler<BufferTransferredEventArgs> BufferTransferred;

    /// <summary>
    /// Event announcing that a new buffer has been added to the stream, indicating to registered consumers that a buffer is available for retrieval (using <see cref="RetrieveBuffer(out GcBuffer, int)"/>).
    /// </summary>
    event EventHandler NewBuffer;
}