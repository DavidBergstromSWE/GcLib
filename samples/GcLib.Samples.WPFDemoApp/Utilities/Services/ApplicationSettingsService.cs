using System.IO;
using System;
using FusionViewer.ViewModels;

namespace FusionViewer.Utilities.Services;

/// <summary>
/// Service providing loading/saving access to application settings.
/// </summary>
/// <param name="mainWindowViewModel">Main window settings.</param>
/// <param name="deviceViewModel">Device view settings.</param>
/// <param name="acquisitionViewModel">Acquisition view settings.</param>
/// <param name="imageDisplayViewModel">Image display view settings.</param>
internal class ApplicationSettingsService(MainWindowViewModel mainWindowViewModel, DeviceViewModel deviceViewModel, AcquisitionViewModel acquisitionViewModel, ImageDisplayViewModel imageDisplayViewModel) : ISettingsService
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
        acquisitionViewModel.AcquisitionChannel1.SaveRawData = _settings.Channel1SaveRawData;
        acquisitionViewModel.AcquisitionChannel1.SaveProcessedData = _settings.Channel1SaveProcessedData;
        if (Directory.Exists(Path.GetDirectoryName(_settings.Channel1SaveFilePath)))
            acquisitionViewModel.AcquisitionChannel1.FilePath = _settings.Channel1SaveFilePath;
        acquisitionViewModel.AcquisitionChannel2.SaveRawData = _settings.Channel2SaveRawData;
        acquisitionViewModel.AcquisitionChannel2.SaveProcessedData = _settings.Channel2SaveProcessedData;
        if (Directory.Exists(Path.GetDirectoryName(_settings.Channel2SaveFilePath)))
            acquisitionViewModel.AcquisitionChannel2.FilePath = _settings.Channel2SaveFilePath;
        acquisitionViewModel.FusedAcquisitionChannel.SaveRawData = _settings.FusedChannelSaveRawData;
        acquisitionViewModel.FusedAcquisitionChannel.SaveProcessedData = _settings.FusedChannelSaveProcessedData;
        if (Directory.Exists(Path.GetDirectoryName(_settings.FusedChannelSaveFilePath)))
            acquisitionViewModel.FusedAcquisitionChannel.FilePath = _settings.FusedChannelSaveFilePath;
        acquisitionViewModel.LogTimeStamps = _settings.LogTimeStamps;
        acquisitionViewModel.AppendTimeStamps = _settings.AppendTimeStamps;

        // ImageDisplayView settings.
        imageDisplayViewModel.SynchronizeViews = _settings.SynchronizeViews;
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
        _settings.Channel1SaveRawData = acquisitionViewModel.AcquisitionChannel1.SaveRawData;
        _settings.Channel1SaveProcessedData = acquisitionViewModel.AcquisitionChannel1.SaveProcessedData;
        _settings.Channel1SaveFilePath = acquisitionViewModel.AcquisitionChannel1.FilePath;
        _settings.Channel2SaveRawData = acquisitionViewModel.AcquisitionChannel2.SaveRawData;
        _settings.Channel2SaveProcessedData = acquisitionViewModel.AcquisitionChannel2.SaveProcessedData;
        _settings.Channel2SaveFilePath = acquisitionViewModel.AcquisitionChannel2.FilePath;
        _settings.FusedChannelSaveRawData = acquisitionViewModel.FusedAcquisitionChannel.SaveRawData;
        _settings.FusedChannelSaveProcessedData = acquisitionViewModel.FusedAcquisitionChannel.SaveProcessedData;
        _settings.FusedChannelSaveFilePath = acquisitionViewModel.FusedAcquisitionChannel.FilePath;
        _settings.LogTimeStamps = acquisitionViewModel.LogTimeStamps;
        _settings.AppendTimeStamps = acquisitionViewModel.AppendTimeStamps;

        // ImageDisplayView settings.
        _settings.SynchronizeViews = imageDisplayViewModel.SynchronizeViews;

        // Save settings.
        _settings.Save();
    }
}
