﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FusionViewer.Models;
using FusionViewer.Utilities.Dialogs;
using FusionViewer.Utilities.Messages;
using FusionViewer.Utilities.Services;
using GcLib;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Serilog.Events;

namespace FusionViewer.ViewModels;

/// <summary>
/// Models a view for handling acquisition and recording of image data from input (device) and output (fusion) channels.
/// </summary>
internal sealed class AcquisitionViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private bool _isEnabled;
    private bool _isBusy;
    private bool _isRecording;
    private bool _autoGenerateFileNames;
    private bool _logTimeStamps;
    private bool _appendTimeStamps;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Service providing dispatching and running of actions onto the UI thread.
    /// </summary>
    private readonly IDispatcherService _dispatcherService;

    /// <summary>
    /// Writes timestamps to a csv file.
    /// </summary>
    private StreamWriter _timeStampWriter;

    /// <summary>
    /// True if an active acquisition is currently being aborted.
    /// </summary>
    private bool _isAborting;

    #endregion

    #region Properties

    /// <summary>
    /// Indicates that view is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        private set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// First input acquisition channel.
    /// </summary>
    public AcquisitionModel AcquisitionChannel { get; }

    /// <summary>
    /// Indicates that filenames will be auto-generated (by current date and time).
    /// </summary>
    public bool AutoGenerateFileNames
    {
        get => _autoGenerateFileNames;
        set => SetProperty(ref _autoGenerateFileNames, value);
    }

    /// <summary>
    /// Indicates that an acquisition (either live view or recording) is currently running.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(field: ref _isBusy, newValue: value, broadcast: true))
                NotifyAcquisitionCommands();
        }
    }

    /// <summary>
    /// Indicates that a recording is currently running.
    /// </summary>
    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetProperty(ref _isRecording, value))
            {
                if (_isRecording)
                {
                    // Register class as recipient for timestamp pairing messages.
                    Messenger.Register<TimeStampPairingMessage>(this, (sender, msg) => 
                    {
                        // Append comma separated timestamps to file in a single terminated line.
                        if (LogTimeStamps && msg.TimeStamp1 != null && msg.TimeStamp2 != null)
                            _timeStampWriter?.WriteLine(string.Join(", ", msg.TimeStamp1, msg.TimeStamp2));
                    });
                }
                else Messenger.Unregister<TimeStampPairingMessage>(this);
            }
        }
    }

    /// <summary>
    /// True if matched timestamps of input channels should be logged to file.
    /// </summary>
    public bool LogTimeStamps
    {
        get => _logTimeStamps;
        set => SetProperty(ref _logTimeStamps, value);
    }

    /// <summary>
    /// Appends logged timestamps to same file.
    /// </summary>
    public bool AppendTimeStamps
    {
        get => _appendTimeStamps;
        set => SetProperty(ref _appendTimeStamps, value);
    }

    // ToDo: Add filepath string for timestamp csv file?

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a view handling image acquisition and recording.
    /// </summary>
    /// <param name="dialogService">Service providing windows and dialogs.</param>
    /// <param name="dispatcherService">Service providing dispatching and running of actions onto the UI thread.</param>
    /// <param name="deviceChannels">Available device channels.</param>
    /// <param name="imageChannel">Available image channels.</param>
    public AcquisitionViewModel(IMetroWindowService dialogService, IDispatcherService dispatcherService, DeviceModel device, ImageModel imageChannel)
    {
        // Get required services.
        _windowService = dialogService;
        _dispatcherService = dispatcherService;

        // Instantiate acquisition channels.
        AcquisitionChannel = new AcquisitionModel(device, imageChannel);

        // Default file paths.
#if DEBUG
        AcquisitionChannel.FilePath = Path.GetFullPath(@"C:\testdata\channel1data.bin");
#else
        AcquisitionChannel.FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"FusionViewer\Recordings\channel1data.bin");
