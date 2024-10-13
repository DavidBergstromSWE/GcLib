using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using GcLib;
using GcLib.Utilities.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace WinFormsDemoApp;

/// <summary>
/// Demo app for <see cref="GcLib"/> class library, allowing connection to devices detected on the system, changing device parameter settings and providing some elementary recording and playback functionality. 
/// </summary>
public partial class WinFormsDemoApp : Form
{
    #region InvokingFlags

    /// <summary>
    /// Flag indicating that invoking of UI control methods is currently in progress.
    /// </summary>
    private bool _invokeInProgress = false;

    /// <summary>
    /// Flag indicating that new invoking of UI control methods should be prevented (e.g. when closing down application).
    /// </summary>
    private bool _stopInvoking = false;

    #endregion

    /// <summary>
    /// System level in the GcLib library, allowing instantiation of new devices.
    /// </summary>
    private readonly GcSystem _system;

    /// <summary>
    /// Camera device connected.
    /// </summary>
    private GcDevice _camera = null;

    /// <summary>
    /// Datastream used for streaming data from device.
    /// </summary>
    private GcDataStream _dataStream = null;

    /// <summary>
    /// Thread used for transferring data from datastream to display control.
    /// </summary>
    private readonly GcProcessingThread _displayThread = new(bufferCapacity: 4);

    /// <summary>
    /// Reads images from file during playback.
    /// </summary>
    private GcBufferReader _imageReader = null;

    /// <summary>
    /// Writes images to file during recording.
    /// </summary>
    private GcBufferWriter _imageWriter = null;

    /// <summary>
    /// Default user visibility setting in UI.
    /// </summary>
    private GcVisibility _defaultVisibility = GcVisibility.Beginner;

    /// <summary>
    /// Initializes library and sets the appropriate start state for UI.
    /// </summary>
    public WinFormsDemoApp()
    {
        InitializeComponent();

        // Configure application logger.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information("Application started");

        // Initialize EmguCV.
        _ = CvInvoke.Init();

        // Initialize GcLib with the logger.
        GcLibrary.Init(logger: new SerilogLoggerFactory(Log.Logger).CreateLogger<GcSystem>());

        // Instantiate system level.
        _system = new GcSystem();

        // Set DisplayControl settings.
        DisplayControl.SizeMode = PictureBoxSizeMode.CenterImage;
        DisplayControl.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.Minimum;

        // Control validation setting.
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        // Center UI on display screen.
        StartPosition = FormStartPosition.CenterScreen;

        // Begin with empty form.
        SetUIState(UIState.InitialState);

#if DEBUG
        SaveImagesTextBox.Text = @"c:\testdata\testfile.bin";
        _defaultVisibility = GcVisibility.Guru;
#else
        SaveImagesTextBox.Text = @"";
        _defaultVisibility = GcVisibility.Beginner;
#endif
    }

