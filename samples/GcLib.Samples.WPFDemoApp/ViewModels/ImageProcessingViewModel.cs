using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FusionViewer.Models;
using FusionViewer.Utilities.Dialogs;
using FusionViewer.Utilities.Messages;
using FusionViewer.Utilities.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog;

namespace FusionViewer.ViewModels;

/// <summary>
/// Models a view handling the processing of input (device) and output (fusion) channel images.
/// </summary>
internal sealed class ImageProcessingViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private ImageModel _selectedImageChannel;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    #endregion

    #region Properties

    /// <summary>
    /// Selected image channel for editing processing settings.
    /// </summary>
    public ImageModel SelectedImageChannel
    {
        get => _selectedImageChannel;
        set
        {
            if (SetProperty(ref _selectedImageChannel, value))
            {
                // Notify command logic of property change.
            }
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a view handling the processing of images.
    /// </summary>
    public ImageProcessingViewModel(IMetroWindowService dialogService, ImageModel imageChannel)
    {
        // Get required services.
        _windowService = dialogService;

        // Available image channels.
        SelectedImageChannel = imageChannel;

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Clear all stored images in all image channels.
    /// </summary>
    public void ClearImages()
    {
        SelectedImageChannel.ClearImages();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Load calibration data from file dialog window.
    /// </summary>
    private void LoadCalibrationDataFromFileDialog()
    {
        // Retrieve filepath from dialog.
        string filePath = _windowService.ShowOpenFileDialog(title: "Select calibration file", multiSelect: false, filter: "Calibration files (*.csv)|*.csv", defaultExtension: "csv");
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            // ToDo: Add file format validation.
            using var reader = new StreamReader(filePath);

            var provider = new NumberFormatInfo { NumberDecimalSeparator = "." };

            double[,] data = new double[3, 2];

            int rowIndex = 0;

            // Read moving channel (image to transform while the other remains fixed).
            string[] stringValues = reader.ReadLine().Split(',');
            if (stringValues[0] != "movingChannel") // also allow fixedChannel?
                throw new FileFormatException("Calibration file is not in correct format!");

            // Read device index for moving channel.
            DeviceIndex deviceIndex;
            if (stringValues[1] == DeviceIndex.Device1.ToString())
                deviceIndex = DeviceIndex.Device1;
            else if (stringValues[1] == DeviceIndex.Device2.ToString())
                deviceIndex = DeviceIndex.Device2;
            else throw new FileFormatException("Calibration file is not in correct format!");

            // Read transformation matrix.
            while (reader.EndOfStream == false && rowIndex < 4)
            {
                stringValues = reader.ReadLine().Split(',');
                data[rowIndex, 0] = Convert.ToDouble(stringValues[0], provider);
                data[rowIndex, 1] = Convert.ToDouble(stringValues[1], provider);
                rowIndex++;
            }

            // Confirm in UI.
            _ = _windowService.ShowMessageDialog(this, "File loaded!", $"Calibration file \'{filePath}\' successfully loaded.", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

            // Log information.
            Log.Information("Calibration file {FileName} successfully loaded", filePath);
        }
        catch (Exception ex)
        {
            _ = _windowService.ShowMessageDialog(this, "File error!", $"Unable to load calibration file! {ex.Message}", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

            // Log error.
            Log.Error(ex, "Failed to load calibration from {FileName}", filePath);
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient of device connection messages.
        Messenger.Register<DeviceConnectedMessage>(this, (r, m) =>
        {
            ClearImages();
        });

        // Register as recipient of device disconnection messages.
        Messenger.Register<DeviceDisconnectedMessage>(this, (r, m) =>
        {
            ClearImages();

            // Reset processing settings.              
            SelectedImageChannel.InitializeSettings();
        });
    }

    #endregion
}