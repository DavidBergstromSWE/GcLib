using System.IO;
using System;
using ImagerViewer.ViewModels;

namespace ImagerViewer.Utilities.Services;

/// <summary>
/// Service providing loading/saving access to application settings.
/// </summary>
/// <param name="mainWindowViewModel">Main window settings.</param>
/// <param name="deviceViewModel">Device view settings.</param>
/// <param name="acquisitionViewModel">Acquisition view settings.</param>
internal class ApplicationSettingsService(MainWindowViewModel mainWindowViewModel, DeviceViewModel deviceViewModel, AcquisitionViewModel acquisitionViewModel) : ISettingsService
{
    /// <summary>
    /// Application settings.
    /// </summary>
    private readonly GcLib.Samples.WPFDemoApp.Properties.Settings _settings = GcLib.Samples.WPFDemoApp.Properties.Settings.Default;

    /// <inheritdoc />
    public void RestoreSettings()
    {
        // MainWindowViewModel settings.
        if (mainWindowViewModel.Themes.Exists(t => t.Name == GcLib.Samples.WPFDemoApp.Properties.Settings.Default.ThemeName))
            mainWindowViewModel.SelectedTheme = mainWindowViewModel.Themes.Find(theme => theme.Name == GcLib.Samples.WPFDemoApp.Properties.Settings.Default.ThemeName);

        // DeviceViewModel settings.
        if (Enum.TryParse(_settings.UserVisibility, out Visibility result))
            deviceViewModel.UserVisibility = result;
        deviceViewModel.DeviceParameterUpdateTimeDelay = _settings.ParameterUpdateDelay;

        // AcquisitionViewModel settings.
        acquisitionViewModel.AutoGenerateFileNames = _settings.AutoGenerateFileNames;
        acquisitionViewModel.AcquisitionChannel.SaveRawData = _settings.SaveRawData;
        acquisitionViewModel.AcquisitionChannel.SaveProcessedData = _settings.SaveProcessedData;
        if (Directory.Exists(Path.GetDirectoryName(_settings.SaveFilePath)))
            acquisitionViewModel.AcquisitionChannel.FilePath = _settings.SaveFilePath;
    }

    /// <inheritdoc />
    public void StoreSettings()
    {
        // MainWindow settings.
        _settings.ThemeName = mainWindowViewModel.SelectedTheme.Name;

        // DeviceView settings.
        _settings.UserVisibility = deviceViewModel.UserVisibility.ToString();
        _settings.ParameterUpdateDelay = deviceViewModel.DeviceParameterUpdateTimeDelay;

        // AcquisitionView settings.
        _settings.AutoGenerateFileNames = acquisitionViewModel.AutoGenerateFileNames;
        _settings.SaveRawData = acquisitionViewModel.AcquisitionChannel.SaveRawData;
        _settings.SaveProcessedData = acquisitionViewModel.AcquisitionChannel.SaveProcessedData;
        _settings.SaveFilePath = acquisitionViewModel.AcquisitionChannel.FilePath;

        // Save settings.
        _settings.Save();
    }
}
