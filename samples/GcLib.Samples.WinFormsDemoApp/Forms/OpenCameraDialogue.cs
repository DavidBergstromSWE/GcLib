using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Forms;
using GcLib;

namespace WinFormsDemoApp;

/// <summary>
/// Dialogue window for letting the user select a camera based on a displayed list of available devices.
/// </summary>
public partial class OpenCameraDialogue : Form
{
    /// <summary>
    /// System level in the GcLib library, allowing instantiation of new devices.
    /// </summary>
    private readonly GcSystem _system;

    /// <summary>
    /// Timer object used for polling available devices.
    /// </summary>
    private readonly System.Timers.Timer _timer;

    /// <summary>
    /// List of currently available camera devices.
    /// </summary>
    private readonly List<GcDeviceInfo> _currentDeviceList;

    /// <summary>
    /// Selected camera device in listbox.
    /// </summary>
    public GcDeviceInfo SelectedDevice => (GcDeviceInfo)CameraListBox.SelectedItem;

    /// <summary>
    /// Opens a new dialogue window.
    /// </summary>
    public OpenCameraDialogue(GcSystem system)
    {
        InitializeComponent();

        _system = system;

        // Update list of available camera devices.
        _system.UpdateDeviceList();
        _currentDeviceList = _system.GetDeviceList();

        // Populate listbox with available devices, displaying ModelName property of GcDeviceInfo.
        CameraListBox.DataSource = _currentDeviceList;
        CameraListBox.DisplayMember = "ModelName";
        CameraListBox.SelectedIndex = 0;

        // Start polling every second for the detection of newly connected cameras.
        _timer = new System.Timers.Timer { AutoReset = true, Interval = 1000, Enabled = true };
        _timer.Elapsed += UpdateCameraListBox;
    }

    /// <summary>
    /// Event-handling method to Elapsed events in timer object. Updates list of devices shown in listbox.
    /// </summary>
    private void UpdateCameraListBox(object sender, ElapsedEventArgs e)
    {
        // Update list of available camera devices.
        bool changed = _system.UpdateDeviceList();
        
        // Only necessary to update listbox if device list has changed.
        if (changed == false)
            return;

        GcDeviceInfo selectedItem;
        if (CameraListBox.InvokeRequired)
        {
            CameraListBox.Invoke((MethodInvoker)delegate
            {
                selectedItem = (GcDeviceInfo)CameraListBox.SelectedItem;
                CameraListBox.DataSource = _system.GetDeviceList();

                // Retain selected item in list box.
                if (selectedItem != null)
                {
                    int index = CameraListBox.FindString(selectedItem.ModelName);
                    if (index >= 0)
                        CameraListBox.SelectedIndex = index;
                    else CameraListBox.ClearSelected();
                }
            });
        }
    }

    /// <summary>
    /// Event-handling method to selection changes in listbox. Updates textboxes with info about currently selected camera.
    /// </summary>
    private void CameraListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (CameraListBox.SelectedItem == null)
            return;

        var selectedCameraID = (GcDeviceInfo)CameraListBox.SelectedItem;

        // Display further info about camera.
        VendorNameLabel.Text = selectedCameraID.VendorName;
        ModelNameLabel.Text = selectedCameraID.ModelName;
        SerialNumberLabel.Text = selectedCameraID.SerialNumber;
        DeviceUniqueIDLabel.Text = selectedCameraID.UniqueID;
        AccessStatusLabel.Text = selectedCameraID.IsAccessible.ToString();
    }

    /// <summary>
    /// Event-handling method to OK button click events. Validates camera selection and closes window.
    /// </summary>
    private void OKButton_Click(object sender, EventArgs e)
    {
        _timer.Enabled = false;
        _timer.Elapsed -= UpdateCameraListBox;
        if (SelectedDevice != null)
        {
            // Check if camera is already opened by application.
            if (SelectedDevice.IsOpen)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show($"Camera is already open!", "Connection Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check for accessibility (camera may be open in another application).
            if (SelectedDevice.IsAccessible == false)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show($"Camera is not accessible! Please check that camera is not opened in another application.", "Connection Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
    }

    /// <summary>
    /// Event-handling method to Cancel button click events. Closes window.
    /// </summary>
    private void CancelButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    /// <summary>
    /// Event-handling method to double click events in listbox. Selects camera and closes window.
    /// </summary>
    private void CameraListBox_MouseDoubleClick(object sender, MouseEventArgs e) => OKButton_Click(sender, e);

    /// <summary>
    /// Event-handling method to Form closing events. Disposes all resources.
    /// </summary>
    private void OpenCameraDialogue_FormClosing(object sender, FormClosingEventArgs e) => _timer.Dispose();
}