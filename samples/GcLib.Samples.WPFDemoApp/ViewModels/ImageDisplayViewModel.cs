using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FusionViewer.Models;
using FusionViewer.Utilities.Messages;
using FusionViewer.Utilities.Services;
using GcLib;
using GcLib.Utilities.Collections;
using GcLib.Utilities.Threading;

namespace FusionViewer.ViewModels;

/// <summary>
/// View model exposing properties and commands related to the displaying of images.
/// </summary>
internal sealed partial class ImageDisplayViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private ImageModel _selectedChannel;
    private DisplayChannel _selectedDisplayChannel;
    private BitmapScalingMode _bitmapScalingMode;
    private bool _showPixelInspector;
    private bool _limitFPS;
    private uint _targetFPS;
    private bool _showFullScreenFrameInfo;
    private bool _showFullScreenChannelInfo;
    private bool _synchronizeViews;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Service providing dispatching and running of actions onto the UI thread.
    /// </summary>
    private readonly IDispatcherService _dispatcherService;

    /// <summary>
    /// Frame rate manager, used for stabilizing frame rate.
    /// </summary>
    private readonly FPSStabilizer _frameStabilizer = new();

    /// <summary>
    /// Stores a display mode for subsequent retrieval.
    /// </summary>
    private DisplayChannel _cachedDisplayChannel;

    /// <summary>
    /// Stores fps limit setting for subsequent retrieval.
    /// </summary>
    private bool _cachedLimitFPS;

    /// <summary>
    /// True if selected display channel can be changed.
    /// </summary>
    private bool _canChangeSelectedDisplayChannel = true;

    /// <summary>
    /// Circular buffer of timestamps used for frame rate calculations.
    /// </summary>
    private readonly CircularBuffer<ulong> _timeStamps = new(10, true);

    #endregion

    #region Properties

    /// <summary>
    /// Image channel 1.
    /// </summary>
    public ImageModel Channel1 { get; }

    /// <summary>
    /// Image channel 2.
    /// </summary>
    public ImageModel Channel2 { get; }

    /// <summary>
    /// Selected display mode.
    /// </summary>
    public DisplayChannel SelectedDisplayChannel
    {
        get => _selectedDisplayChannel;
        set
        {
            if (SetProperty(ref _selectedDisplayChannel, value))
            {
                // Unsubscribe from image updates in all channels.
                Channel1.ImagesUpdated -= Channel_ImagesUpdated;
                Channel2.ImagesUpdated -= Channel_ImagesUpdated;

                // Subscribe to image updates in selected channel.
                if (SelectedDisplayChannel != DisplayChannel.All)
                {
                    SelectedChannel.ImagesUpdated += Channel_ImagesUpdated;
                }
                else
                {
                    Channel1.ImagesUpdated += Channel_ImagesUpdated;
                    Channel2.ImagesUpdated += Channel_ImagesUpdated;
                }

                UpdateImages();
            }
        }
    }

    /// <summary>
    /// Selected image channel for display.
    /// </summary>
    public ImageModel SelectedChannel
    {
        get => _selectedChannel;
        set => SetProperty(ref _selectedChannel, value);
    }

    /// <summary>
    /// Raw image of channel 1.
    /// </summary>
    public GcBuffer Channel1SourceImage => Channel1.RawImage;

    /// <summary>
    /// Processed image of channel 1.
    /// </summary>
    public GcBuffer Channel1ProcessedImage => Channel1.ProcessedImage;

    /// <summary>
    /// Raw image of channel 2.
    /// </summary>
    public GcBuffer Channel2SourceImage => Channel2.RawImage;

    /// <summary>
    /// Processed image of channel 2.
    /// </summary>
    public GcBuffer Channel2ProcessedImage => Channel2.ProcessedImage;

    /// <summary>
    /// List of available bitmap scaling modes (for displaying images in ImageViewer control).
    /// </summary>
    public static List<BitmapScalingMode> BitmapScalingModes => Enum.GetValues<BitmapScalingMode>().Distinct().Skip(1).ToList();

    /// <summary>
    /// Bitmap scaling mode selected.
    /// </summary>
    public BitmapScalingMode SelectedBitmapScalingMode
    {
        get => _bitmapScalingMode;
        set
        {
            if (BitmapScalingModes.Contains(value))
                SetProperty(ref _bitmapScalingMode, value);
            else throw new ArgumentOutOfRangeException($"Bitmap scaling mode must be one of the available ones ({string.Join(", ", BitmapScalingModes)})!");
        }
    }

    /// <summary>
    /// Show pixel value inspector when moving mouse pointer over displayed image.
    /// </summary>
    public bool ShowPixelInspector
    {
        get => _showPixelInspector;
        set => SetProperty(ref _showPixelInspector, value);
    }

    /// <summary>
    /// Limit displayed frame rate. 
    /// If true, frame rate will be limited to <see cref="TargetFPS"/>.
    /// </summary>
    public bool LimitFPS
    {
        get => _limitFPS;
        set
        {
            // Reset timestamp buffer on change.
            if (SetProperty(ref _limitFPS, value))
                _frameStabilizer.Reset();
        }
    }

    /// <summary>
    /// Targeted display frame rate.
    /// </summary>
    public uint TargetFPS
    {
        get => _targetFPS;
        set
        {
            if (AvailableFPS.Contains(value))
            {
                // Reset timestamp buffer on change.
                if (SetProperty(ref _targetFPS, value))
                    _frameStabilizer.Reset();
            }
            else throw new ArgumentOutOfRangeException($"Targeted display frame rate must be one of the available ones ({string.Join(", ", AvailableFPS)})!");
        }
    }

    /// <summary>
    /// Available display frame rates.
    /// </summary>
    public static List<uint> AvailableFPS => [1, 5, 10, 15, 20, 25, 30];

    /// <summary>
    /// Currently displayed frame rate.
    /// </summary>
    public double CurrentFPS => CalcFPS();

    /// <summary>
    /// Show frame info in full screen mode.
    /// </summary>
    public bool ShowFullScreenFrameInfo
    {
        get => _showFullScreenFrameInfo;
        set => SetProperty(ref _showFullScreenFrameInfo, value);
    }

    /// <summary>
    /// Show channel info in full screen mode.
    /// </summary>
    public bool ShowFullScreenChannelInfo
    {
        get => _showFullScreenChannelInfo;
        set => SetProperty(ref _showFullScreenChannelInfo, value);
    }

    /// <summary>
    /// Synchronize image scaling and translation between views.
    /// </summary>
    public bool SynchronizeViews
    {
        get => _synchronizeViews;
        set => SetProperty(ref _synchronizeViews, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a command to change the displayed channel in the UI.
    /// </summary>
    public IRelayCommand<DisplayChannel> ChangeSelectedDisplayChannelCommand { get; }

    /// <summary>
    /// Relays a command to open a full screen window of displayed image stream.
    /// </summary>
    public IRelayCommand OpenFullScreenImageWindowCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates new view model for exposing properties and commands related to the displaying of images.
    /// </summary>
    /// <param name="windowService">Service providing windows and dialogs.</param>
    /// <param name="dispatcherService">Service providing dispatching and running of actions onto the UI thread.</param>
    /// <param name="imageChannels">Available image channels.</param>
    public ImageDisplayViewModel(IMetroWindowService windowService, IDispatcherService dispatcherService, ImageModel[] imageChannels)
    {
        // Retrieve required services.
        _windowService = windowService;
        _dispatcherService = dispatcherService;

        Channel1 = imageChannels[0];
        Channel2 = imageChannels[1];

        // Default display channel.
        SelectedChannel = Channel1;
        SelectedDisplayChannel = DisplayChannel.Channel1;

        // Default bitmap scaling mode.
        SelectedBitmapScalingMode = BitmapScalingMode.Linear;

        // Default pixel inspector mode.
        ShowPixelInspector = true;

        // Default frame rate limitation.
        LimitFPS = false;

        // Default targeted frame rate.
        TargetFPS = 30;

        // Default view synchronization.
        SynchronizeViews = false;

        // Instantiate commands.
        ChangeSelectedDisplayChannelCommand = new RelayCommand<DisplayChannel>(ChangeSelectedDisplayChannel, (ch) => _canChangeSelectedDisplayChannel);
        OpenFullScreenImageWindowCommand = new RelayCommand(OpenFullScreenImageWindow, () => SelectedChannel.ProcessedImage != null);

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Events

    /// <summary>
    /// Eventhandler to events raised in one of the displaychannels that images (both raw and processed) have been updated. 
    /// Handler updates the displayed images if (and only if) the displaychannel is the currently selected one.
    /// </summary>
    private void Channel_ImagesUpdated(object sender, EventArgs eventArgs)
    {
        _dispatcherService.Invoke(() =>
        {
            if (LimitFPS)
            {
                if (_frameStabilizer.IsTimeToDisplay(TargetFPS))
                    UpdateImages();
            }
            else UpdateImages();

            if (sender == SelectedChannel) 
            {
                _timeStamps.Put((ulong)DateTime.Now.Ticks);
                OnPropertyChanged(nameof(CurrentFPS));
            }
        });
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient for messages over the "Busy" channel.
        Messenger.Register<PropertyChangedMessage<bool>, string>(this, "Busy", BusyHandler);

        // Register as recipient of messages requesting currently selected image channel for display.
        Messenger.Register<SelectedChannelRequestMessage>(this, (r, m) => { m.Reply(SelectedChannel); });
    }

    /// <summary>
    /// Handler of <see cref="PropertyChangedMessage{T}"/> messages broadcasted over the "Busy" channel.
    /// </summary>
    private void BusyHandler(object recipient, PropertyChangedMessage<bool> message)
    {
        if (message.PropertyName == nameof(AcquisitionViewModel.IsBusy))
        {
            // Reset timestamp buffer at acquisition stop.
            if (message.NewValue == false)
                _frameStabilizer.Reset();
        }

        if (message.PropertyName == nameof(PlayBackViewModel.IsLoaded))
        {
            // Change active display mode during playback.
            if (message.NewValue == true)
            {
                // Cache used display mode.
                _cachedDisplayChannel = SelectedDisplayChannel;

                // Select display mode based on current channel selection (but avoid side-by-side display mode).
                SelectedDisplayChannel = SelectedChannel.ImageChannel;

                // Disable changing of display mode.
                _canChangeSelectedDisplayChannel = false;
                ChangeSelectedDisplayChannelCommand.NotifyCanExecuteChanged();

                // Disable fps limitations.
                _cachedLimitFPS = LimitFPS;
                LimitFPS = false;
            }
            else 
            {
                // Restore previously used display mode.
                SelectedDisplayChannel = _cachedDisplayChannel;

                // Re-enable changing of display mode.
                _canChangeSelectedDisplayChannel = true;
                ChangeSelectedDisplayChannelCommand.NotifyCanExecuteChanged();

                // Restore fps limitations.
                LimitFPS = _cachedLimitFPS;
            }
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Updates images.
    /// </summary>
    private void UpdateImages()
    {
        // Notify of property changes.
        switch (SelectedDisplayChannel)
        {
            case DisplayChannel.Channel1:
                OnPropertyChanged(nameof(Channel1SourceImage));
                OnPropertyChanged(nameof(Channel1ProcessedImage));
                break;
            case DisplayChannel.Channel2:
                OnPropertyChanged(nameof(Channel2SourceImage));
                OnPropertyChanged(nameof(Channel2ProcessedImage));
                break;
            default:
                OnPropertyChanged(nameof(Channel1SourceImage));
                OnPropertyChanged(nameof(Channel1ProcessedImage));
                OnPropertyChanged(nameof(Channel2SourceImage));
                OnPropertyChanged(nameof(Channel2ProcessedImage));
                break;
        }

        OpenFullScreenImageWindowCommand?.NotifyCanExecuteChanged();

        // Notify of update.
        Messenger.Send(new ImagesUpdatedMessage(SelectedChannel.RawImage, SelectedChannel.ProcessedImage));
    }

    /// <summary>
    /// Changes channel selected for display.
    /// </summary>
    /// <param name="channel">Channel to display.</param>
    private void ChangeSelectedDisplayChannel(DisplayChannel channel)
    {
        SelectedChannel = channel switch
        {
            DisplayChannel.Channel1 => Channel1,
            DisplayChannel.Channel2 => Channel2,
            _ => throw new NotImplementedException()
        };

        SelectedDisplayChannel = channel;

    }

    /// <summary>
    /// Opens a new window for displaying image stream in full screen mode.
    /// </summary>
    private void OpenFullScreenImageWindow()
    {
        _windowService.ShowWindow<FullScreenImageWindowViewModel>();
    }

    /// <summary>
    /// Calculates effective frame rate (Hz) from received timestamps.
    /// </summary>
    /// <returns>Estimated number of frames per second.</returns>
    private double CalcFPS()
    {
        if (_timeStamps.Size > 1)
            return TimeSpan.TicksPerSecond / (_timeStamps.Max() - (double)_timeStamps.Min()) * (_timeStamps.Size - 1);
        else return 0.0;
    }

    #endregion
}