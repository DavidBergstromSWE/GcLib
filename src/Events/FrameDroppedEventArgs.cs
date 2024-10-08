using System;

namespace GcLib;

/// <summary>
/// Provides data for a <see cref="GcDataStream.FrameDropped"/> event.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the <see cref="GcDataStream.FrameDropped"/> event.
/// </remarks>
/// <param name="lostFrameCount">Total (aggregated) number of frames lost from the datastream.</param>
public sealed class FrameDroppedEventArgs(ulong lostFrameCount) : EventArgs
{
    /// <summary>
    /// Total (aggregated) number of frames lost from the datastream.
    /// </summary>
    public ulong LostFrameCount { get; } = lostFrameCount;
}