using System;

namespace GcLib;

/// <summary>
/// Provides data for an event announcing a new buffer.
/// </summary>
/// <remarks>
/// Creates event args with stream information and the acquired buffer.
/// </remarks>
/// <param name="buffer">Acquired buffer.</param>
/// <param name="receptionTime">Reception time of new buffer at host (PC).</param>
/// <param name="streamID">Unique ID of the datastream that produced the buffer.</param>
public sealed class NewBufferEventArgs(string streamID, DateTime receptionTime, GcBuffer buffer) : EventArgs
{
    /// <summary>
    /// Unique ID of the datastream that produced the buffer.
    /// </summary>
    public string StreamID { get; } = streamID ?? throw new ArgumentNullException(nameof(streamID));

    /// <summary>
    /// Reception time of new buffer at host (PC).
    /// </summary>
    public DateTime ReceptionTime { get; } = receptionTime;

    /// <summary>
    /// Buffer data and metadata.
    /// </summary>
    public GcBuffer Buffer { get; } = buffer ?? throw new ArgumentNullException(nameof(buffer));
}