#endif
        // Register eventhandlers to available acquisition channels.
        AcquisitionChannel.AcquisitionStopped += Channel_AcquisitionStopped;
        AcquisitionChannel.FrameDropped += Channel_FrameDropped;
        AcquisitionChannel.AcquisitionAborted += Channel_AcquisitionAborted;
        AcquisitionChannel.RecordingAborted += Channel_RecordingAborted;


        // Register eventhandlers to available input devices.
        device.PropertyChanged += DeviceModel_PropertyChanged;

        // Register eventhandlers to available images.
        imageChannel.ProcessingException += ImageModel_ProcessingException;

        // Instantiate commands.
        PlayCommand = new AsyncRelayCommand(PlayAsync, CanAcquire);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsBusy);
        RecordCommand = new AsyncRelayCommand(RecordAsync, CanAcquire);

        LogTimeStamps = false;

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a play command from the UI, for starting an acquisition.
    /// </summary>
    public IAsyncRelayCommand PlayCommand { get; }

    /// <summary>
    /// Relays a stop command from the UI, for stopping an acquisition or recording.
    /// </summary>
    public IAsyncRelayCommand StopCommand { get; }

    /// <summary>
    /// Relays a record command from the UI, for starting a recording.
    /// </summary>
    public IAsyncRelayCommand RecordCommand { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Start acquisition on available channels.
    /// </summary>
    public async Task PlayAsync()
    {
        Log.Information("Live view started");

        // TODO : Synchronize thread starts using ManuelResetEventSlim?
        try
        {
            // Use Parallel library? Task.WhenAll?
            await AcquisitionChannel.StartAcquisitionAsync();

            // Synchronize grabbing from channels.
            AcquisitionChannel.StartGrabbing();

            IsBusy = true;
        }
        catch (Exception ex)
        {
            // Stop acquisitions.
            await StopAsync();

            // Show dialog to user.
            _ = _windowService.ShowMessageDialog(this, "Acquisition error!", ex.Message, MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);
        }
    }

    /// <summary>
    /// Start recording on available channels.
    /// </summary>
    public async Task RecordAsync()
    {
        Log.Information("Recording started");

        // Show warning dialog to user that files will be overwritten (with yes or no choices).
        if (AutoGenerateFileNames == false && File.Exists(AcquisitionChannel.FilePath))
        {
            var result = _windowService.ShowMessageDialog(this, "Warning!", "File already exists! Overwrite?", MessageDialogStyle.AffirmativeAndNegative, MetroDialogHelper.DefaultSettings);
            if (result == MessageDialogResult.Negative)
            {
                Log.Information("Recording cancelled");

                return;
            }
        }

        IsRecording = true;

        // Generate common substring based on current time and date.
        string dateTimeSubString = $"_{DateTime.Now:yyyyMMddHHmmssfff}";

        try
        {
            // Save input channel timestamps to file (matching file path and substring).
            if (LogTimeStamps && AcquisitionChannel.IsEnabled)
            {
                // Create directory if it doesn't already exist.
                _ = Directory.CreateDirectory(Path.GetDirectoryName(AcquisitionChannel.FilePath));

                if (AppendTimeStamps)
                {
                    // Log to same file (and append).
                    _timeStampWriter = new(Path.GetDirectoryName(AcquisitionChannel.FilePath) + Path.DirectorySeparatorChar + "timestamps" + ".csv", append: true);
                }
                else
                {
                    // Log to separate files.
                    _timeStampWriter = new(Path.GetDirectoryName(AcquisitionChannel.FilePath) + Path.DirectorySeparatorChar + "timestamps" + (AutoGenerateFileNames ? dateTimeSubString : string.Empty) + ".csv", append: false);
                }
                
            }

            // Start input channel recordings (try/catch? Synchronize start using ManuelResetEventSlim?).
            await AcquisitionChannel.StartRecordingAsync(AutoGenerateFileNames ? dateTimeSubString : string.Empty);

            // Start grabbing here?
            AcquisitionChannel.StartGrabbing();

            IsBusy = true;
        }
        catch (Exception ex)
        {
            // Stop acquisitions.
            await StopAsync();

            // Show dialog to user.
            _ = _windowService.ShowMessageDialog(this, "Acquisition error!", "Failed to start acquisition: " + ex.Message, MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);
        }

    }

    /// <summary>
    /// Stop acquisition or recording on available channels.
    /// </summary>
    public async Task StopAsync()
    {
        Log.Information(IsRecording ? "Recording stopped" : "Live view stopped");

        try
        {
            // Stop acquisition on all channels.
            if (AcquisitionChannel.IsAcquiring)
                await AcquisitionChannel.StopAcquisitionAsync();

            // Close timestamp writer.
            if (IsRecording && LogTimeStamps)
                _timeStampWriter?.Close();

            // Update UI state.
            IsBusy = false;
            IsRecording = false;
        }
        catch (Exception ex)
        {
            // Show dialog to user.
            _ = _windowService.ShowMessageDialog(this, "Acquisition error!", "Failed to stop acquisition: " + ex.Message, MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);
        }
    }

    #endregion

    #region Private/protected methods

    /// <summary>
    /// Discover if acquisition/recording is currently possible.
    /// </summary>
    /// <returns>True if acquisition/recording is possible.</returns>
    private bool CanAcquire()
    {
        // Disable new acquisition if an existing one is actively running.
        if (IsBusy)
            return false;

        // Disable while connecting to devices.
        if (AcquisitionChannel.DeviceModel.IsConnecting)
            return false;

        // Request status from PlayBackViewModel.
        var message = Messenger.Send<PlayBackStatusRequestMessage>();
        if (message.HasReceivedResponse && message.Response)
            return !message.Response;

        // Enable if at least one device is connected.
        return AcquisitionChannel.DeviceModel.IsConnected;
    }

    /// <summary>
    /// Notify acquisition commands of changes to CanExecute properties.
    /// </summary>
    private void NotifyAcquisitionCommands()
    {
        PlayCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RecordCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Events

    /// <summary>
    /// Eventhandler to <see cref="AcquisitionModel.AcquisitionAborted"/> events, raised in one of the acquisition channels.
    /// </summary>
    private async void Channel_AcquisitionAborted(object sender, AcquisitionAbortedEventArgs eventArgs)
    {
        // Stop acquisition on all channels.
        await _dispatcherService.Invoke(StopAsync);

        // Show error message to user.
        _dispatcherService.Invoke(() =>
        {
            _ = _windowService.ShowMessageDialog(this, "Acquisition error!", eventArgs.ErrorMessage, MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);
        });

        Log.Error(eventArgs.Exception, eventArgs.ErrorMessage);
    }

    /// <summary>
    /// Eventhandler to <see cref="AcquisitionModel.AcquisitionStopped"/> events, raised in one of the acquisition channels.
    /// </summary>
    private async void Channel_AcquisitionStopped(object sender, EventArgs e)
    {
        // Stop acquisition on all channels.
        await _dispatcherService.Invoke(StopAsync);
    }

    /// <summary>
    /// Eventhandler to <see cref="AcquisitionModel.FrameDropped"/> events, raised in one of the acquisition channels.
    /// </summary>
    private void Channel_FrameDropped(object sender, FrameDroppedEventArgs frameDroppedEventArgs)
    {
        var acquisitionModel = (AcquisitionModel)sender;

        // ToDo: Log warning message instead? Too verbose?
        // Send message to status bar? Remove?
        _ = Messenger.Send(new StatusBarLogMessage($"Frames have been lost (Total number: {frameDroppedEventArgs.LostFrameCount}).", LogEventLevel.Warning));
    }

    /// <summary>
    /// Eventhandler to <see cref="AcquisitionModel.RecordingAborted"/> events.
    /// </summary>
    private async void Channel_RecordingAborted(object sender, WritingAbortedEventArgs eventArgs)
    {
        // Adds thread safety (avoids multiple thread calls).
        if (_isAborting == false)
        {
            _isAborting = true;

            if (IsRecording)
                await _dispatcherService.Invoke(StopAsync);

            // Show error message to user.
            _windowService.ShowMessageDialog(this, "Recording error!", eventArgs.ErrorMessage, MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

            _isAborting = false;

            Log.Error(eventArgs.Exception, eventArgs.ErrorMessage);
        }
    }

    /// <summary>
    /// Eventhandler to PropertyChanged events in a device.
    /// </summary>
    private void DeviceModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceModel.IsConnected))
        {
            // Enable view (acquisition buttons) if at least one device is connected.
            IsEnabled = AcquisitionChannel.DeviceModel.IsConnected;
            NotifyAcquisitionCommands();
        }
        if (e.PropertyName == nameof(DeviceModel.IsConnecting))
        {
            // Disable view (acquisition buttons) while connecting to a device.
            NotifyAcquisitionCommands();
        }
    }

    /// <summary>
    /// Eventhandler to <see cref="ImageModel.ProcessingException"/> events raised in an image channel.
    /// </summary>
    /// <param name="sender">ImageModel.</param>
    /// <param name="ex">Exception thrown.</param>
    private void ImageModel_ProcessingException(object sender, Exception ex)
    {
        // Adds thread safety (avoids multiple thread calls).
        if (_isAborting == false)
        {
            _isAborting = true;

            Log.Error(ex, "Exception thrown while processing image");

            _dispatcherService.Invoke(() =>
            {
                // Stop acquisition.
                StopCommand.Execute(null);

                // Show error message.
                _ = _windowService.ShowMessageDialog(this, "Image Processing Error!", $"Exception thrown while processing image: {ex.Message}", MessageDialogStyle.Affirmative, MetroDialogHelper.MessageDialogSettings);
            });

            _isAborting = false;
        }
    }

    /// <summary>
    /// Eventhandler to <see cref="Window.Closing"/> events.
    /// </summary>
    public void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Show dialog if acquisition is currently running.
        if (IsBusy)
        {
            MessageDialogResult result = _windowService.ShowMessageDialog(this, "Exit application?", "An acquisition is actively running, close application anyway?", MessageDialogStyle.AffirmativeAndNegative, MetroDialogHelper.DefaultSettings);
            if (result == MessageDialogResult.Affirmative)
            {
                StopCommand.Execute(null);
            }
            else
            {
                e.Cancel = true;
                return;
            }
        }
    }

    protected override void Broadcast<T>(T oldValue, T newValue, string propertyName)
    {
        if (propertyName == nameof(IsBusy))
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

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient for messages over the "Busy" channel.
        Messenger.Register<PropertyChangedMessage<bool>, string>(this, "Busy", BusyHandler);

        // Register as recipient for messages requesting active acquisitions to stop.
        Messenger.Register<StopAcquisitionAsyncRequestMessage>(this, (r, m) => 
        {
            // Stop acquisition on all channels.
            if (IsBusy)
                m.Reply(StopAsync());
            else m.Reply(Task.CompletedTask);
        });
    }

    /// <summary>
    /// Handler of <see cref="PropertyChangedMessage{T}"/> messages broadcasted over the "Busy" channel.
    /// </summary>
    private void BusyHandler(object recipient, PropertyChangedMessage<bool> message)
    {
        // Disable acquisition buttons in playback mode.
        if (message.PropertyName == nameof(PlayBackViewModel.IsLoaded) || message.PropertyName == nameof(PlayBackViewModel.IsLoading))
        {
            IsEnabled = message.NewValue == false;
            NotifyAcquisitionCommands();
        }
    }

    #endregion
}