using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GcLib;
using GcLib.Utilities.Threading;
using Serilog;

namespace ImagerViewer.Models;

/// <summary>
/// Grabs, acquires and records image data from a device (input channel) datastream.
/// </summary>
internal class AcquisitionModel : ObservableObject
{
    #region Fields

    // backing-fields
    private bool _saveRawData;
    private bool _saveProcessedData;
    private string _filePath;

    /// <summary>
    /// Image datastream from device.
    /// </summary>
    private GcDataStream _dataStream;

    /// <summary>
    /// Thread used for grabbing images from datastream.
    /// </summary>
    private readonly GcProcessingThread _imageGrabbingThread;

    #endregion

    #region Properties

    /// <summary>
    /// Device used as image acquisition source.
    /// </summary>
    public DeviceModel DeviceModel { get; }

    /// <summary>
    /// Storage and processing of acquired image data.
    /// </summary>
    public ImageModel ImageModel { get; }

    /// <summary>
    /// File path for saving image data.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    /// <summary>
    /// Setting indicating that raw image data will be saved to file.
    /// </summary>
    public bool SaveRawData
    {
        get => _saveRawData;
        set
        {
            _ = SetProperty(ref _saveRawData, value);
            if (_saveRawData)
                _ = SetProperty(ref _saveProcessedData, false, nameof(SaveProcessedData));
        }
    }

    /// <summary>
    /// Setting indicating that processed image data will be saved to file.
    /// </summary>
    public bool SaveProcessedData
    {
        get => _saveProcessedData;
        set
        {
            _ = SetProperty(ref _saveProcessedData, value);
            if (_saveProcessedData)
                _ = SetProperty(ref _saveRawData, false, nameof(SaveRawData));
        }
    }

    /// <summary>
    /// True if channel is currently acquiring.
    /// </summary>
    public bool IsAcquiring { get; protected set; }

    /// <summary>
    /// True if channel is currently grabbing.
    /// </summary>
    public virtual bool IsGrabbing { get; protected set; }

    /// <summary>
    /// True if channel is enabled for acquisition.
    /// </summary>
    public virtual bool IsEnabled => DeviceModel != null && DeviceModel.IsConnected;

