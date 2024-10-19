using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ImagerViewer.Models;
using ImagerViewer.Utilities.Messages;

namespace ImagerViewer.ViewModels;

/// <summary>
/// Models a view handling the processing of input (device) images.
/// </summary>
internal sealed class ImageProcessingViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private ImageModel _imageChannel;


    #endregion

    #region Properties

    /// <summary>
    /// Image data channel for processing.
    /// </summary>
    public ImageModel ImageChannel
    {
        get => _imageChannel;
        set => SetProperty(ref _imageChannel, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a view handling the processing of images.
    /// </summary>
    /// <param name="imageChannel">Image data channel.</param>
    public ImageProcessingViewModel(ImageModel imageChannel)
    {
        ImageChannel = imageChannel;

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Private methods

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient of device connection messages.
        Messenger.Register<DeviceConnectedMessage>(this, (r, m) =>
        {
            ImageChannel.ClearImages();
        });

        // Register as recipient of device disconnection messages.
        Messenger.Register<DeviceDisconnectedMessage>(this, (r, m) =>
        {
            ImageChannel.ClearImages();

            // Reset processing settings.              
            ImageChannel.InitializeSettings();
        });
    }

    #endregion
}