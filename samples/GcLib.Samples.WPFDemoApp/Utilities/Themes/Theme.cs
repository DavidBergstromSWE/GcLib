using System.Windows.Media;

namespace ImagerViewer.Utilities.Themes;

/// <summary>
/// Stores data about a theme for the user interface.
/// </summary>
/// <remarks>
/// Creates a theme for the user interface
/// </remarks>
/// <param name="name">Name of theme.</param>
/// <param name="schemeColor">Scheme color for theme.</param>
/// <param name="baseColor">Base color for theme.</param>
/// <param name="brush">Brush to be used for showcasing theme.</param>
/// <param name="backgroundBrush">Background brush of theme.</param>
/// <param name="foregroundBrush">Foreground brush of theme.</param>
public readonly struct Theme(string name, string schemeColor, string baseColor, Brush brush, Brush foregroundBrush, Brush backgroundBrush)
{
    /// <summary>
    /// Name of theme.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Scheme color for theme.
    /// </summary>
    public string SchemeColor { get; } = schemeColor;

    /// <summary>
    /// Base color for theme.
    /// </summary>
    public string BaseColor { get; } = baseColor;

    /// <summary>
    /// Brush to be used for showcasing theme.
    /// </summary>
    public Brush Brush { get; } = brush;

    /// <summary>
    /// Foreground brush of theme.
    /// </summary>
    public Brush ForegroundBrush { get; } = foregroundBrush;

    /// <summary>
    /// Background brush of theme.
    /// </summary>
    public Brush BackgroundBrush { get; } = backgroundBrush;
}