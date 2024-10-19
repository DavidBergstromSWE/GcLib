using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ImagerViewerApp.Models;
using ImagerViewerApp.Utilities.Dialogs;
using ImagerViewerApp.Utilities.Messages;
using ImagerViewerApp.Utilities.Services;
using GcLib;
using MahApps.Metro.Controls.Dialogs;
using Serilog;

namespace ImagerViewerApp.ViewModels;

/// <summary>
/// Models a view for handling playback of previously recorded image data.
/// </summary>
internal sealed class PlayBackViewModel : ObservableRecipient, IDisposable
{
    #region Fields

    // backing-fields
    private bool _isLoading;
    private bool _isLoaded;
    private bool _isPlaying;
    private bool _isEnabled;
    private int _firstFrame;
    private int _lastFrame;
    private int _smallFrameChange;
    private int _largeFrameChange;
    private int _currentFrame;
    private string _frameInfo;

    /// <summary>
    /// Indicates if a sequence can be loaded.
    /// </summary>
    private bool _canOpenSequence = true;

    /// <summary>
    /// Timer object used for continuous playback.
    /// </summary>
    private System.Timers.Timer _playbackTimer;

    /// <summary>
    /// Reader of images from file.
    /// </summary>
    private GcBufferReader _imageReader;

    /// <summary>
    /// Timestamp of first frame in opened image sequence.
    /// </summary>
    private ulong _time0;

    /// <summary>
    /// Stores buffer displayed before loading a new sequence.
    /// </summary>
    private GcBuffer _cachedBuffer;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Image channel for playback.
    /// </summary>
    private ImageModel _playbackChannel;

    /// <summary>
    /// Stores processing settings of playback channel.
    /// </summary>
    private MemoryStream _playbackChannelSettings;

    /// <summary>
    /// Signals to a <see cref="CancellationToken"/> that it should be canceled.
    /// </summary>
    private CancellationTokenSource _cts;

    #endregion

    #region Properties

