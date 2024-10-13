using System.Drawing;
using System.Windows.Forms;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp
{
    /// <summary>
    /// User interface states.
    /// </summary>
    private enum UIState
    {
        /// <summary>
        /// UI state used when camera is connected and is currently streaming (e.g. during live view or recording).
        /// </summary>
        AcquisitionState,

        /// <summary>
        /// UI state used when camera is connected but is currently not streaming and is awaiting user input.
        /// </summary>
        ReadyState,

        /// <summary>
        /// UI state used during playback of previously recorded image sequences.
        /// </summary>
        PlaybackState,

        /// <summary>
        /// UI state used when application is busy and no user interaction should be possible (controls will be disabled).
        /// </summary>
        BusyState,

        /// <summary>
        /// UI state used as an initial state (e.g. when starting up the application or when no camera is currently connected).
        /// </summary>
        InitialState
    };

    /// <summary>
    /// Set user interface state.
    /// </summary>
    /// <param name="uiState">User interface state.</param>
    private void SetUIState(UIState uiState)
    {
        switch (uiState)
        {
            case UIState.InitialState:

                // Cursor setting.
                Cursor = Cursors.Default;

                // Connection buttons.
                ConnectButton.Enabled = true;
                DisconnectButton.Enabled = false;

                // Main menu.
                LoadConfigurationMenuItem.Enabled = false;
                SaveConfigurationMenuItem.Enabled = false;
                OpenImagesMenuItem.Enabled = true;
                CloseImagesMenuItem.Enabled = false;

                // Display menu.
                LimitFPSCheckBox.Enabled = true;

                // DeviceInfo panel.
                ClearDeviceInfo();
                DeviceInfoPanel.Enabled = false;

                // Acquisition panel.
                AcquisitionPanel.Enabled = false;
                PlayButton.Enabled = false;
                RecordButton.Enabled = false;
                StopButton.Enabled = false;

                // ParameterGridView control.
                ParameterGridView.Clear();
                ParameterGridView.Enabled = false;

                // Display control.
                DisplayControl.Clear();

                // Playback control.
                PlayBackControl.Visible = false;
                ExitPlaybackButton.Visible = false;

                // Status control.
                StatusControl.Visible = ShowStatusControlMenuItem.Checked;
                StatusControl.Enabled = false;

                break;

            case UIState.ReadyState:

                // Cursor setting.
                Cursor = Cursors.Default;

                // Connection buttons.
                ConnectButton.Enabled = true;
                DisconnectButton.Enabled = true;

                // Main menu.
                LoadConfigurationMenuItem.Enabled = true;
                SaveConfigurationMenuItem.Enabled = true;
                OpenImagesMenuItem.Enabled = true;
                CloseImagesMenuItem.Enabled = false;

                // Display menu.
                LimitFPSCheckBox.Enabled = true;

                // DeviceInfoPanel.
                DeviceInfoPanel.Enabled = true;

                // Acquisition panel.
                RecordButton.Enabled = true;
                PlayButton.Enabled = true;
                StopButton.Enabled = false;
                AcquisitionPanel.Enabled = true;

                // ParameterGridView control.
                ParameterGridView.Enabled = true;

                // Display control.
                if (DisplayControl.Image == null)
                    DisplayControl.BackColor = Color.Black;
                else DisplayControl.BackColor = BackColor;

                // PlayBack control.
                PlayBackControl.Visible = false;
                ExitPlaybackButton.Visible = false;

                // Status control.
                StatusControl.Enabled = true;
                StatusControl.Visible = ShowStatusControlMenuItem.Checked;

                break;

            case UIState.BusyState:

                // Cursor setting.
                Cursor = Cursors.WaitCursor;

                // Connection buttons.
                ConnectButton.Enabled = false;
                DisconnectButton.Enabled = false;

                // Main menu.
                LoadConfigurationMenuItem.Enabled = false;
                SaveConfigurationMenuItem.Enabled = false;
                OpenImagesMenuItem.Enabled = false;
                CloseImagesMenuItem.Enabled = false;

                // Display menu.
                LimitFPSCheckBox.Enabled = false;

                // DeviceInfo panel.
                DeviceInfoPanel.Enabled = false;

                // Acquisition panel.
                AcquisitionPanel.Enabled = false;
                PlayButton.Enabled = false;
                RecordButton.Enabled = false;
                StopButton.Enabled = false;

                // ParameterGridView control.
                ParameterGridView.Enabled = false;

                // Display control.
                DisplayControl.BackColor = Color.Black;

                // PlayBack control.
                PlayBackControl.Visible = false;
                ExitPlaybackButton.Visible = false;

                // Status control.
                StatusControl.Visible = ShowStatusControlMenuItem.Checked;
                StatusControl.Enabled = false;

                break;

            case UIState.AcquisitionState:

                // Cursor setting.
                Cursor = Cursors.Default;

                // Connection buttons.
                ConnectButton.Enabled = false;
                DisconnectButton.Enabled = false;

                // File menu.
                LoadConfigurationMenuItem.Enabled = false;
                SaveConfigurationMenuItem.Enabled = false;
                OpenImagesMenuItem.Enabled = false;
                CloseImagesMenuItem.Enabled = false;

                // Display menu.
                LimitFPSCheckBox.Enabled = false;

                // DeviceInfo panel.
                DeviceInfoPanel.Enabled = true;

                // Acquisition panel.
                RecordButton.Enabled = false;
                PlayButton.Enabled = false;
                StopButton.Enabled = true;
                AcquisitionPanel.Enabled = false;

                // ParameterGridView control.
                ParameterGridView.Enabled = false;

                // Display control.
                DisplayControl.BackColor = BackColor;

                // PlayBack control.
                PlayBackControl.Visible = false;
                ExitPlaybackButton.Visible = false;

                // Status control.
                StatusControl.Enabled = false;
                StatusControl.Visible = ShowStatusControlMenuItem.Checked;

                break;

            case UIState.PlaybackState:

                // Cursor setting.
                Cursor = Cursors.Default;

                // Connection buttons.
                ConnectButton.Enabled = false;
                DisconnectButton.Enabled = false;

                // File menu.
                LoadConfigurationMenuItem.Enabled = false;
                SaveConfigurationMenuItem.Enabled = false;
                OpenImagesMenuItem.Enabled = true;
                CloseImagesMenuItem.Enabled = true;

                // Display menu.
                LimitFPSCheckBox.Enabled = false;

                // DeviceInfo panel.
                DeviceInfoPanel.Enabled = false;

                // Acquisition panel.
                AcquisitionPanel.Enabled = false;
                RecordButton.Enabled = false;
                PlayButton.Enabled = false;
                StopButton.Enabled = false;

                // ParameterGridView control.
                ParameterGridView.Enabled = false;

                // DisplayControl.
                DisplayControl.BackColor = BackColor;

                // PlayBack control.
                PlayBackControl.Visible = true;
                ExitPlaybackButton.Visible = true;

                // Status control.
                StatusControl.Visible = false;

                break;
        }
    }
}