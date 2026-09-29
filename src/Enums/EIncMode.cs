namespace GcLib;

/// <summary>
/// Type of increment.
/// </summary>
public enum EIncMode
{
    /// <summary>
    /// No increments.
    /// </summary>
    noIncrement,
    /// <summary>
    /// Fixed range between increments.
    /// </summary>
    fixedIncrement,
    /// <summary>
    /// List of increments.
    /// </summary>
    listIncrement
}