    /// <summary>
    /// Opens a dialog window allowing user to select which device to connect to.
    /// </summary>
    private void ConnectButton_Click(object sender, EventArgs e)
    {
        // Show dialog window.
        var form = new OpenCameraDialogue(_system) { StartPosition = FormStartPosition.CenterParent };

        // Return if no device is selected or dialog is cancelled,
        if ((form.ShowDialog() != DialogResult.OK) || (form.SelectedDevice == null))
        {
            if (_camera == null)
                SetUIState(UIState.InitialState);
            else SetUIState(UIState.ReadyState);
            return;
        }

        // Close previously opened device & datastream.
        CloseDataStream();
        CloseDevice();

        // Disable controls while opening device.
        SetUIState(UIState.BusyState);

        try
        {
            // Connect to selected device.
            _camera = _system.OpenDevice(form.SelectedDevice.UniqueID);

            // Register to device events.
            _camera.ConnectionLost += OnConnectionLost;
            _camera.AcquisitionStopped += OnAcquisitionStopped;

            // Open a new datastream on the device.
            _dataStream = _camera.OpenDataStream();

            // Display device info.
            DisplayDeviceInfo(form.SelectedDevice);

            // Setup parameter grid view.
            ParameterGridView.Init(_camera, _defaultVisibility);
            ParameterGridView.ParameterValueChanged += ParameterGridView_ValueChanged;

            // Setup panels with device parameters.
            InitializeAcquisitionPanel();

            // Initialize status control.
            StatusControl.DataStream = _dataStream;
            StatusControl.DisplayThread = _displayThread;
            StatusControl.ImageWriter = _imageWriter;

            // Show empty display control.
            DisplayControl.Image = null;
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show(ex.Message + " Please check that camera is not already in use!", "Connection error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Change UI state.
        if (_camera != null)
            SetUIState(UIState.ReadyState);
        else SetUIState(UIState.InitialState);
    }

    /// <summary>
    /// Event handler to connection lost events in camera/datastream.
    /// </summary>
    private async void OnConnectionLost(object sender, EventArgs e)
    {
        // Set UI in busy state.
        Invoke(() => { SetUIState(UIState.BusyState); });

        // Shutdown device-allocated resources. Wait until task is completed.
        await Task.Run(() =>
        {
            StopRecording();
            CloseDataStream();
            CloseDevice();
        });

        // Reset UI state.
        Invoke(() =>  {  SetUIState(UIState.InitialState); });
    }

    /// <summary>
    /// Stops running threads, shuts down datastream and camera and resets UI to its default state.
    /// </summary>
    private void DisconnectButton_Click(object sender, EventArgs e)
    {
        StopRecording();
        CloseDataStream();
        CloseDevice();

        SetUIState(UIState.InitialState);
    }

    /// <summary>
    /// Shuts down camera.
    /// </summary>
    private void CloseDevice()
    {
        if (_camera != null)
        {
            _camera.Close();
            _camera = null;
        }
    }

    /// <summary>
    /// Shuts down datastream.
    /// </summary>
    private void CloseDataStream()
    {
        if (_dataStream != null)
        {
            // Unregister from events.
            _camera.AcquisitionStopped -= OnAcquisitionStopped;
            _camera.ConnectionLost -= OnConnectionLost;

            // Stop streaming.
            if (_dataStream.IsStreaming)
                _dataStream.Stop();

            // Close datastream.
            if (_dataStream.IsOpen)
                _dataStream.Close();

            _dataStream = null;
        }
    }

    /// <summary>
    /// Updates parameter-related (tagged) controls in GUI when a parameter value in GcParameterGridView has been changed.
    /// </summary>
    private void ParameterGridView_ValueChanged(object sender, EventArgs e)
    {
        UpdateControls(AcquisitionPanel);
    }

    /// <summary>
    /// Update parameter-related (tagged) child controls in parent Control with current parameter values from camera.
    /// </summary>
    /// <param name="parentControl">Parent control.</param>
    private void UpdateControls(Control parentControl)
    {
        foreach (Control c in parentControl.Controls)
        {
            // Skip all non-tagged controls.
            if (c.Tag == null)
                continue;

            string parameterName = ((GcParameter)c.Tag).Name;
            string parameterValue = _camera.Parameters.GetParameterValue(parameterName);

            if (parameterValue != null)
            {
                switch (c)
                {
                    case TextBox textBox:
                        textBox.Text = parameterValue;
                        break;

                    case CheckBox checkBox:
                        checkBox.Checked = Convert.ToBoolean(parameterValue);
                        break;

                    case ComboBox comboBox:
                        comboBox.SelectedItem = parameterValue;
                        break;

                    default:
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Shut down GUI, closing any running datastream, camera and/or threads.
    /// </summary>
    private async void TestGUI_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Stop invoking of UI controls from other threads before closing.
        if (_invokeInProgress)
        {
            // Cancel the original event.
            e.Cancel = true;

            // Stop taking new work.
            _stopInvoking = true;

            // Wait until current invoke finishes.
            await Task.Factory.StartNew(() => { while (_invokeInProgress) ; });

            // Ready to close the form.
            Close();
        }

        // Stop displaying images (if active).
        _displayThread.BufferProcess -= OnBufferDisplay;
        _displayThread.Stop(false);
        _displayThread.Dispose();

        // Stop recording (if active).
        StopRecording();

        // Release resources.
        CloseDataStream();
        CloseDevice();

        // Dipose system.
        _system?.Dispose();

        // Close library.
        GcLibrary.Close();

        Log.Information("Application stopped");
    }
}