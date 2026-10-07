using GcLib;

namespace WPFDemoApp;

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