    /// <summary>
    /// True if currently in continuous play mode.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                // Start playback.
                if (IsPlaying) 
                {
                    // Register to playback timer event.
                    _playbackTimer.Elapsed += OnTimerElapsed;

                    // Start playback timer.
                    _playbackTimer.Start();

                    Log.Information("Playback started");
                }
                // Stop playback.
                else
                {
                    // Unregister from playback timer event.
                    _playbackTimer.Elapsed -= OnTimerElapsed;

                    // Stop playback timer.
                    _playbackTimer.Stop();

                   Log.Information("Playback stopped");
                }
            }
        }
    }

    /// <summary>
    /// Index of first frame in sequence.
    /// </summary>
    public int FirstFrame
    {
        get => _firstFrame;
        private set => SetProperty(ref _firstFrame, value);
    }

    /// <summary>
    /// Index of last frame in sequence.
    /// </summary>
    public int LastFrame
    {
        get => _lastFrame;
        private set => SetProperty(ref _lastFrame, value);
    }

    /// <summary>
    /// Current frame index in sequence.
    /// </summary>
    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            _ = SetProperty(ref _currentFrame, value);
            OnCurrentFrameUpdated();
        }
    }

    /// <summary>
    /// Number of frames representing a small change of the current frame index.
    /// </summary>
    public int SmallFrameChange
    {
        get => _smallFrameChange;
        set => SetProperty(ref _smallFrameChange, value);
    }

    /// <summary>
    /// Number of frames representing a large change of the current frame index.
    /// </summary>
    public int LargeFrameChange
    {
        get => _largeFrameChange;
        set => SetProperty(ref _largeFrameChange, value);
    }

    /// <summary>
    /// Text string containing frame index and time elapsed info.
    /// </summary>
    public string FrameInfo
    {
        get => _frameInfo;
        private set => SetProperty(ref _frameInfo, value);
    }

    /// <summary>
    /// True if a sequence is currently being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(field: ref _isLoading, newValue: value, broadcast: true);
    }

    /// <summary>
    /// Indicates that image sequence has been succesfully loaded.
    /// </summary>
    public bool IsLoaded
    {
        get => _isLoaded;
        private set
        {
            if (SetProperty(field: ref _isLoaded, newValue: value, broadcast: true))
            {
                CloseSequenceCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// True if playback buttons are enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        private set => SetProperty(field: ref _isEnabled, newValue: value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays command to open a previously recorded image sequence for playback.
    /// </summary>
    public IAsyncRelayCommand OpenSequenceCommand { get; }

    /// <summary>
    /// Relays command to close an opened image sequence and exit playback mode.
    /// </summary>
    public IRelayCommand CloseSequenceCommand { get; }

    /// <summary>
    /// Relays command to rewind the image sequence to the start position.
    /// </summary>
    public IRelayCommand GoToStartCommand { get; }

    /// <summary>
    /// Relays command to jump to the end position of the image sequence.
    /// </summary>
    public IRelayCommand GoToEndCommand { get; }

    /// <summary>
    /// Relays command to step one position back in the image sequence.
    /// </summary>
    public IRelayCommand StepBackCommand { get; }

    /// <summary>
    /// Relays command to step a <see cref="LargeFrameChange"/> position back in the image sequence.
    /// </summary>
    public IRelayCommand StepLargeBackCommand { get; }

    /// <summary>
    /// Relays command to step one position forward in the image sequence.
    /// </summary>
    public IRelayCommand StepForwardCommand { get; }

    /// <summary>
    /// Relays command to step a <see cref="LargeFrameChange"/> position forward in the image sequence.
    /// </summary>
    public IRelayCommand StepLargeForwardCommand { get; }

    /// <summary>
    /// Relays command to start or pause a continuous playback of the image sequence.
    /// </summary>
    public IRelayCommand PlayPauseCommand { get; }
    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a view handling playback of previously recorded image data.
    /// </summary>
    public PlayBackViewModel(IMetroWindowService dialogService, ImageModel imageModel)
    {
        // Get required services.
        _windowService = dialogService;
        _playbackChannel = imageModel;

        // Instantiate commands.
        OpenSequenceCommand = new AsyncRelayCommand(OpenSequenceAsync, () => _canOpenSequence);
        CloseSequenceCommand = new RelayCommand(CloseSequence, () => IsLoaded);
        GoToStartCommand = new RelayCommand(GoToStart, () => IsEnabled);
        GoToEndCommand = new RelayCommand(GoToEnd, () => IsEnabled);
        StepBackCommand = new RelayCommand(StepBack, () => IsEnabled);
        StepLargeBackCommand = new RelayCommand(StepLargeBack, () => IsEnabled);
        StepForwardCommand = new RelayCommand(StepForward, () => IsEnabled);
        StepLargeForwardCommand = new RelayCommand(StepLargeForward, () => IsEnabled);
        PlayPauseCommand = new RelayCommand(PlayPauseToggle, () => IsEnabled);

        // Set default property values.
        SmallFrameChange = 1;
        LargeFrameChange = 10;

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Event-handling method to <see cref="DragDrop.DropEvent"/> events.
    /// </summary>
    public async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Note that you can drag more than one file...
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // ...but only single files are accepted.
            if (files.Length != 1)
                return;

            // Only recordings are handled here (configuration files are handled in DeviceViewModel).
            if (Path.GetExtension(files[0]) == ".bin")
            {
                // Close any previously opened sequences.
                if (IsLoaded)
                    CloseSequence();

                // Cache currently displayed image into memory.
                _cachedBuffer = _playbackChannel.RawImage;

                // Reset processing settings for playback channel.
                ResetPlaybackChannel();

                // Load recorded image sequence from dropped file.
                await LoadSequenceFromFileAsync(files[0]);
            }                
        }
    }

    /// <summary>
    /// Eventhandler to <see cref="Window.Closing"/> events.
    /// </summary>
    public void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Close opened sequence (and stop ongoing playback).
        if (e.Cancel == false && IsLoaded)
            CloseSequence();
    }

    #endregion

    #region Private/protected methods

    /// <summary>
    /// Open file dialog and load selected image sequence.
    /// </summary>
    private async Task OpenSequenceAsync()
    {
        // Retrieve filepath from dialog.
        string filePath = _windowService.ShowOpenFileDialog(title: "Select image sequence file", multiSelect: false, filter: "Image sequence files (*.bin)|*.bin", defaultExtension: "bin");
        if (string.IsNullOrEmpty(filePath))
            return;

        // Close any previously opened sequences.
        if (IsLoaded)
            CloseSequence();

        // Cache currently displayed image into memory.
        _cachedBuffer = _playbackChannel.RawImage;

        // Reset processing settings for playback channel.
        ResetPlaybackChannel();

        // Load recorded image sequence from path.
        await LoadSequenceFromFileAsync(filePath);
    }

    /// <summary>
    /// Load recorded sequence from file.
    /// </summary>
    /// <param name="filePath">Filepath to recorded sequence.</param>
    /// <exception cref="FileLoadException"/>
    private async Task LoadSequenceFromFileAsync(string filePath)
    {
        // Instantiate new cancellation token.
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        // Enable progress report.
        var progress = new Progress<double>();

        try
        {
            // Start task of opening a delayed progress dialog.
            var task = Task.Run(() => _windowService.ShowProgressDialogAsync(viewModel: this,
                                                                             title: "Loading sequence...Please wait...",
                                                                             message: $"Loading sequence from \'{filePath}\'...",
                                                                             isCancelable: true,
                                                                             settings: MetroDialogHelper.GetProgressDialogSettings(token),
                                                                             progress: progress,
                                                                             tokenSource: _cts,
                                                                             delay: 1000));

            // Open image sequence.
            IsLoading = true;
            _imageReader = new GcBufferReader(filePath);
            await _imageReader.OpenAsync(progress, token);
            IsLoading = false;

            // Cancel dialog.
            _cts.Cancel();

            // Await task of opening dialog.
            await task;

            // Read timestamp of first image.
            _time0 = _imageReader.GetTimeStamp(0);

            // Generate frame indices.
            var frameIndices = Enumerable.Range(1, (int)_imageReader.FrameCount).ToList();

            // Reset UI controls.
            FirstFrame = frameIndices.First();
            LastFrame = frameIndices.Last();
            CurrentFrame = FirstFrame;

            // Enable/disable playback buttons depending on number of images in sequence.
            if (frameIndices.Count > 1)
            {
                // Initialize playback timer and set interval (ms) corresponding to framerate (Hz) of opened image sequence (use 10 Hz as fallback).
                _playbackTimer = new System.Timers.Timer { AutoReset = true, Interval = _imageReader.FrameRate > 0 ? 1 / _imageReader.FrameRate * 1000 : 100 };

                // Enable playback buttons.
                IsEnabled = true;
            }
            else
            {
                // Disable playback buttons.
                IsEnabled = false;
            }

            // Sequence is now loaded and ready to read from.
            IsLoaded = true;

            // Log information.
            Log.Information("Image sequence opened from {FileName} ({FrameCount} images)", filePath, _imageReader.FrameCount);
        }
        catch (OperationCanceledException) // User canceled operation
        {
            // Log error.
            Log.Information("Opening of image sequence '{filePath}' was cancelled", filePath);

            // Restore settings to playback channel.
            RestorePlaybackChannel();

            // Release unmanaged resources.
            Dispose();

            IsLoading = false;
            IsEnabled = false;
        }
        catch (Exception ex) // Failed to load file
        {
            // Cancel progress dialog.
            _cts?.Cancel();

            // Log error.
            Log.Error(ex, "Image sequence {FileName} failed to open", filePath);

            // Show error dialogue.
            _ = _windowService.ShowMessageDialog(this, "File error!", $"Unable to load image sequence in '{filePath}'! {ex.Message}", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

            // Restore settings to playback channel.
            RestorePlaybackChannel();

            // Release unmanaged resources.
            Dispose();

            IsLoading = false;
            IsEnabled = false;
        }
    }

    /// <summary>
    /// Closes currently opened image sequence.
    /// </summary>
    private void CloseSequence()
    {
        if (IsLoaded == false)
            return;

        // Log information.
        Log.Information("Image sequence {FileName} closed", _imageReader.FilePath);

        // Restore settings to playback channel.
        RestorePlaybackChannel();

        // Release unmanaged resources.
        Dispose();

        // Disable playback mode.
        IsPlaying = false;
        IsLoaded = false;
        IsEnabled = false;
    }

    /// <summary>
    /// Reset settings for playback channel.
    /// </summary>
    private void ResetPlaybackChannel()
    {
        // Cache display channel settings.
        _playbackChannelSettings = new MemoryStream();
        using XmlWriter xmlWriter = XmlWriter.Create(_playbackChannelSettings, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Fragment });
        _playbackChannel.WriteXml(xmlWriter);

        // Reset display channel settings.
        _playbackChannel.InitializeSettings();
    }

    /// <summary>
    /// Restore settings to playback channel.
    /// </summary>
    private void RestorePlaybackChannel()
    {
        // Restore display channel settings.
        _playbackChannelSettings.Seek(0, SeekOrigin.Begin);
        using XmlReader xmlReader = XmlReader.Create(_playbackChannelSettings, new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment });
        xmlReader.Read();
        _playbackChannel.ReadXml(xmlReader);

        // Restore cached buffer in display channel.
        _playbackChannel.RawImage = _cachedBuffer;
    }

    /// <summary>
    /// Go to first image in sequence.
    /// </summary>
    private void GoToStart()
    {
        CurrentFrame = FirstFrame;
    }

    /// <summary>
    /// Go to final image in sequence.
    /// </summary>
    private void GoToEnd()
    {
        CurrentFrame = LastFrame;
    }

    /// <summary>
    /// Go single step back in image sequence.
    /// </summary>
    private void StepBack()
    {
        if (CurrentFrame > FirstFrame)
            CurrentFrame -= 1;
    }

    /// <summary>
    /// Go a larger step back in image sequence.
    /// </summary>
    private void StepLargeBack()
    {
        if (CurrentFrame - LargeFrameChange > FirstFrame)
            CurrentFrame -= LargeFrameChange;
        else
            CurrentFrame = FirstFrame;
    }

    /// <summary>
    /// Go single step forward in image sequence.
    /// </summary>
    private void StepForward()
    {
        if (CurrentFrame < LastFrame)
            CurrentFrame += 1;
    }

    /// <summary>
    /// Go a larger step forward in image sequence.
    /// </summary>
    private void StepLargeForward()
    {
        if (CurrentFrame + LargeFrameChange < LastFrame)
            CurrentFrame += LargeFrameChange;
        else
            CurrentFrame = LastFrame;
    }

    /// <summary>
    /// Toggles playing and pausing.
    /// </summary>
    private void PlayPauseToggle()
    {
        IsPlaying = !IsPlaying;
    }

    /// <summary>
    /// Eventhandler to updates of current frame index, reading corresponding image from sequence and displaying frame and frame info.
    /// </summary>
    private void OnCurrentFrameUpdated()
    {
        // Only update if within range.
        if (CurrentFrame >= FirstFrame && CurrentFrame <= LastFrame && _imageReader != null)
        {
            // Read image from file (using zero-based index).
            if (_imageReader.ReadImage(out GcBuffer buffer, (ulong)CurrentFrame - 1))
            {
                // Update displayed image.
                _playbackChannel.RawImage = buffer;

                // Update frame info.
                FrameInfo = $"Frame {(ulong)CurrentFrame}/{LastFrame} ({new DateTime((long)(buffer.TimeStamp - _time0)):HH:mm:ss.fff})";
            }
        }
    }

    /// <summary>
    /// Eventhandler to <see cref="Timer.Elapsed"/> events, advancing current frame index or stopping if final frame has been reached.
    /// </summary>
    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (IsLoaded)
        {
            // Step forward until last frame.
            if (CurrentFrame == LastFrame)
            {
                IsPlaying = false;

                // Rewind sequence.
                CurrentFrame = FirstFrame;
            }
            else
                CurrentFrame++;
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient for messages requesting playback status.
        Messenger.Register<PlayBackStatusRequestMessage>(this, (r, m) => m.Reply(IsLoaded || IsLoading));

        // Register as recipient for messages over the "Busy" channel.
        Messenger.Register<PropertyChangedMessage<bool>, string>(this, "Busy", BusyHandler);
    }

    /// <summary>
    /// Handler of <see cref="PropertyChangedMessage{T}"/> messages broadcasted over the "Busy" channel.
    /// </summary>
    private void BusyHandler(object recipient, PropertyChangedMessage<bool> message)
    {
        // Disable loading of image sequences during active acquisitions (enable when stopped).
        if (message.PropertyName == nameof(AcquisitionViewModel.IsBusy))
        {
            _canOpenSequence = message.NewValue == false;
            OpenSequenceCommand?.NotifyCanExecuteChanged();
        }
    }

    protected override void Broadcast<T>(T oldValue, T newValue, string propertyName)
    {
        if (propertyName == nameof(IsLoading) || propertyName == nameof(IsLoaded))
        {
            // Broadcast over "Busy" channel.
            Messenger.Send(message: new PropertyChangedMessage<T>(this, propertyName, oldValue, newValue), token: "Busy");
        }
        else
        {
            // Broadcast over default channel.
            base.Broadcast(oldValue, newValue, propertyName);
        }

    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Dipose unmanaged resources.
    /// </summary>
    /// <param name="disposing"></param>
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // dispose managed state (managed objects)
        }

        // Free unmanaged resources (unmanaged objects) and override finalizer.
        _imageReader?.Dispose();
        _playbackTimer?.Dispose();
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~PlayBackViewModel()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}