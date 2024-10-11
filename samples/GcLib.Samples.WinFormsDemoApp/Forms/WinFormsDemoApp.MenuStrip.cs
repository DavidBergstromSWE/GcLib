using System;
using System.Reflection;
using System.Windows.Forms;
using GcLib;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp : Form
{
    #region File menu

    /// <summary>
    /// Event-handling method for Load Configuration button click events in File menu. Loads a previously saved configuration file and updates camera parameter list and associated UI controls.
    /// </summary>
    private void LoadConfigurationStripMenuItem_Click(object sender, EventArgs e)
    {
        if (_camera != null)
        {
            OpenFileDialog.InitialDirectory = @"";
            OpenFileDialog.FileName = $@"";
            OpenFileDialog.Filter = "Configuration files (*.xml)|*.xml";
            OpenFileDialog.DefaultExt = "xml";
            OpenFileDialog.RestoreDirectory = true;

            if (OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Set UI state to busy (prevents user interaction and auto-refreshing of controls while loading and restoring).
                SetUIState(UIState.BusyState);

                string filePath = OpenFileDialog.FileName;

                // Load configuration file and restore setting to camera.
                var configurationManager = new GcConfigurationManager(_camera);

                try
                {
                    configurationManager.Load(filePath);
                    configurationManager.Restore(_camera);
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(this, "Error reading configuration file: " + ex.Message, "FileIO Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    configurationManager.Dispose();
                }

                // Update UI state.
                SetUIState(UIState.ReadyState);

                // Update GUI controls.
                ParameterGridView.RefreshGridView();
                UpdateControls(PropertyPanel);
                UpdateControls(AcquisitionPanel);
            }
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// Event-handling method for Save Configuration button click events in File menu. Saves a configuration file based on current camera parameter list.
    /// </summary>
    private void SaveConfigurationStripMenuItem_Click(object sender, EventArgs e)
    {
        if (_camera != null)
        {
            SaveFileDialog.InitialDirectory = @"";
            SaveFileDialog.FileName = $@"{_camera.DeviceInfo.VendorName}_{_camera.DeviceInfo.ModelName}_v{Assembly.GetExecutingAssembly().GetName().Version}.xml";
            SaveFileDialog.Filter = "Configuration files (*.xml)|*.xml";
            SaveFileDialog.DefaultExt = "xml";
            SaveFileDialog.RestoreDirectory = true;

            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Set UI state to busy (prevents user interaction and auto-refreshing of controls while storing and saving).
                SetUIState(UIState.BusyState);

                // Instantiate new configuration manager for saving configuration to file.
                var configurationManager = new GcConfigurationManager(_camera);

                try
                {
                    // Retrieve updated list of visible parameters.
                    _camera.Parameters.Update();
                    var parameterList = _camera.Parameters.ToList(GcVisibility.Guru);

                    // Store parameter list and save it to xml configuration file.
                    configurationManager.Store(parameterList);
                    configurationManager.Save(SaveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(this, "Error writing configuration file: " + ex.Message, "FileIO Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    configurationManager.Dispose(); // is this enough?
                }

                // Update UI state.
                SetUIState(UIState.ReadyState);
            }
        }
    }

    /// <summary>
    /// Event-handling method for Open Image Sequence button click events in File menu. Lets user select a file in a dialogue window, opens file and initializes playback control.
    /// </summary>
    private async void OpenImagesStripMenuItem_Click(object sender, EventArgs e)
    {
        SetUIState(UIState.BusyState); // will not update ui?

        OpenFileDialog.InitialDirectory = @"";
        OpenFileDialog.FileName = $@"";
        OpenFileDialog.Filter = "bin files (*.bin)|*.bin";
        OpenFileDialog.RestoreDirectory = true;

        if (OpenFileDialog.ShowDialog() == DialogResult.OK)
        {
            // Close previously opened file.
            _imageReader?.Dispose();

            try
            {
                // Instantiate new reader of the opened file.
                _imageReader = new GcBufferReader(OpenFileDialog.FileName);
                await _imageReader.OpenAsync();

                // Hook display event handler and open playback control.
                PlayBackControl.BufferDisplay += OnBufferDisplay;
                PlayBackControl.Open(_imageReader);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(this, "Error reading image sequence file " + ex.Message, "FileIO Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update UI state.
            SetUIState(UIState.PlaybackState);
        }
    }

    /// <summary>
    /// Event-handling method for Close Image Sequence button click events in File menu. Closes playback control and file.
    /// </summary>
    private void CloseImagesStripMenuItem_Click(object sender, EventArgs e)
    {
        // Unregister display event handler.
        PlayBackControl.BufferDisplay -= OnBufferDisplay;

        // Close playback control.
        PlayBackControl.Close();

        // Close reader of file.
        _imageReader.Dispose();

        // Clear display control.
        DisplayControl.Image = null;

        // Update UI state.
        if (_camera == null)
            SetUIState(UIState.InitialState);
        else SetUIState(UIState.ReadyState);
    }

    /// <summary>
    /// Event-handling method for Exit button click events in File menu. Closes application.
    /// </summary>
    private void ExitButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    #endregion

    #region View menu

    /// <summary>
    /// Event-handling method for Show DataStream Stats checkbox button click events in View menu.
    /// </summary>
    private void ShowDataStreamStats_CheckedChanged(object sender, EventArgs e)
    {
        // Only show when checked and not in playback mode.
        StatusControl.Visible = ShowStatusControlMenuItem.Checked && (PlayBackControl.Visible == false);
    }

    /// <summary>
    /// Event-handling methods for Limit Display FPS checkbox button click events in View menu.
    /// </summary>
    private void LimitFPSCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        _displayThread.LimitFPS = LimitFPSCheckBox.Checked;
        _displayThread.TargetFPS = 30;
    }

    #endregion

    #region Display menu

    /// <summary>
    /// Event-handling method for Show Frame ID checkbox button click events in View menu. Enables/disables text overlay with frame ID shown in top left corner of displayed image.
    /// </summary>
    private void ShowFrameIDMenuItem_CheckChanged(object sender, EventArgs e)
    {
        DisplayControl.ShowFrameID = DisplayFrameCounterCheckBox.Checked;
    }

    /// <summary>
    /// Event-handling method for Show Timestamp checkbox button click events in View menu. Enables/disables text overlay with timestamp shown in top right corner of displayed image..
    /// </summary>
    private void ShowTimeStampMenuItem_CheckChanged(object sender, EventArgs e)
    {
        DisplayControl.ShowTimeStamp = DisplayTimeStampCheckBox.Checked;
    }

    /// <summary>
    /// Event-handling method for Show FPS checkbox button click events in View menu. Enables/disables text overlay with displayed frame rate shown in bottom center of displayed image.
    /// </summary>
    private void ShowFrameRateMenuItem_CheckChanged(object sender, EventArgs e)
    {
        DisplayControl.ShowFPS = DisplayFPSCheckBox.Checked;
    }

    /// <summary>
    /// Event-handling method for Show All checkbox button click events in View menu. Enables/disables all text overlays in displayed image.
    /// </summary>
    private void ShowAllMenuItem_CheckChanged(object sender, EventArgs e)
    {
        DisplayFrameCounterCheckBox.Checked = DisplayTimeStampCheckBox.Checked = DisplayFPSCheckBox.Checked = DisplayAllCheckBox.Checked;
    }

    #endregion

    #region About menu

    /// <summary>
    /// Event-handling method for About button click events in About menu. Shows a modal form with application and author info.
    /// </summary>
    private void AboutStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new About
        {
            StartPosition = FormStartPosition.CenterParent
        };
        _ = form.ShowDialog(this);
    }

    #endregion
}
