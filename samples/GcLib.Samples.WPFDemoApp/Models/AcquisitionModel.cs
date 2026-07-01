using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GcLib;
using GcLib.FileIO;
using GcLib.Utilities.Threading;
using Serilog;

namespace ImagerViewer.Models;

/// <summary>
/// Grabs, acquires and records image data from a device (input channel) datastream.
/// </summary>
internal partial class AcquisitionModel : ObservableObject
{
    #region Fields

    /// <summary>
    /// Image datastream from device.
    /// </summary>
    private GcDataStream _dataStream;

    /// <summary>
    /// Thread used for grabbing images from datastream.
    /// </summary>
    private readonly GcProcessingThread _imageGrabbingThread;

    /// <summary>
    /// Storage and processing of acquired image data.
    /// </summary>
    private readonly ImageModel _imageModel;

    /// <summary>
    /// Writer of image buffer data.
    /// </summary>
    private GcBufferWriter _bufferWriter;

    #endregion

    #region Properties

    /// <summary>
    /// Device used as image acquisition source.
    /// </summary>
    public DeviceModel DeviceModel { get; }

    /// <summary>
    /// File path for saving binary image data.
    /// </summary>
    [ObservableProperty]
    public partial string BinaryFilePath { get; set; }

    /// <summary>
    /// File path for saving video.
    /// </summary>
    [ObservableProperty]
    public partial string VideoFolderPath { get; set; }

    /// <summary>
    /// Setting indicating that raw binary image data will be saved to file.
    /// </summary>
    public bool SaveRawData
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (field)
                    SaveProcessedData = false;
            }
        }
    }

    /// <summary>
    /// Setting indicating that processed binary image data will be saved to file.
    /// </summary>
    public bool SaveProcessedData
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (field)
                    SaveRawData = false;
            }
        }
    }

    /// <summary>
    /// Setting indicating that video will be saved to file.
    /// </summary>
    [ObservableProperty]
    public partial bool SaveVideo { get; set; }

    /// <summary>
    /// True if channel is currently acquiring.
    /// </summary>
    public bool IsAcquiring { get; protected set; }

    /// <summary>
    /// Available video codecs.
    /// </summary>
    public static List<VideoWriter.CODEC> Codecs => [.. Enum.GetValues<VideoWriter.CODEC>()];

    /// <summary>
    /// Selected video codec.
    /// </summary>
    [ObservableProperty]
    public partial VideoWriter.CODEC SelectedCodec { get; set; }

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
        _imageModel = imageModel;

        // Save raw data by default.
        SaveRawData = true;

        // Default codec.
        SelectedCodec = VideoWriter.CODEC.MJPEG;

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
    public virtual async Task StartAcquisitionAsync()
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

            // Hook handler to events announcing new buffers for processing.
            _imageGrabbingThread.BufferProcess += _imageModel.OnBufferProcess;

            // Start grabbing images using thread.
            _imageGrabbingThread.Start(_dataStream);

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
        string filePath = Path.GetDirectoryName(BinaryFilePath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(BinaryFilePath) + subString + ".bin";

        // Start writing to file.
        if (SaveRawData || SaveProcessedData)
            StartWriting(filePath);

        // Save video (if selected). Videos will be saved using auto-generated filenames based on current date and time.
        if (SaveVideo)
            StartVideoWriting(VideoFolderPath + Path.DirectorySeparatorChar + "Video" + $"_{DateTime.Now:yyyyMMddHHmmss}" + ".mp4");

        // Start acquisition.
        return StartAcquisitionAsync();
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
        if (_bufferWriter != null && _bufferWriter.IsWriting)
            await StopWritingAsync();

        if (_videoWriter != null && _videoWriter.IsWriting)
            await StopVideoWriting();

        // Stop grabbing images from datastream.
        _imageGrabbingThread.Stop();

        // Unhook grabbing eventhandler.
        _imageGrabbingThread.BufferProcess -= _imageModel.OnBufferProcess;

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
        _bufferWriter = new GcBufferWriter(filePath);

        // Hook appropriate image announcing event according to recording settings.
        if (SaveRawData)
            _imageModel.RawImageAdded += _bufferWriter.OnBufferTransferred;
        else _imageModel.ProcessedImageAdded += _bufferWriter.OnBufferTransferred;

        // Hook eventhandler to exceptions thrown while writing.
        _bufferWriter.WritingAborted += OnWritingAborted;

        // Start writing images to disk.
        _bufferWriter.Start();

        // Log information.
        Log.Debug("Recording to {file} started", _bufferWriter.FilePath);
    }

    /// <summary>
    /// Stop writing image data to file.
    /// </summary>
    /// <returns></returns>
    protected async Task StopWritingAsync()
    {
        // Unhook image announcing events.
        _imageModel.RawImageAdded -= _bufferWriter.OnBufferTransferred;
        _imageModel.ProcessedImageAdded -= _bufferWriter.OnBufferTransferred;

        // Stop writing images to disk.
        await _bufferWriter.StopAsync();

        // Log information.
        Log.Debug("Recording to {file} finished ({buffers} buffers and {bytes} bytes written)", _bufferWriter.FilePath, _bufferWriter.BuffersWritten, _bufferWriter.FileSize);

        // Unhook exception eventhandler.
        _bufferWriter.WritingAborted -= OnWritingAborted;

        // Close writer and dispose resources.
        _bufferWriter?.Dispose();
    }

    #endregion

    #region Video recording

    /// <summary>
    /// Converts buffers to video frames and writes to disk.
    /// </summary>
    private VideoWriter _videoWriter;

    /// <summary>
    /// Start writing to video file.
    /// </summary>
    /// <param name="filePath"></param>
    protected void StartVideoWriting(string filePath)
    {
        _videoWriter = new VideoWriter(filePath, 0.0, SelectedCodec);
        _imageModel.ProcessedImageAdded += _videoWriter.OnBufferTransferred;
        _videoWriter.WritingAborted += OnWritingAborted;
        _videoWriter.Start();

        // Log information.
        Log.Debug("Recording to {file} started", _videoWriter.FilePath);
    }

    /// <summary>
    /// Stop writing to video file.
    /// </summary>
    protected async Task StopVideoWriting()
    {
        // Unhook image announcing events.
        _imageModel.ProcessedImageAdded -= _videoWriter.OnBufferTransferred;

        // Stop writing frames to video.
        await _videoWriter.StopAsync();

        // Log information.
        Log.Debug("Recording to {file} finished ({buffers} frames written)", _videoWriter.FilePath, _videoWriter.FramesWritten);

        // Unhook exception eventhandler.
        _videoWriter.WritingAborted -= OnWritingAborted;

        // Close writer and dispose resources.
        _videoWriter.Dispose();
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
    protected void OnWritingAborted(object sender, WritingAbortedEventArgs eventArgs)
    {
        // Abort recording with error message.
        OnRecordingAborted(this, new WritingAbortedEventArgs($"Recording to {_bufferWriter.FilePath} was aborted: {eventArgs.ErrorMessage}", eventArgs.Exception));
    }

    #endregion
}