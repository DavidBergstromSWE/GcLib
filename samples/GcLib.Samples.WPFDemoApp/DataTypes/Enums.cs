using GcLib;

namespace ImagerViewer;

/// <summary>
/// Device parameter visibility.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Features that should be visible for all users via the GUI and API (default).
    /// </summary>
    Beginner = GcVisibility.Beginner,
    /// <summary>
    /// Features that require a more in-depth knowledge of the camera functionality.
    /// </summary>
    Expert = GcVisibility.Expert,
    /// <summary>
    /// Advanced features that might bring the cameras into a state where it will not work properly anymore if it is set incorrectly for the cameras current mode of operation.
    /// </summary>
    Guru = GcVisibility.Guru
}

/// <summary>
/// Base color for themes.
/// </summary>
public enum ThemeBaseColor
{
    /// <summary>
    /// Light background with dark text.
    /// </summary>
    Light = 0,
    /// <summary>
    /// Dark background with light text.
    /// </summary>
    Dark = 1
}