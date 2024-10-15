using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FusionViewer.Models;
using FusionViewer.Utilities.Dialogs;
using FusionViewer.Utilities.IO;
using FusionViewer.Utilities.Messages;
using FusionViewer.Utilities.Services;
using GcLib;
using MahApps.Metro.Controls.Dialogs;
using Serilog;

namespace FusionViewer.ViewModels;

/// <summary>
/// Models a view for connecting/disconnecting to devices and loading/saving device settings. 
/// </summary>
internal sealed class DeviceViewModel : ObservableRecipient
{
    #region Fields

    // Backing-fields.
    private bool _canLoadConfiguration;
    private bool _canSaveConfiguration;
    private DeviceModel _selectedDevice;
    private bool _isEnabled;
    private uint _deviceParameterUpdateTimeDelay;
    private string _configurationFilePath;

    /// <summary>
    /// Service providing devices.
    /// </summary>
    private readonly IDeviceProvider _deviceProvider;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Service providing dispatching and running of actions onto the UI thread.
    /// </summary>
    private readonly IDispatcherService _dispatcherService;

    /// <summary>
    /// Service managing loading/saving of fusion system configurations.
    /// </summary>
    private readonly IConfigurationService _fusionConfigurationService;

    /// <summary>
    /// Signals to a <see cref="CancellationToken"/> that it should be canceled.
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource;

    #endregion

    #region Properties

    /// <summary>
    /// Flag indicating if view is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                // Notify commands of property changes.
                ConnectCameraFromDialogCommand?.NotifyCanExecuteChanged();
                OpenParameterDialogWindowCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Currently selected device channel.
    /// </summary>
    public DeviceModel SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                OpenParameterDialogWindowCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Time delay before updating device parameter after changing value (in milliseconds).
    /// </summary>
    public uint DeviceParameterUpdateTimeDelay
    {
        get => _deviceParameterUpdateTimeDelay;
        set => SetProperty(ref _deviceParameterUpdateTimeDelay, value);
    }


    /// <summary>
    /// User visibility level.
    /// </summary>
    public Visibility UserVisibility { get; set; } = Visibility.Beginner;

