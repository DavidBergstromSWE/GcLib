using System;
using System.IO;
using System.Windows.Forms;
using GcLib;
using Serilog;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp
{
    /// <summary>
    /// Event-handling method to Play button click events. Starts streaming and displaying images in UI.
    /// </summary>
    private void PlayButton_Click(object sender, EventArgs e)
    {
        try
        {
            // Reset display control stats.
            DisplayControl.ResetStats();

            // Start display thread.
            _displayThread.BufferProcess += OnBufferDisplay;
            _displayThread.Start(_dataStream);

            // Retrieve selected acquisition mode.
            var acquisitionMode = Enum.Parse<AcquisitionMode>(AcquisitionModeComboBox.SelectedItem.ToString(), false);
            
            // Start acquisition on datastream.
            switch (acquisitionMode)
            {
                case AcquisitionMode.Continuous:
                default:
                    _dataStream.Start();
                    break;
                case AcquisitionMode.MultiFrame:
                    ulong frameCount = Convert.ToUInt64(AcquisitionFrameCountTextBox.Text);
                    _dataStream.Start(frameCount);
                    break;
                case AcquisitionMode.SingleFrame:
                    _dataStream.Start(1);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Stop display thread (if started).
            _displayThread.Stop();
            _displayThread.BufferProcess -= OnBufferDisplay;

            _ = MessageBox.Show(ex.Message, "Acquisition error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Update UI state.
        SetUIState(UIState.AcquisitionState);
    }

    /// <summary>
    /// Event-handling method to Rec button click events. Starts streaming, displaying and recording images.
    /// </summary>
    private void RecordButton_Click(object sender, EventArgs e)
    {
        try
        {
            // Start recording images to file.
            StartRecording();

            // Reset display control stats.
            DisplayControl.ResetStats();

            // Start display thread.
            _displayThread.BufferProcess += OnBufferDisplay;
            _displayThread.Start(_dataStream);

            // Retrieve selected acquisition mode.
            var acquisitionMode = Enum.Parse<AcquisitionMode>(AcquisitionModeComboBox.SelectedItem.ToString(), false);

            // Start acquisition on datastream.
            switch (acquisitionMode)
            {
                case AcquisitionMode.Continuous:
                default:
                    _dataStream.Start();
                    break;
                case AcquisitionMode.MultiFrame:
                    _dataStream.Start(Convert.ToUInt64(AcquisitionFrameCountTextBox.Text));
                    break;
                case AcquisitionMode.SingleFrame:
                    _dataStream.Start(1);
                    break;
            }

            Log.Information("Acquisition started");
        }
        catch (IOException ex)
        {
            _ = MessageBox.Show(ex.Message, "Acquisition error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        catch (Exception ex)
        {
            // Stop recording (if started).
            StopRecording();

            // Stop display thread (if started).
            _displayThread.Stop();
            _displayThread.BufferProcess -= OnBufferDisplay;

            _ = MessageBox.Show(ex.Message, "Acquisition error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Update UI state.
        SetUIState(UIState.AcquisitionState);
    }

    /// <summary>
    /// Event-handling method to Stop button click events. Stops streaming, displaying and recording images.
    /// </summary>
    private void StopButton_Click(object sender, EventArgs e)
    {
        // Stop acquisition on datastream.
        _dataStream.Stop();

        // Stop display thread.
        // ToDo: why does not aWait = true work here for XiCam (but does for OnStreamingStopping)?
        _displayThread.Stop();
        //await Task.Run(() => _displayThread.Stop(aWait: true));
        _displayThread.BufferProcess -= OnBufferDisplay;

        Log.Information("Acquisition manually stopped");

        // Stop recording images to file (if started).
        StopRecording();

        // Update UI state.
        SetUIState(UIState.ReadyState);
    }

    /// <summary>
    /// Event-handling method to <see cref="GcDevice.AcquisitionStopped"/> events, handles SingleFrame and MultiFrame acquisition scenarios.
    /// </summary>
    private void OnAcquisitionStopped(object sender, EventArgs e)
    {
        // Stop acquisition on datastream.
        _dataStream.Stop();

        // Stop display thread.
        // ToDo: test aWait = true? (without WaitComplete)
        _displayThread.Stop();
        _displayThread.BufferProcess -= OnBufferDisplay;

        Log.Information("Acquisition automatically stopped");

        // Stop recording images to file (if started).
        StopRecording();

        // Update UI state.
        if (InvokeRequired)
        {
            if (_stopInvoking == false)
            {
                // Invoke delegate.
                _invokeInProgress = true;
                _ = Invoke(new Action<UIState>(SetUIState), UIState.ReadyState);
                _invokeInProgress = false;
            }
        }
        else
        {
            SetUIState(UIState.ReadyState);
        };

        // Wait for display thread to complete.
        //_displayThread.WaitComplete();
    }

    /// <summary>
    /// Starts writing images to file.
    /// </summary>
    /// <param name="filePath">Filepath to write to (relative or absolute path).</param>
    private void StartRecording()
    {
        // Verify validity of filepath.
        string filePath = SaveImagesTextBox.Text;
        if (FileIsWritable(filePath) == false)
        {
            return;
        }

        // Instantiate ImageWriter with specified filepath.
        _imageWriter = new GcBufferWriter(filePath);

        // Subcribe to events.
        _dataStream.BufferTransferred += _imageWriter.OnBufferTransferred;
        _camera.AcquisitionStopped += OnAcquisitionStopped;

        // Start recording images from datastream.
        _imageWriter.Start();
    }

    /// <summary>
    /// Stops writing images to file.
    /// </summary>
    private void StopRecording()
    {
        if (_imageWriter == null || _imageWriter.IsDisposed)
            return;

        // Stop and close ImageWriter.
        if (_imageWriter.IsWriting)
        {
            _imageWriter.StopAsync().Wait();

            // Unsubcribe from events.
            _dataStream.BufferTransferred -= _imageWriter.OnBufferTransferred;
            _camera.AcquisitionStopped -= OnAcquisitionStopped;

            Log.Information("Recording stopped");
        }
        _imageWriter.Dispose();

        _imageWriter = null;
    }

    /// <summary>
    /// Checks that user selected file path is OK and can be written to.
    /// </summary>
    /// <param name="filePath">Filepath to write to (relative or absolute path).</param>
    /// <returns>True if file is writable, false if not.</returns>
    private bool FileIsWritable(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // Check if file is available to write to.
                if (FileIsLocked(filePath, FileAccess.Write))
                {
                    throw new IOException($"Unable to write to file '{filePath}'! " + "File is already opened in another application!");
                }
                else
                // Give user a warning if file already exists.
                {
                    DialogResult dialogResult = MessageBox.Show(this, "File already exists! Do you want to overwrite existing file?", "FileIO Alert!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    return dialogResult == DialogResult.No
                        ? throw new IOException($"Cannot use the file {filePath} since it already exists and overwriting was denied!")
                        : true;
                }
            }

            // Check if directory exists.
            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
                throw new DirectoryNotFoundException($"Directory {Path.GetDirectoryName(filePath)} not found!");
        }
        catch (DirectoryNotFoundException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }

        // New file in an existing directory.
        return true;
    }

    /// <summary>
    /// Check if file is locked for a given kind of access.
    /// </summary>
    /// <param name="filePath">Path to file.</param>
    /// <param name="file_access">File access permission (read, write, read/write).</param>
    /// <returns>True if the file is locked for the indicated access.</returns>
    private static bool FileIsLocked(string filePath, FileAccess file_access)
    {
        // Try to open the file with the indicated access.
        try
        {
            var fs = new FileStream(filePath, FileMode.Open, file_access);
            fs.Close();
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Opens a dialogue window for user selection of a file to save images to.
    /// </summary>
    private void SaveImagesButton_Click(object sender, EventArgs e)
    {
        SaveFileDialog.Filter = "bin files (*.bin)|*.bin";
        SaveFileDialog.Title = "Choose an existing file or type in a new filename";
        SaveFileDialog.InitialDirectory = @"C:\testdata";
        SaveFileDialog.FileName = $"{DateTime.Now:yyyyMMddHHmmssfff}.bin"; // generates a filename based on current time and date
        DialogResult dialogResult = SaveFileDialog.ShowDialog();

        if (dialogResult == DialogResult.OK)
        {
            SaveImagesTextBox.Text = SaveFileDialog.FileName;
        }
    }

    /// <summary>
    /// Initialize AcquisitionPanel with default settings.
    /// </summary>
    private void InitializeAcquisitionPanel()
    {
        // Enable all acquisition modes in enum.
        AcquisitionModeComboBox.DataSource = Enum.GetNames<AcquisitionMode>();
        AcquisitionModeComboBox.SelectedItem = Enum.GetName(AcquisitionMode.Continuous);
        AcquisitionModeComboBox.Enabled = true;

        // Set acquisition frame count to a default value.
        AcquisitionFrameCountTextBox.Text = "10";
        AcquisitionFrameCountTextBox.Enabled = true;
    }
}