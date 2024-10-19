using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using ImagerViewer.Utilities.Dialogs;
using ImagerViewer.Utilities.Services;
using GcLib;
using MahApps.Metro.Controls.Dialogs;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for the selection of a device to connect to.
/// </summary>
internal sealed class OpenDeviceDialogWindowViewModel : ObservableObject, IDisposable
{
    #region Fields

    // backing-fields
    private List<GcDeviceInfo> _deviceList;
    private GcDeviceInfo _selectedDevice;
    private MessageDialogResult _dialogResult;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Timer object to update device list.
    /// </summary>
    private readonly Timer _updateDeviceListTimer;

    /// <summary>
    /// Service providing devices of type <see cref="GcDevice"/>.
    /// </summary>
    private readonly IDeviceProvider _deviceProvider;

    #endregion

    #region Properties

    /// <summary>
    /// Current list of devices available.
    /// </summary>
    public List<GcDeviceInfo> DeviceList
    {
        get => _deviceList;
        set => SetProperty(ref _deviceList, value);
    }

    /// <summary>
    /// Selected device (in current list of devices available).
    /// </summary>
    public GcDeviceInfo SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    /// <summary>
    /// Result of dialog.
    /// </summary>
    public MessageDialogResult DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new view model for selection of a device to open from a list of available devices on the system.
    /// </summary>
    /// <param name="dialogService">Dialog service.</param>
    /// <param name="deviceProvider">Service providing devices.</param>
    /// <param name="updateInterval">Interval in milliseconds between updates of available devices on the system.</param>
    public OpenDeviceDialogWindowViewModel(IMetroWindowService dialogService, IDeviceProvider deviceProvider, uint updateInterval = 1000)
    {
        // Retrieve injected services.
        _windowService = dialogService;
        _deviceProvider = deviceProvider;

        // Get list of available camera devices.
        _deviceProvider.UpdateDeviceList();
        DeviceList = _deviceProvider.GetDeviceList();

        // Default selection will be first device in list.
        if (DeviceList.Count > 0)
            SelectedDevice = DeviceList[0];

        // Start polling for detection of newly connected or disconnected devices.
        _updateDeviceListTimer = new Timer { AutoReset = true, Interval = updateInterval, Enabled = true };
        _updateDeviceListTimer.Elapsed += UpdateDeviceList;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Event-handling method to dialog closing events.
    /// </summary>
    public void OnWindowClosing(object sender, CancelEventArgs e)
    {
        // Check that current device selection is OK.
        if (DialogResult == MessageDialogResult.Affirmative && SelectedDevice != null)
        {
            // Device is already opened in application.
            if (SelectedDevice.IsOpen)
            {
                _ = _windowService.ShowMessageDialog(this, "Connection error!", "Camera is already opened!", MessageDialogStyle.Affirmative, MetroDialogHelper.MessageDialogSettings);

                // Cancel closing.
                e.Cancel = true;
                return;
            }
            // Device is (probably) opened in other application.
            else if (SelectedDevice.IsAccessible == false)
            {
                _ = _windowService.ShowMessageDialog(this, "Connection error!", "Camera is not accessible! Please check that camera is not opened in any other application.", MessageDialogStyle.Affirmative, MetroDialogHelper.MessageDialogSettings);

                // Cancel closing.
                e.Cancel = true;
                return;
            }

            // Valid selection, go ahead and dispose.
            Dispose();
        }
        else
        {
            // Cancelled, go ahead and dispose.
            Dispose();
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Event-handler to <see cref="Timer.Elapsed"/> events, updating list of available devices.
    /// </summary>
    private void UpdateDeviceList(object sender, ElapsedEventArgs e)
    {
        // Update device list only if changed since last check.
        if (_deviceProvider.UpdateDeviceList())
        {
            DeviceList = _deviceProvider.GetDeviceList();
            if (DeviceList.Contains(SelectedDevice) == false)
                SelectedDevice = null;
        }
    }

    public void Dispose()
    {
        // Stop and dispose timer.
        _updateDeviceListTimer.Elapsed -= UpdateDeviceList;
        _updateDeviceListTimer.Stop();
        _updateDeviceListTimer.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}