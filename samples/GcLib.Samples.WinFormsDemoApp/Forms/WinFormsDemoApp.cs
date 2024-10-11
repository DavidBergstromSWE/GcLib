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
/// Test application based on Windows Forms for testing and developing GcLib class library.
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
    /// Thread used for transporting data between datastream and display control.
    /// </summary>
    private readonly GcProcessingThread _displayThread = null;

    /// <summary>
    /// Reads images from file during playback.
    /// </summary>
    private GcBufferReader _imageReader = null;

    /// <summary>
    /// Writes images to file during recording.
    /// </summary>
    private GcBufferWriter _imageWriter = null;

    /// <summary>
    /// Default visibility setting in UI.
    /// </summary>
    private GcVisibility _defaultVisibility = GcVisibility.Beginner;

    /// <summary>
    /// Constructor, initializing UI components and setting appropriate starting mode/state.
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

        // Initialize GcLib with logging.
        var logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<GcSystem>();
        GcLibrary.Init(logger: logger);

        _system = new GcSystem();

        // Control validation setting.
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        // Center UI on display screen.
        StartPosition = FormStartPosition.CenterScreen;

        // Set DisplayControl settings.
        DisplayControl.SizeMode = PictureBoxSizeMode.Normal;
        DisplayControl.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.Everything; // Should be specified in GUIMode or made into UI option?

        // Instantiate display thread.
        _displayThread = new GcProcessingThread(bufferCapacity: 4);

        // Begin with empty form.
        SetUIState(UIState.InitialState);

#if DEBUG
        SaveImagesTextBox.Text = @"C:\testdata\testfile.bin";
        _defaultVisibility = GcVisibility.Guru;
#else
        SaveImagesTextBox.Text = @"";
        _defaultVisibility = GcVisibility.Beginner;
#endif
    }

    /// <summary>
    /// Open a dialogue window for user to select which camera to connect to.
    /// </summary>
    private void ConnectButton_Click(object sender, EventArgs e)
    {
        GcDeviceInfo selectedDevice;

        SetUIState(UIState.BusyState);

        // Show dialogue window.
        var form = new OpenCameraDialogue(_system) { StartPosition = FormStartPosition.CenterParent };

        if ((form.ShowDialog() != DialogResult.OK) || (form.SelectedDevice == null))
        {
            if (_camera == null)
                SetUIState(UIState.InitialState);
            else SetUIState(UIState.ReadyState);
            return;
        }

        selectedDevice = form.SelectedDevice;

        // Close previously opened camera & datastream.
        CloseDataStream();
        CloseCamera();

        // Disable controls and show WaitCursor while opening camera.
        SetUIState(UIState.BusyState);

        try
        {
            // Connect to selected camera.
            _camera = _system.OpenDevice(selectedDevice.UniqueID);

            // Register to camera events.
            _camera.ConnectionLost += OnConnectionLost;
            _camera.AcquisitionStopped += OnAcquisitionStopped;

            // Open a new datastream on camera.
            _dataStream = _camera.OpenDataStream();

            // Display device info.
            DisplayDeviceInfo(selectedDevice);

            // Setup ParameterGridView (user control allowing display/setting of camera parameters).
            ParameterGridView.Init(_camera, _defaultVisibility);
            ParameterGridView.ParameterValueChanged += ParameterGridView_ValueChanged;

            // Setup panels with camera parameters.
            InitializePropertyPanel();
            InitializeAcquisitionPanel();

            // Initialize StatusControl with properties.
            StatusControl.DataStream = _dataStream;
            StatusControl.DisplayThread = _displayThread;
            StatusControl.ImageWriter = _imageWriter;

            // Clear ImageBox.
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
        // Retain device info (if needed for reconnection).
        GcDeviceInfo deviceInfo = _camera.DeviceInfo;

        // Ask user whether to reconnect or not.
        DialogResult dialogResult = DialogResult.Yes;
        Invoke(new Action(() => { dialogResult = MessageBox.Show(this, $"Lost connection to camera {deviceInfo.ModelName}, check cable connection! Try to connect again?", "Connection error!", MessageBoxButtons.YesNo, MessageBoxIcon.Error); }));

        // Shutdown device-allocated resources. Wait until task is completed.
        await Task.Run(() =>
        {
            Invoke(new Action(() =>
              {
                  ParameterGridView.Clear();
                  SetUIState(UIState.BusyState);
                  Cursor = Cursors.WaitCursor;
              }));

            StopRecording();

            CloseDataStream();
            CloseCamera(); // can be slow...
        });


        // Reconnect to camera (if possible).
        if (dialogResult == DialogResult.Yes)
        {
            try
            {
                // Update list of available devices.
                _system.UpdateDeviceList();

                // Connect to previously connected camera.
                _camera = _system.OpenDevice(deviceInfo.UniqueID);
                _camera.ConnectionLost += OnConnectionLost;
                _camera.AcquisitionStopped += OnAcquisitionStopped;

                // Open new datastream on device.
                _dataStream = _camera.OpenDataStream();

                // Invoke changes to UI.
                Invoke(new Action(() =>
                {
                    // Display device info.
                    DisplayDeviceInfo(deviceInfo);

                    // Setup ParameterGridView control.
                    ParameterGridView.Init(_camera, _defaultVisibility);
                    ParameterGridView.ParameterValueChanged += ParameterGridView_ValueChanged;

                    // Setup panels with camera parameters.
                    InitializePropertyPanel();
                    InitializeAcquisitionPanel();

                    // Clear display control.
                    DisplayControl.Image = null;
                }));
            }
            catch (Exception ex)
            {
                // Reconnection not possible.
                _ = MessageBox.Show(ex.Message, "Connection error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Change UI state.
        Invoke(new Action(() =>
          {
              if (_camera != null)
                  SetUIState(UIState.ReadyState);
              else SetUIState(UIState.InitialState);
          }));

        // Return default cursor.
        Invoke(new Action(() => { Cursor = Cursors.Default; }));
    }

    /// <summary>
    /// Stops running threads, shuts down datastream and camera and resets UI to its default state.
    /// </summary>
    private void DisconnectButton_Click(object sender, EventArgs e)
    {
        StopRecording();
        CloseDataStream();
        CloseCamera();

        SetUIState(UIState.InitialState);
    }

    /// <summary>
    /// Shuts down camera.
    /// </summary>
    private void CloseCamera()
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
        //ParameterGridView.RefreshGridView();
        UpdateControls(PropertyPanel);
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
        CloseCamera();

        // Dipose system.
        _system?.Dispose();

        // Close library.
        GcLibrary.Close();

        Log.Information("Application stopped");
    }
}