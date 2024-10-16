using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FusionViewer.Models;
using FusionViewer.Utilities.Messages;

namespace FusionViewer.ViewModels;

/// <summary>
/// Models a view handling the processing of input (device) images.
/// </summary>
internal sealed class ImageProcessingViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private ImageModel _selectedImageChannel;


    #endregion

    #region Properties

    /// <summary>
    /// Selected image channel for editing processing settings.
    /// </summary>
    public ImageModel SelectedImageChannel
    {
        get => _selectedImageChannel;
        set => SetProperty(ref _selectedImageChannel, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a view handling the processing of images.
    /// </summary>
    public ImageProcessingViewModel(ImageModel imageChannel)
    {
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