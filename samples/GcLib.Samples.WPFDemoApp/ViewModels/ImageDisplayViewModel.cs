using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ImagerViewer.Models;
using ImagerViewer.Utilities.Messages;
using ImagerViewer.Utilities.Services;
using GcLib;
using GcLib.Utilities.Collections;
using GcLib.Utilities.Threading;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model exposing properties and commands related to the displaying of images.
/// </summary>
internal sealed partial class ImageDisplayViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private BitmapScalingMode _bitmapScalingMode;
    private bool _showPixelInspector;
    private bool _limitFPS;
    private uint _targetFPS;
    private bool _showFullScreenFrameInfo;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Service providing dispatching and running of actions onto the UI thread.
    /// </summary>
    private readonly IDispatcherService _dispatcherService;

    /// <summary>
    /// Image source.
    /// </summary>
    private readonly ImageModel _imageSource;

    /// <summary>
    /// Frame rate manager, used for stabilizing frame rate.
    /// </summary>
    private readonly FPSStabilizer _frameStabilizer = new();

    /// <summary>
    /// Stores fps limit setting for subsequent retrieval.
    /// </summary>
    private bool _cachedLimitFPS;

    /// <summary>
    /// Circular buffer of timestamps used for frame rate calculations.
    /// </summary>
    private readonly CircularBuffer<ulong> _timeStamps = new(10, true);

    #endregion

    #region Properties

    /// <summary>
    /// Raw image.
    /// </summary>
    public GcBuffer SourceImage => _imageSource.RawImage;

    /// <summary>
    /// Processed image.
    /// </summary>
    public GcBuffer ProcessedImage => _imageSource.ProcessedImage;

    /// <summary>
    /// List of available bitmap scaling modes.
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

    #endregion

    #region Commands

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
    /// <param name="imageSource">Image source.</param>
    public ImageDisplayViewModel(IMetroWindowService windowService, IDispatcherService dispatcherService, ImageModel imageSource)
    {
        // Retrieve required services.
        _windowService = windowService;
        _dispatcherService = dispatcherService;

        _imageSource = imageSource;
        _imageSource.ImagesUpdated += Channel_ImagesUpdated;

        // Default bitmap scaling mode.
        SelectedBitmapScalingMode = BitmapScalingMode.Linear;

        // Default pixel inspector mode.
        ShowPixelInspector = true;

        // Default frame rate limitation.
        LimitFPS = false;

        // Default targeted frame rate.
        TargetFPS = 30;

        // Instantiate commands.
        OpenFullScreenImageWindowCommand = new RelayCommand(OpenFullScreenImageWindow, () => _imageSource.ProcessedImage != null);

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Events

    /// <summary>
    /// Eventhandler to <see cref="ImageModel.ImagesUpdated"/> events raised in image source. 
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

            _timeStamps.Put((ulong)DateTime.Now.Ticks);
            OnPropertyChanged(nameof(CurrentFPS));
        });
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient for messages over the "Busy" channel.
        Messenger.Register<PropertyChangedMessage<bool>, string>(this, "Busy", BusyHandler);
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
                // Disable fps limitations.
                _cachedLimitFPS = LimitFPS;
                LimitFPS = false;
            }
            else 
            {
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
        OnPropertyChanged(nameof(SourceImage));
        OnPropertyChanged(nameof(ProcessedImage));

        OpenFullScreenImageWindowCommand?.NotifyCanExecuteChanged();

        // Notify of update.
        Messenger.Send(new ImagesUpdatedMessage(_imageSource.RawImage, _imageSource.ProcessedImage));
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