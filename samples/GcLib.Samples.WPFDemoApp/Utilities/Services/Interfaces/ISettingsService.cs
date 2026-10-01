namespace WPFDemoApp.Utilities.Services;

/// <summary>
/// Interface for a service providing access to application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Current application settings.
    /// </summary>
    UserSettings Current { get; }

    /// <summary>
    /// Restore application settings.
    /// </summary>
    public void Restore();

    /// <summary>
    /// Store application settings.
    /// </summary>
    public void Store();
}