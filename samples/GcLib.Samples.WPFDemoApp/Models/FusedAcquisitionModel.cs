using System;
using System.IO;
using System.Threading.Tasks;
using GcLib;
using Serilog;

namespace FusionViewer.Models;

/// <summary>
/// Acquires and records image data from a fused output channel, combining images from two input channel device datastreams.
/// </summary>
internal sealed class FusedAcquisitionModel : AcquisitionModel
{
    #region Fields

    /// <summary>
    /// Acquires and records image data from first input channel.
    /// </summary>
    private readonly AcquisitionModel _channel1;

    /// <summary>
    /// Acquires and records image data from second input channel.
    /// </summary>
    private readonly AcquisitionModel _channel2;

    #endregion

    #region Properties

    public override bool IsEnabled => _channel1.DeviceModel.IsConnected && _channel2.DeviceModel.IsConnected;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new model for the acquisition and recording of image data from a fused output channel, combining images from two input channel device datastreams.
    /// </summary>
    /// <param name="channel1">First input channel.</param>
    /// <param name="channel2">Second input hannel.</param>
    /// <param name="fusedImageModel">Fused output channel.</param>
    public FusedAcquisitionModel(AcquisitionModel channel1, AcquisitionModel channel2, FusedImageModel fusedImageModel) : base(null, fusedImageModel)
    {
        _channel1 = channel1;
        _channel2 = channel2;

        // Save processed data by default.
        SaveProcessedData = true;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Starts acquisition of input channel images.
    /// </summary>
    /// <param name="startGrabbing">Has no function (grabbing is always started automatically).</param>
    public override Task StartAcquisitionAsync(bool startGrabbing = false)
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Acquisition is already actively running on {ImageModel.ImageChannel}!");

        // Hook eventhandler to image announcing events in input channels.
        if (IsEnabled)
        {
            IsAcquiring = true;

            // Auto-grab images from channels.
            _channel1.ImageModel.ProcessedImageAdded += ImageModel_ProcessedImageAdded;
            _channel2.ImageModel.ProcessedImageAdded += ImageModel_ProcessedImageAdded;

            StartGrabbing();

            Log.Debug("Acquisition on {channel} started", ImageModel.ImageChannel.ToString());
        }

        return Task.CompletedTask;
    }

    public override void StartGrabbing() 
    {
        // Do nothing? Hook eventhandlers here instead?
        IsGrabbing = true;
    }

    /// <summary>
    /// Start recording image data to file.
    /// </summary>
    /// <param name="subString">Substring to add to file name.</param>
    /// <param name="startGrabbing">Has no function (grabbing is always started automatically).</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override Task StartRecordingAsync(string subString = "", bool startGrabbing = false)
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Acquisition is already actively running on {ImageModel.ImageChannel}!");

        if (IsEnabled)
        {
            // Create file path by adding substring to end of filename.
            string filePath = Path.GetDirectoryName(FilePath) + "\\" + Path.GetFileNameWithoutExtension(FilePath) + subString + ".bin";

            // Start writing to file.
            if (SaveRawData || SaveProcessedData)
                StartWriting(filePath);

            // Start acquisition.
            return StartAcquisitionAsync();
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops acquisition of input channel images.
    /// </summary>
    public override async Task StopAcquisitionAsync()
    {
        if (IsEnabled)
        {
            // Unhook eventhandlers.
            _channel1.ImageModel.ProcessedImageAdded -= ImageModel_ProcessedImageAdded;
            _channel2.ImageModel.ProcessedImageAdded -= ImageModel_ProcessedImageAdded;

            IsGrabbing = false;

            Log.Verbose("Grabbing stopped on channel {channel}", ImageModel.ImageChannel.ToString());

            IsAcquiring = false;

            // Stop recording (if writing).
            if (ImageWriter != null && ImageWriter.IsWriting)
                await StopWritingAsync();

            // Log information.
            Log.Debug("Acquisition on {channel} stopped", ImageModel.ImageChannel.ToString());
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Eventhandler to <see cref="ImageModel.ProcessedImageAdded"/> events.
    /// </summary>
    private void ImageModel_ProcessedImageAdded(object sender, BufferTransferredEventArgs e)
    {
        var imageModel = sender as ImageModel;

        // Re-direct added image data to corresponding channel.
        if (imageModel.ImageChannel == DisplayChannel.Channel1)
            (ImageModel as FusedImageModel).Channel1Image = e.Buffer;
        else if (imageModel.ImageChannel == DisplayChannel.Channel2)
            (ImageModel as FusedImageModel).Channel2Image = e.Buffer;
    }

    #endregion
}