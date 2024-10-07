using System;

namespace GcLib;

/// <summary>
/// Provides data for an event announcing a new buffer.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the event.
/// </remarks>
/// <param name="buffer">Buffer.</param>
/// <param name="receptionTime">Reception time at host (PC).</param>
public sealed class NewBufferEventArgs(GcBuffer buffer, DateTime receptionTime) : EventArgs
{
    /// <summary>
    /// The new buffer.
    /// </summary>
    public GcBuffer Buffer { get; } = buffer;

    /// <summary>
    /// Reception time of new buffer at host (PC).
    /// </summary>
    public DateTime ReceptionTime { get; } = receptionTime;
}