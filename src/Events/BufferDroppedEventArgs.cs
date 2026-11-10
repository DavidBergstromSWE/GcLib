using System;

namespace GcLib;

/// <summary>
/// Provides data for a <see cref="GcDataStream.BufferDropped"/> event.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the <see cref="GcDataStream.BufferDropped"/> event.
/// </remarks>
/// <param name="lostBufferCount">Total (aggregated) number of buffers lost from the datastream.</param>
public sealed class BufferDroppedEventArgs(ulong lostBufferCount) : EventArgs
{
    /// <summary>
    /// Total (aggregated) number of buffers lost from the datastream.
    /// </summary>
    public ulong LostBufferCount { get; } = lostBufferCount;
}