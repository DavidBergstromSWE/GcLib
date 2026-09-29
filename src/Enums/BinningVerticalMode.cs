namespace GcLib;

/// <summary>
/// Vertical binning mode.
/// </summary>
public enum BinningVerticalMode
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
