using System.Windows.Input;

namespace ImagerViewerApp;

/// <summary>
/// Represents a keyboard shortcut in the application.
/// </summary>
/// <remarks>
/// Creates a new keyboard shortcut.
/// </remarks>
/// <param name="keyGesture">Key and modifier of shortcut.</param>
/// <param name="category">Category for shortcut.</param>
/// <param name="description">Description of shortcut.</param>
internal readonly struct Shortcut(KeyGesture keyGesture, string category, string description)
{
    /// <summary>
    /// Description of keyboard shortcut.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Key and modifier for keyboard shortcut.
    /// </summary>
    public KeyGesture KeyGesture { get; } = keyGesture;

    /// <summary>
    /// Category of keyboard shortcut.
    /// </summary>
    public string Category { get; } = category;
}