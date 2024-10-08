using System;

namespace GcLib;

/// <summary>
/// Provides data for a <see cref="GcDataStream.BufferTransferred"/> event.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the <see cref="GcDataStream.BufferTransferred"/> event.
/// </remarks>
/// <param name="buffer">Transferred image buffer.</param>
public sealed class BufferTransferredEventArgs(GcBuffer buffer) : EventArgs
{
    /// <summary>
    /// Image buffer transferred.
    /// </summary>
    public GcBuffer Buffer { get; } = buffer;
}