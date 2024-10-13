using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Emgu.CV;
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
    /// First input image channel.
    /// </summary>
    public ImageModel ImageChannel1 { get; }

    /// <summary>
    /// Second input image channel.
    /// </summary>
    public ImageModel ImageChannel2 { get; }

    /// <summary>
    /// Fused output image channel.
    /// </summary>
    public FusedImageModel FusedImageChannel { get; }

    /// <summary>
    /// Available image channels.
    /// </summary>
    public ImageModel[] ImageChannels { get; }

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

    #region Commands

    /// <summary>
    /// Relays a UI request to load a geometric calibration from file.
    /// </summary>
    public IRelayCommand LoadCalibrationDataCommand { get; }

    /// <summary>
    /// Relays a UI request to edit an existing geometric calibration.
    /// </summary>
    public IRelayCommand EditCalibrationDataCommand { get; }

    /// <summary>
    /// Relays a UI request to clear an existing geometric calibration.
    /// </summary>
    public IRelayCommand ClearCalibrationDataCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a view handling the processing of images.
    /// </summary>
    public ImageProcessingViewModel(IMetroWindowService dialogService, ImageModel[] imageChannels)
    {
        // Get required services.
        _windowService = dialogService;

        // Available image channels.
        ImageChannels = imageChannels;

        // Are these needed?
        ImageChannel1 = ImageChannels[0];
        ImageChannel2 = ImageChannels[1];
        FusedImageChannel = ImageChannels[2] as FusedImageModel;

        // Hook eventhandler to PropertyChanged events in image channels.
        foreach (ImageModel imageChannel in ImageChannels)
            imageChannel.PropertyChanged += ImageModel_PropertyChanged;

        // Default editing channel.
        SelectedImageChannel = ImageChannels[0];

        // Instantiate commands.
        LoadCalibrationDataCommand = new RelayCommand(LoadCalibrationDataFromFileDialog);
        EditCalibrationDataCommand = new RelayCommand(EditCalibrationData, HasCalibrationData);
        ClearCalibrationDataCommand = new RelayCommand(ClearCalibrationData, HasCalibrationData);

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Events

    /// <summary>
    /// Eventhandler to PropertyChanged events raised in an image channel.
    /// </summary>
    private void ImageModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is FusedImageModel fusedImageModel)
        {
            if (e.PropertyName == nameof(FusedImageModel.CalibrationData))
            {
                // Notify calibration commands of property changes.
                ClearCalibrationDataCommand.NotifyCanExecuteChanged();
                EditCalibrationDataCommand.NotifyCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Clear all stored images in all image channels.
    /// </summary>
    public void ClearImages()
    {
        foreach (ImageModel channel in ImageChannels)
            channel.ClearImages();
    }

    /// <summary>
    /// Clear images stored in specified image channel.
    /// </summary>
    /// <param name="imageChannel">Channel.</param>
    public void ClearImages(DisplayChannel imageChannel)
    {
        // Clear images in specified channel.
        var imageModel = Array.Find(ImageChannels, m => m.ImageChannel == imageChannel);
        imageModel.ClearImages();

        // Also clear fused channel.
        FusedImageChannel.ClearImages();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Checks if calibration data is available.
    /// </summary>
    /// <returns>True if data is available.</returns>
    private bool HasCalibrationData()
    {
        return FusedImageChannel.CalibrationData != null;
    }

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

            // Update affine transformation matrix.
            FusedImageChannel.CalibrationData = new GeometricCalibration(deviceIndex, new Matrix<double>(data).Transpose());

            ClearCalibrationDataCommand.NotifyCanExecuteChanged();
            EditCalibrationDataCommand.NotifyCanExecuteChanged();

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

    /// <summary>
    /// Opens a dialog window to edit the current geometric calibration.
    /// </summary>
    private void EditCalibrationData()
    {
        // Cache current settings.
        var data = FusedImageChannel.CalibrationData.AffineTransform.Clone();

        // Show dialog.
        var result = _windowService.ShowDialog(new EditCalibrationDialogWindowViewModel(FusedImageChannel));

        // Reset if cancelled.
        if (result == false)
        {
            FusedImageChannel.CalibrationData = new GeometricCalibration(DeviceIndex.Device1, data);
        }
    }

    /// <summary>
    /// Clear geometric calibration data.
    /// </summary>
    private void ClearCalibrationData()
    {
        FusedImageChannel.CalibrationData = new GeometricCalibration();

        ClearCalibrationDataCommand.NotifyCanExecuteChanged();
        EditCalibrationDataCommand.NotifyCanExecuteChanged();

        // Log information.
        Log.Information("Calibration data cleared");
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient of device connection messages.
        Messenger.Register<DeviceConnectedMessage>(this, (r, m) =>
        {
            ClearImages(m.DisplayChannel);
        });

        // Register as recipient of device disconnection messages.
        Messenger.Register<DeviceDisconnectedMessage>(this, (r, m) =>
        {
            ClearImages(m.DisplayChannel);

            // Reset processing settings.              
            Array.Find(ImageChannels, o => o.ImageChannel == m.DisplayChannel).InitializeSettings();
            FusedImageChannel.InitializeSettings();
        });
    }

    #endregion
}