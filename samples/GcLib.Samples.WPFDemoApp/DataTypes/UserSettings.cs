namespace WPFDemoApp;

/// <summary>
/// Class representing user settings for the application.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Selected theme.
    /// </summary>
    public string Theme { get; set; } = "Light.Blue";

    /// <summary>
    /// User visibility level.
    /// </summary>
    public string UserVisibility { get; set; } = Visibility.Guru.ToString();

    /// <summary>
    /// Delay in milliseconds for updating device parameters.
    /// </summary>
    public double ParameterUpdateDelay { get; set; } = 500;

    /// <summary>
    /// Indicates whether to save raw data during acquisition.
    /// </summary>
    public bool SaveRawData { get; set; } = false;

    /// <summary>
    /// Indicates whether to save processed data during acquisition.
    /// </summary>
    public bool SaveProcessedData { get; set; } = false;

    /// <summary>
    /// Indicates whether to automatically generate binary file paths during acquisition.
    /// </summary>
    public bool AutoGenerateBinaryFilePath { get; set; } = false;

    /// <summary>
    /// Indicates whether to save video during acquisition.
    /// </summary>
    public bool SaveVideo { get; set; } = true;

    /// <summary>
    /// Path to the folder for saving recordings during acquisition.
    /// </summary>
    public string RecordingFolderPath { get; set; } = @"C:\testdata";
}