    /// <summary>
    /// Writer of image data.
    /// </summary>
    protected GcBufferWriter ImageWriter { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new model of the acquisition and recording of image data from a device datastream.
    /// </summary>
    /// <param name="deviceModel">Device input source.</param>
    /// <param name="imageModel">Channel for storing images.</param>
    public AcquisitionModel(DeviceModel deviceModel, ImageModel imageModel)
    {
        DeviceModel = deviceModel;
        ImageModel = imageModel;

        // Save raw data by default.
        SaveRawData = true;

        // Initialize grabbing thread with device ID.
        if (deviceModel != null)
            _imageGrabbingThread = new GcProcessingThread();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Start acquisition in device.
    /// </summary>
    /// <param name="startGrabbing">True if image grabbing should be started automatically on the device datastream. If false, grabbing needs to be manually started using <see cref="StartGrabbing"/>.</param>
    /// <exception cref="InvalidOperationException"/>
    public virtual async Task StartAcquisitionAsync(bool startGrabbing = false)
    {
        if (DeviceModel.IsConnected == false)
            throw new InvalidOperationException($"No device is connected!");

        if (IsAcquiring)
            throw new InvalidOperationException($"Acquisition is already actively running!");

        // Open datastream.
        _dataStream = DeviceModel.Device.OpenDataStream();

        // Hook handler to events announcing dropped frames.
        _dataStream.FrameDropped += OnFrameDropped;

        // Hook handlers to events announcing acquisition events in device.
        DeviceModel.Device.AcquisitionStarted += OnAcquisitionStarted;
        DeviceModel.Device.AcquisitionStopped += OnAcquisitionStopped;
        DeviceModel.Device.AcquisitionAborted += OnAcquisitionAborted;

        IsAcquiring = true;

        try
        {
            // Start acquisition.
            await Task.Run(() => _dataStream.Start());

            // Auto-start grabbing.
            if (startGrabbing)
                StartGrabbing();

            // Log information.
            Log.Debug("Acquisition started");
        }
        catch (Exception ex)
        {
            // Stop acquisition and wait for it to finish.
            await StopAcquisitionAsync();

            Log.Error(ex, "Failed to start acquisition");
                
            throw new InvalidOperationException($"Failed to start acquisition: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts grabbing images from device datastream.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual void StartGrabbing()
    {
        if (IsAcquiring == false)
            throw new InvalidOperationException($"No acquisition is actively running!");

        if (IsGrabbing)
            throw new InvalidOperationException($"Grabbing has already been started!");

        // Hook handler to events announcing new buffers for processing.
        _imageGrabbingThread.BufferProcess += ImageModel.OnBufferProcess;

        // Start grabbing images using thread.
        _imageGrabbingThread.Start(_dataStream);

        IsGrabbing = true;

        Log.Verbose("Grabbing started");
    }

    /// <summary>
    /// Start recording image data to file.
    /// </summary>
    /// <param name="subString">Substring to add to file name.</param>
    /// <param name="startGrabbing">True if image grabbing should be started automatically on the device datastream. If false, grabbing needs to be manually started using <see cref="StartGrabbing"/>.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual Task StartRecordingAsync(string subString = "", bool startGrabbing = false)
    {
        if (DeviceModel.IsConnected == false)
            throw new InvalidOperationException($"No device is connected!");

        if (IsAcquiring)
            throw new InvalidOperationException($"Acquisition is already actively running!");

        // Create file path by adding substring to end of filename.
        string filePath = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(FilePath) + subString + ".bin";

        // Start writing to file.
        if (SaveRawData || SaveProcessedData)
            StartWriting(filePath);

        // Start acquisition.
        return StartAcquisitionAsync(startGrabbing);
    }

    /// <summary>
    /// Stop acquisition in device.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual async Task StopAcquisitionAsync()
    {
        if (DeviceModel.IsConnected == false)
            throw new InvalidOperationException($"No device is connected!");

        if (IsAcquiring == false)
            throw new InvalidOperationException($"No acquisition is actively running!");

        // Unregister eventhandlers.
        DeviceModel.Device.AcquisitionStarted -= OnAcquisitionStarted;
        DeviceModel.Device.AcquisitionStopped -= OnAcquisitionStopped;
        DeviceModel.Device.AcquisitionAborted -= OnAcquisitionAborted;
        _dataStream.FrameDropped -= OnFrameDropped;

        // Stop recording (if writing).
        if (ImageWriter != null && ImageWriter.IsWriting)
            await StopWritingAsync();

        // Stop grabbing images from datastream.
        _imageGrabbingThread.Stop();

        // Unhook eventhandler.
        _imageGrabbingThread.BufferProcess -= ImageModel.OnBufferProcess;

        IsGrabbing = false;

        Log.Verbose("Grabbing stopped");

        // Stop streaming image data from device.
        _dataStream.Stop();

        // Close datastream.
        _dataStream.Close();

        IsAcquiring = false;

        // Log information.
        Log.Debug("Acquisition stopped");
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Start writing image data to file.
    /// </summary>
    /// <param name="filePath">Path to file.</param>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="PathTooLongException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    protected void StartWriting(string filePath)
    {
        // Instantiate new writer with filepath.
        ImageWriter = new GcBufferWriter(filePath);

        // Hook appropriate image announcing event according to recording settings.
        if (SaveRawData)
            ImageModel.RawImageAdded += ImageWriter.OnBufferTransferred;
        else ImageModel.ProcessedImageAdded += ImageWriter.OnBufferTransferred;

        // Hook eventhandler to exceptions thrown while writing.
        ImageWriter.WritingAborted += OnWritingAborted;

        // Start writing images to disk.
        ImageWriter.Start();

        // Log information.
        Log.Debug("Recording to {file} started", ImageWriter.FilePath);
    }

    /// <summary>
    /// Stop writing image data to file.
    /// </summary>
    /// <returns></returns>
    protected async Task StopWritingAsync()
    {
        // Unhook image announcing events.
        ImageModel.RawImageAdded -= ImageWriter.OnBufferTransferred;
        ImageModel.ProcessedImageAdded -= ImageWriter.OnBufferTransferred;

        Log.Verbose("Stopping recording thread");

        // Stop writing images to disk.
        await ImageWriter.StopAsync();

        // Log information.
        Log.Debug("Recording to {file} finished ({buffers} buffers and {bytes} bytes written)", ImageWriter.FilePath, ImageWriter.BuffersWritten, ImageWriter.FileSize);

        // Unhook exception eventhandler.
        ImageWriter.WritingAborted -= OnWritingAborted;

        // Close writer and dispose resources.
        ImageWriter?.Dispose();
    }

    #endregion

    #region Events

    /// <summary>
    /// Event announcing that acquisition was started on the channel.
    /// </summary>
    public event EventHandler AcquisitionStarted;

    /// <summary>
    /// Event-invoking method, announcing that acquisition was started on the channel.
    /// </summary>
    private void OnAcquisitionStarted(object sender, EventArgs eventArgs)
    {
        AcquisitionStarted?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Event announcing that an acquisition was stopped on the channel.
    /// </summary>
    public event EventHandler AcquisitionStopped;

    /// <summary>
    /// Event-invoking method, announcing that an acquisition was stopped on the channel.
    /// </summary>
    private void OnAcquisitionStopped(object sender, EventArgs eventArgs)
    {
        AcquisitionStopped?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Event announcing that an acquisition was aborted on the channel, due to an error described in the event arguments.
    /// </summary>
    public event EventHandler<AcquisitionAbortedEventArgs> AcquisitionAborted;

    /// <summary>
    /// Event-invoking method, announcing that an acquisition was aborted on the channel, due to an error described in the event arguments.
    /// </summary>
    protected void OnAcquisitionAborted(object sender, AcquisitionAbortedEventArgs eventArgs)
    {
        // Handle error.
        AcquisitionAborted?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Event announcing that a frame has been dropped (lost) during acquisition.
    /// </summary>
    public event EventHandler<FrameDroppedEventArgs> FrameDropped;

    /// <summary>
    /// Event-invoking method, announcing that a frame has been dropped (lost) during acquisition.
    /// </summary>
    /// <param name="droppedFramesCount">Total number of frames dropped during acquisition.</param>
    private void OnFrameDropped(object sender, FrameDroppedEventArgs frameDroppedEventArgs)
    {
        FrameDropped?.Invoke(this, frameDroppedEventArgs);
    }

    /// <summary>
    ///  Event announcing that a recording was aborted on the channel, due to an error described in the event arguments.
    /// </summary>
    public event EventHandler<WritingAbortedEventArgs> RecordingAborted;

    /// <summary>
    /// Event-invoking method, announcing that a recording was aborted on the channel, due to an error described in the event arguments.
    /// </summary>
    protected void OnRecordingAborted(object sender, WritingAbortedEventArgs eventArgs)
    {
        RecordingAborted?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Eventhandler to <see cref="GcBufferWriter.WritingAborted"/> events.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="ex">Exception thrown.</param>
    protected void OnWritingAborted(object sender, WritingAbortedEventArgs eventArgs)
    {
        // Abort recording with error message.
        OnRecordingAborted(this, new WritingAbortedEventArgs($"Recording to {ImageWriter.FilePath} was aborted: {eventArgs.ErrorMessage}", eventArgs.Exception));
    }

    #endregion
}