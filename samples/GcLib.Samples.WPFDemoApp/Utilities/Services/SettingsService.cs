using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace WPFDemoApp.Utilities.Services;

/// <summary>
/// Service providing access to application settings.
/// </summary>
internal class SettingsService : ISettingsService
{
    /// <summary>
    /// Path to the folder for storing application settings.
    /// </summary>
    private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WPFDemoApp");

    /// <summary>
    /// Path to the file for storing application settings.
    /// </summary>
    private static readonly string FilePath = Path.Combine(FolderPath, "appsettings.json");

    /// <summary>
    /// JSON serializer options for serializing and deserializing application settings.
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UserSettings Current { get; private set; } = new();

    public void Restore()
    {
        // Check if the settings file exists.
        if (File.Exists(FilePath))
        {
            try
            {
                // Read the JSON content from the settings file and deserialize it into the current settings.
                string json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings(); // If deserialization fails, create a new instance of UserSettings.
                return;
            }
            catch
            {
                /* Log errors here */
                Log.Error("Failed to restore user settings from file, Creating a new instance of UserSettings.");
            }
        }

        Current = new UserSettings(); // If the settings file does not exist or an error occurs during deserialization, create a new instance of UserSettings.
    }

    public void Store()
    {
        // Ensure the folder for storing application settings exists.
        Directory.CreateDirectory(FolderPath);

        // Serialize the current settings to JSON and write it to the settings file.
        string json = JsonSerializer.Serialize(Current, _jsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
