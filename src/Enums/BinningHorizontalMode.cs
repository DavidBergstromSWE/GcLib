namespace GcLib;

/// <summary>
/// Horizontal binning mode.
/// </summary>
public enum BinningHorizontalMode
{
    /// <summary>
    /// The response from the combined cells will be added, resulting in increased sensitivity.
    /// </summary>
    Sum,
    /// <summary>
    /// The response from the combined cells will be averaged, resulting in increased signal/noise ratio.
    /// </summary>
    Average
}
