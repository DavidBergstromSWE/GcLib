namespace FusionViewer.Utilities.Messages;

/// <summary>
/// Message pairing two timestamps.
/// </summary>
/// <remarks>
/// Creates a new message pairing two timestamps.
/// </remarks>
/// <param name="timeStamp1">First timestamp.</param>
/// <param name="timeStamp2">Second timestamp.</param>
internal sealed class TimeStampPairingMessage(ulong? timeStamp1, ulong? timeStamp2)
{
    /// <summary>
    /// First timestamp.
    /// </summary>
    public ulong? TimeStamp1 { get; } = timeStamp1;

    /// <summary>
    /// Second timestamp.
    /// </summary>
    public ulong? TimeStamp2 { get; } = timeStamp2;
}