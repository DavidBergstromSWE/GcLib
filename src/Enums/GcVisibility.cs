namespace GcLib;

/// <summary>
/// Parameter visibility.
/// </summary>
public enum GcVisibility
{
    /// <summary>
    /// Features that should be visible for all users via the GUI and API (default).
    /// </summary>
    Beginner = 0,
    /// <summary>
    /// Features that require a more in-depth knowledge of the camera functionality.
    /// </summary>
    Expert,
    /// <summary>
    /// Advanced features that might bring the cameras into a state where it will not work properly anymore if it is set incorrectly for the cameras current mode of operation.
    /// </summary>
    Guru,
    /// <summary>
    /// Features that should be kept hidden for the GUI users but still be available via the API.
    /// </summary>
    Invisible
}
