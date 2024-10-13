namespace FusionViewer.Utilities.Services;

/// <summary>
/// Interface for a service providing access to application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Restore application settings.
    /// </summary>
    public void RestoreSettings();

    /// <summary>
    /// Store application settings.
    /// </summary>
    public void StoreSettings();
}