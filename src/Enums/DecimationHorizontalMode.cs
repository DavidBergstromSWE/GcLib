namespace GcLib;

/// <summary>
/// Mode used to reduce the horizontal resolution.
/// </summary>
public enum DecimationHorizontalMode
{
    /// <summary>
    /// The value of every Nth pixel is kept, others are discarded.
    /// </summary>
    Discard,
    /// <summary>
    /// The values of a group of N adjacent pixels are averaged.
    /// </summary>
    Average
}