    /// <summary>
    /// Currently used file path for configurations.
    /// </summary>
    public string ConfigurationFilePath
    {
        get => _configurationFilePath; 
        set => SetProperty(ref _configurationFilePath, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a request invoked by a UI command to open a view for editing parameters of selected device.
    /// </summary>
    public IRelayCommand OpenParameterDialogWindowCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to load a fusion system configuration from file selected in a dialog window.
    /// </summary>
    public IAsyncRelayCommand LoadConfigurationDialogCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to save a fusion system configuration to file selected in a dialog window.
    /// </summary>
    public IAsyncRelayCommand<bool> SaveConfigurationDialogCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to connect to a device selected from a dialog window.
    /// </summary>
    public IAsyncRelayCommand<DeviceModel> ConnectCameraFromDialogCommand { get; }

    #endregion

    #region Command signals

    /// <summary>
    /// Signals a command that a configuration can be loaded.
    /// </summary>
    private bool CanLoadConfiguration
    {
        get => _canLoadConfiguration;
        set
        {
            _canLoadConfiguration = value;

            // Notify command of property changes.
            LoadConfigurationDialogCommand?.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Signals a command that a configuration can be saved.
    /// </summary>
    private bool CanSaveConfiguration
    {
        get => _canSaveConfiguration;
        set
        {
            _canSaveConfiguration = value;

            // Notify command of property changes.
            SaveConfigurationDialogCommand?.NotifyCanExecuteChanged();
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new view model providing commands for connection/disconnection of devices and exposing device information. 
    /// </summary>
    /// <param name="windowService">Service providing windows and dialogs.</param>
    /// <param name="dispatcherService">Service providing dispatching and running of actions onto the UI thread.</param>
    /// <param name="deviceProvider">Service providing devices.</param>
    /// <param name="fusionConfigurationService">Service managing loading/saving of fusion system configurations.</param>
    /// <param name="deviceChannels">Available device channels.</param>
    public DeviceViewModel(IMetroWindowService windowService, IDispatcherService dispatcherService, IDeviceProvider deviceProvider, IConfigurationService fusionConfigurationService, DeviceModel device)
    {
        // Get required services.
        _windowService = windowService;
        _dispatcherService = dispatcherService;
        _deviceProvider = deviceProvider;
        _fusionConfigurationService = fusionConfigurationService;
            
        // Set default device to be selected at start-up.
        SelectedDevice = device;
        SelectedDevice.ConnectionLost += DeviceModel_ConnectionLost;
        SelectedDevice.PropertyChanged += DeviceModel_PropertyChanged;

        // Default settings at startup.
        CanLoadConfiguration = true;
        CanSaveConfiguration = false;
        IsEnabled = true;
        DeviceParameterUpdateTimeDelay = 500;

        // Instantiate commands.
        OpenParameterDialogWindowCommand = new RelayCommand(() => OpenParameterDialogWindow(SelectedDevice), () => IsEnabled && SelectedDevice.IsConnecting == false && SelectedDevice.IsConnected);
        LoadConfigurationDialogCommand = new AsyncRelayCommand(LoadConfigurationDialogAsync, () => CanLoadConfiguration);
        SaveConfigurationDialogCommand = new AsyncRelayCommand<bool>(SaveConfigurationDialogAsync, b => CanSaveConfiguration);
        ConnectCameraFromDialogCommand = new AsyncRelayCommand<DeviceModel>(ConnectCameraFromDialogAsync, (d) => IsEnabled);

        // Activate viewmodel for message sending/receiving.
        IsActive = true;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Loads a fusion configuration from file, with the option of cancelling the operation.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    /// <param name="token">Token for cancellation.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="FileLoadException"/>
    private async Task LoadConfigurationFromFileAsync(string filePath, CancellationToken token)
    {
        // Show loading progress dialog.
        IProgressController controller = await _windowService.ShowProgressDialogAsync(viewModel: this,
                                                                                      title: "Loading configuration. Please wait...",
                                                                                      message: $"Loading configuration from \'{filePath}\'...",
                                                                                      isCancelable: true,
                                                                                      settings: MetroDialogHelper.GetProgressDialogSettings(token));
        if (controller != null)
        {
            controller.SetIndeterminate();
            controller.Canceled += (s, e) => { _cancellationTokenSource.Cancel(); };
        }
        
        Log.Debug("Loading configuration from {fileName}...", filePath);

        try
        {
            await _fusionConfigurationService.RestoreAsync(filePath, token);
        }
        catch (InvalidOperationException ex)
        {
            // Device not available, rethrow.
            throw new FileLoadException(ex.Message);
        }
        catch (Exception)
        {
            // Disconnect devices.
            if (SelectedDevice.IsConnected)
                await SelectedDevice.DisconnectDeviceAsync();


            // Rethrow.
            throw;
        }
        finally
        {
            // Close progress dialogue.
            if (controller != null)
            {
                await controller.CloseAsync();
                controller.Canceled -= (s, e) => { _cancellationTokenSource.Cancel(); };
            }
        }

        Log.Information("Configuration successfully loaded from {fileName}", filePath);

        // Show configuration completed dialog.
        _ = _windowService.ShowMessageDialog(this, "Configuration loaded!", $"Configuration loaded from \'{filePath}\'.", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

        ConfigurationFilePath = filePath;

        // Update current working directory.
        Directory.SetCurrentDirectory(Directory.GetParent(filePath).FullName);
    }

    /// <summary>
    /// Save device configuration to file.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    private async Task SaveConfigurationToFileAsync(string filePath, bool useExisting = false)
    {
        Log.Debug("Saving configuration from {fileName}...", filePath);

        IProgressController controller = null;

        if (useExisting == false)
        {
            // Instantiate configuration saving progress dialogue.
            controller = await _windowService.ShowProgressDialogAsync(viewModel: this,
                                                                      title: "Saving configuration. Please wait...",
                                                                      message: $"Saving configuration to \'{filePath}\'.",
                                                                      isCancelable: false,
                                                                      settings: MetroDialogHelper.DefaultSettings);
        }

        controller?.SetIndeterminate();

        try
        {
            // Make sure devices are updated before saving.
            if (SelectedDevice.IsConnected)
                await SelectedDevice.UpdateDeviceAsync();

            // Store configuration to file.
            await _fusionConfigurationService.StoreAsync(filePath);

            // Build dialog message.
            string dialogMessage = $"Settings successfully saved to \'{filePath}\' .";

            // Close progress dialogue.
            if (controller != null)
                await controller.CloseAsync();

            // Show message dialog.
            if (useExisting == false)
                _ = _windowService.ShowMessageDialog(this, "Configuration saved!", dialogMessage, MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

            Log.Information("Configuration successfully saved to {fileName}", filePath);

            ConfigurationFilePath = filePath;

            // Update current working directory.
            Directory.SetCurrentDirectory(Directory.GetParent(filePath).FullName);
        }
        catch (Exception ex)
        {
            // Close progress dialogue.
            if (controller != null)
                await controller.CloseAsync();

            // Show error dialogue.
            _ = _windowService.ShowMessageDialog(this, "File error!", $"Unable to save configuration! {ex.Message}", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

            Log.Error(ex, "Failed to save configuration");
        }
    }

    /// <summary>
    /// Opens a dialog window for selecting a device to asynchronously connect to.
    /// </summary>
    /// <param name="deviceModel">DeviceModel.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    private async Task ConnectCameraFromDialogAsync(DeviceModel deviceModel)
    {
        if (deviceModel.IsConnected)
        {
            // Disconnect device.
            await deviceModel.DisconnectDeviceAsync();
        }
        else
        {
            // Instantiate view model for dialog.
            using var dialogViewModel = new OpenDeviceDialogWindowViewModel(_windowService, _deviceProvider);

            // Show dialog.
            _ = _windowService.ShowDialog(dialogViewModel);

            if (dialogViewModel.DialogResult == MessageDialogResult.Canceled || dialogViewModel.SelectedDevice == null)
                return;

            // Retrieve selected device from dialog.
            var deviceInfo = new DeviceInfo(dialogViewModel.SelectedDevice);

            // Make new device the currently selected one.
            SelectedDevice = deviceModel;

            try
            {
                // Open device.
                await deviceModel.ConnectDeviceAsync(deviceInfo, _deviceProvider);
            }
            catch (Exception ex)
            {
                _ = _windowService.ShowMessageDialog(this, "Connection error!", $"Could not open device {deviceInfo.ModelName} (ID: {deviceInfo.UniqueID})! \n\nMessage: {ex.Message}", MessageDialogStyle.Affirmative, MetroDialogHelper.MessageDialogSettings);
            }
            finally
            {
                // Notify commands of changes.
                OpenParameterDialogWindowCommand?.NotifyCanExecuteChanged();
                ConnectCameraFromDialogCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Opens a file dialog and saves current configuration to selected file.
    /// </summary>
    /// <param name="useExisting">Use existing file path.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    private async Task SaveConfigurationDialogAsync(bool useExisting)
    {
        string filePath = ConfigurationFilePath;

        if (useExisting == false || string.IsNullOrEmpty(filePath))
        {
            string fileName = ConfigurationFilePath;

            // Create new filename.
            if (string.IsNullOrEmpty(fileName))
            {
                useExisting = false;

                // Build filename using device model name(s) and current date.
                fileName = "FusionConfiguration";
                if (SelectedDevice.IsConnected)
                    fileName += $"_{SelectedDevice.ModelName}";
                fileName += $"_{DateTime.Now:yyyyMMdd}.xml";
            }

            // Retrieve filepath from dialog.
            filePath = _windowService.ShowSaveFileDialog(title: "Select configuration file", fileName: fileName, filter: "Configuration files (*.xml)|*.xml", defaultExtension: "xml");
            if (string.IsNullOrEmpty(filePath))
                return;
        }

        // Save configuration to file.
        await SaveConfigurationToFileAsync(filePath, useExisting);
    }

    /// <summary>
    /// Opens a file dialog and loads configuration from selected file.
    /// </summary>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    private async Task LoadConfigurationDialogAsync(CancellationToken token)
    {
        // Retrieve filepath from dialog.
        string filePath = _windowService.ShowOpenFileDialog(title: "Select configuration file", multiSelect: false, filter: "Configuration files (*.xml)|*.xml", defaultExtension: "xml");
        if (string.IsNullOrEmpty(filePath))
            return;

        // Reset cancellation signal source.
        _cancellationTokenSource = new CancellationTokenSource();

        // Register cancellation request delegate to command cancellations.
        var registration = token.Register(() => _cancellationTokenSource.Cancel());

        try
        {
            await LoadConfigurationFromFileAsync(filePath, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            HandleLoadConfigurationCancellation(filePath);
        }
        catch (Exception ex)
        {
            HandleLoadConfigurationException(filePath, ex);
        }
        finally
        {
            // Unregister and dispose delegate.
            registration.Unregister();
            registration.Dispose();

            // Dispose cancellation signal source.
            _cancellationTokenSource.Dispose();

            // Update commands.
            OpenParameterDialogWindowCommand?.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Handles user cancellations while loading a configuration.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    private void HandleLoadConfigurationCancellation(string filePath)
    {
        // Show error dialogue.
        _ = _windowService.ShowMessageDialog(this, "Operation cancelled!", $"Loading of configuration in {filePath} was cancelled.", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

        Log.Error("Loading of configuration in {FileName} was cancelled", filePath);
    }

    /// <summary>
    /// Handles exceptions occuring while loading a configuration.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    /// <param name="exception">Exception raised during loading.</param>
    private void HandleLoadConfigurationException(string filePath, Exception exception)
    {
        // Show error dialogue.
        _ = _windowService.ShowMessageDialog(this, "File error!", $"Unable to load configuration in \"{filePath}\": {exception.Message}", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);

        Log.Error(exception, "Failed to load configuration in {FileName}", filePath);
    }

    /// <summary>
    /// Opens a new modal editor dialog window, for displaying and editing available parameters of device.
    /// </summary>
    /// <param name="deviceModel">Model of device.</param>
    private void OpenParameterDialogWindow(DeviceModel deviceModel)
    {
        _ = _windowService.ShowDialog(new ParameterDialogWindowViewModel(viewTitle: $"Device Settings ({deviceModel.Device.DeviceInfo.ModelName})",
                                                                         parameterCollection: deviceModel.Device.Parameters,
                                                                         visibility: (GcVisibility)(int)UserVisibility,
                                                                         toolbarVisibility: System.Windows.Visibility.Visible,
                                                                         timeDelay: DeviceParameterUpdateTimeDelay));
    }

    #endregion

    #region Eventhandlers

    /// <summary>
    /// Event-handling method to <see cref="INotifyPropertyChanged.PropertyChanged"/> events in DeviceModel.
    /// </summary>
    private void DeviceModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Update commands at connection changes.
        if (e.PropertyName == nameof(DeviceModel.IsConnected))
        {
            // Enable saving of configuration if any device is connected (disable if none is connected).
            CanSaveConfiguration = SelectedDevice.IsConnected;

            // Notify commands of changes.
            OpenParameterDialogWindowCommand?.NotifyCanExecuteChanged();
            ConnectCameraFromDialogCommand?.NotifyCanExecuteChanged();

            // Reset file path if no device is connected.
            if (SelectedDevice.IsConnected == false)
                ConfigurationFilePath = null;
        }
    }

    /// <summary>
    /// Event-handling method to <see cref="DeviceModel.ConnectionLost"/> events.
    /// </summary>
    private async void DeviceModel_ConnectionLost(object sender, EventArgs e)
    {
        var deviceModel = sender as DeviceModel;

        // Note: Some method calls need to be invoked on UI thread, since this event is raised in a separate thread and cannot update UI.

        // Close dialogs and windows.
        _dispatcherService.Invoke(_windowService.CloseWindow<FullScreenImageWindowViewModel>);
        _dispatcherService.Invoke(_windowService.CloseWindow<ParameterDialogWindowViewModel>);

        // Show modal progress window while stopping acquisitions and disconnecting device (only show if process is long?).
        IProgressController controller = await _windowService.ShowProgressDialogAsync(viewModel: this,
                                                                                      title: "Device error!",
                                                                                      message: "Connection problems detected...Please wait...",
                                                                                      isCancelable: false,
                                                                                      settings: MetroDialogHelper.GetProgressDialogSettings());

        // Set progress bar to be indeterminate.
        controller?.SetIndeterminate();

        // Stop active acquisitions on all channels.
        await _dispatcherService.Invoke(() => Messenger.Send<StopAcquisitionAsyncRequestMessage>().Response);

        // Cache device information before disconnecting it.
        var deviceInfo = new DeviceInfo(deviceModel.Device.DeviceInfo);

        // Disconnect device.
        if (deviceModel.IsConnected)
            await _dispatcherService.Invoke(deviceModel.DisconnectDeviceAsync);

        // Close progress window.
        if (controller != null)
            await controller.CloseAsync();

        // Log error.
        Log.Error("Connection to device {DeviceName} (ID: {DeviceID}) was lost", deviceInfo.ModelName, deviceInfo.UniqueID);

        // Show dialog to user.
        await _windowService.ShowMessageAsync(viewModel: this,
                                              title: "Device error!",
                                              message: $"Connection to device was lost!",
                                              style: MessageDialogStyle.Affirmative,
                                              settings: MetroDialogHelper.DefaultSettings);
    }

    /// <summary>
    /// Event-handling method to a <see cref="DragDrop.DropEvent"/>, handling loading of configuration files dropped onto UI.
    /// </summary>
    public async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Note that you can drag more than one file...
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // ...but only single files are accepted.
            if (files.Length != 1)
            {
                _ = _windowService.ShowMessageDialog(this, "File error!", "Only one file at a time can be dropped!", MessageDialogStyle.Affirmative, MetroDialogHelper.DefaultSettings);
                return;
            }

            // Only configuration files are handled here (recordings are handled in PlayBackViewModel).
            if (Path.GetExtension(files[0]) != ".xml")
                return;

            // Reset cancellation signal source.
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Load configuration from dropped file.
                await LoadConfigurationFromFileAsync(files[0], _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Handle user cancellation.
                HandleLoadConfigurationCancellation(files[0]);
            }
            catch (Exception ex)
            {
                // Handle exceptions.
                HandleLoadConfigurationException(files[0], ex);
            }
            finally
            {
                // Dispose cancellation signal source.
                _cancellationTokenSource.Dispose();

                // Update commands.
                OpenParameterDialogWindowCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Event-handling method to window closing events.
    /// </summary>
    public async void OnMainWindowClosing(object sender, CancelEventArgs e)
    {
        if (e.Cancel == false)
        {
            // Disconnect devices.
            if (SelectedDevice.IsConnected)
                await SelectedDevice.DisconnectDeviceAsync();
        }
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
            // Disable loading/saving of configurations during acquisition.
            CanLoadConfiguration = message.NewValue == false;
            CanSaveConfiguration = message.NewValue == false;

            // Disable changing settings during acquisition.
            IsEnabled = message.NewValue == false;
        }

        if (message.PropertyName == nameof(PlayBackViewModel.IsLoaded) || message.PropertyName == nameof(PlayBackViewModel.IsLoading))
        {
            // Disable changing settings during playback.
            IsEnabled = message.NewValue == false;
        }
    }

    #endregion
}