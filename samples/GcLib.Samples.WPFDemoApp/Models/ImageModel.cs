using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Threading;

namespace WPFDemoApp.Models;

/// <summary>
/// Processes and stores acquired image data.
/// </summary>
internal class ImageModel : ObservableObject, IXmlSerializable
{
    #region Fields

    /// <summary>
    /// List of pixel formats supported.
    /// </summary>
    /// <remarks>This list defines the pixel formats that can be processed or produced. Derived classes may
    /// use this collection to determine format compatibility or to advertise supported formats to consumers.</remarks>
    private readonly List<PixelFormat> SupportedPixelFormats =
    [
        PixelFormat.Mono8,
        PixelFormat.Mono10,
        PixelFormat.Mono12,
        PixelFormat.Mono14,
        PixelFormat.Mono16,
        PixelFormat.BGR8,
        PixelFormat.BGR10,
        PixelFormat.BGR12,
        PixelFormat.BGR14,
        PixelFormat.BGR16,
        PixelFormat.RGB8,
        PixelFormat.RGB10,
        PixelFormat.RGB12,
        PixelFormat.RGB14,
        PixelFormat.RGB16,
        PixelFormat.BGRa8,
        PixelFormat.BGRa10,
        PixelFormat.BGRa12,
        PixelFormat.BGRa14,
        PixelFormat.BGRa16,
        PixelFormat.RGBa8,
        PixelFormat.RGBa10,
        PixelFormat.RGBa12,
        PixelFormat.RGBa14,
        PixelFormat.RGBa16
    ];

    /// <summary>
    /// Queue of buffers pending processing.
    /// </summary>
    private readonly ConcurrentQueue<GcBuffer> _pendingBuffers = new();

    /// <summary>
    /// Flag indicating if queued buffer processing is currently running.
    /// </summary>
    private int _isProcessingQueue;

    /// <summary>
    /// True if incoming buffers should be accepted for asynchronous processing.
    /// </summary>
    private volatile bool _isBufferProcessingEnabled = true;

    #endregion

    #region Properties

    /// <summary>
    /// Raw (un-processed) image data.
    /// </summary>
    public GcBuffer RawImage
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (field != null)
                {
                    OnRawImageAdded(field);

                    // Process image.
                    try
                    {
                        ProcessedImage = ProcessImage(field);
                    }
                    catch (Exception ex)
                    {
                        // Raise event.
                        OnProcessingException(ex);
                    }
                }
                else ProcessedImage = null;

                OnImagesUpdated();
            }
        }
    }

    /// <summary>
    /// Processed image data.
    /// </summary>
    public GcBuffer ProcessedImage
    {
        get;
        private set
        {
            if (SetProperty(ref field, value) && field != null)
                OnProcessedImageAdded(field);
        }
    }

    /// <summary>
    /// Horizontal flipping of image.
    /// </summary>
    public bool FlipHorizontal
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Update image.
                if (RawImage != null)
                {
                    ProcessedImage = ProcessImage(RawImage);
                    OnImagesUpdated();
                }
            }
        }
    }

    /// <summary>
    /// Vertical flipping of image.
    /// </summary>
    public bool FlipVertical
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Update image.
                if (RawImage != null)
                {
                    ProcessedImage = ProcessImage(RawImage);
                    OnImagesUpdated();
                }
            }
        }
    }

    /// <summary>
    /// Image brightness (0 - 100).
    /// </summary>
    public int Brightness
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Update image.
                if (RawImage != null)
                {
                    ProcessedImage = ProcessImage(RawImage);
                    OnImagesUpdated();
                }
            }
        }
    }

    /// <summary>
    /// Image contrast (0 - 100).
    /// </summary>
    public int Contrast
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Update image.
                if (RawImage != null)
                {
                    ProcessedImage = ProcessImage(RawImage);
                    OnImagesUpdated();
                }
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event announcing that a raw image has been added, with the image in the event arguments.
    /// </summary>
    public event EventHandler<BufferTransferredEventArgs> RawImageAdded;

    /// <summary>
    /// Raises event that a raw image has been added.
    /// </summary>
    /// <param name="buffer">Image added.</param>
    protected void OnRawImageAdded(GcBuffer buffer)
    {
        RawImageAdded?.Invoke(this, new BufferTransferredEventArgs(buffer));
    }

    /// <summary>
    /// Event announcing that a processed image has been added, with the image in the event arguments.
    /// </summary>
    public event EventHandler<BufferTransferredEventArgs> ProcessedImageAdded;

    /// <summary>
    /// Raises event that a processed image has been added.
    /// </summary>
    /// <param name="buffer">Image added.</param>
    protected void OnProcessedImageAdded(GcBuffer buffer)
    {
        ProcessedImageAdded?.Invoke(this, new BufferTransferredEventArgs(buffer));
    }

    /// <summary>
    /// Event announcing that images (both raw and processed) have been updated.
    /// </summary>
    public event EventHandler ImagesUpdated;

    /// <summary>
    /// Raises event that images (both raw and processed) have been updated.
    /// </summary>
    protected void OnImagesUpdated()
    {
        ImagesUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Eventhandler to event raised when a buffer is ready for processing.
    /// </summary>
    /// <param name="sender">Processing thread.</param>
    /// <param name="buffer">Image buffer to be processed/displayed.</param>
    public void OnBufferProcess(object sender, GcBuffer buffer)
    {
        if (sender is GcProcessingThread)
            RawImage = buffer;
    }

    /// <summary>
    /// Event announcing that an exception was thrown while processing an image.
    /// </summary>
    public event EventHandler<Exception> ProcessingException;

    /// <summary>
    /// Raises event announcing that an exception was thrown while processing an image.
    /// </summary>
    /// <param name="ex">Exception thrown.</param>
    protected void OnProcessingException(Exception ex)
    {
        ProcessingException?.Invoke(this, ex);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new model for the storage and processing of image data.
    /// </summary>
    /// <param name="channel">Channel owner.</param>
    public ImageModel()
    {
        InitializeSettings();


    }

    #endregion

    #region Public methods

    /// <summary>
    /// Initializes processing settings.
    /// </summary>
    public virtual void InitializeSettings()
    {
        // Disable logging.
        App.IsLoggingEnabled = false;

        FlipHorizontal = false;
        FlipVertical = false;

        Brightness = 50;
        Contrast = 50;

        // Re-enable logging.
        App.IsLoggingEnabled = true;
    }

    /// <summary>
    /// Clears both raw and processed image data.
    /// </summary>
    public virtual void ClearImages()
    {
        RawImage = null;
        ProcessedImage = null;
    }

    /// <inheritdoc/>
    public XmlSchema GetSchema()
    {
        return null;
    }

    /// <inheritdoc/>
    public virtual void ReadXml(XmlReader reader)
    {
        // Read flip settings.
        FlipHorizontal = reader.Name == nameof(FlipHorizontal) && bool.Parse(reader.ReadElementContentAsString());
        FlipVertical = reader.Name == nameof(FlipVertical) && bool.Parse(reader.ReadElementContentAsString());

        // Read brightness setting.
        Brightness = reader.Name == nameof(Brightness) ? reader.ReadElementContentAsInt() : 50;

        // Read contrast setting.
        Contrast = reader.Name == nameof(Contrast) ? reader.ReadElementContentAsInt() : 50;
    }

    /// <inheritdoc/>
    public virtual void WriteXml(XmlWriter writer)
    {
        // Write flip settings.
        writer.WriteElementString(nameof(FlipHorizontal), FlipHorizontal.ToString());
        writer.WriteElementString(nameof(FlipVertical), FlipVertical.ToString());

        // Write brightness setting.
        writer.WriteElementString(nameof(Brightness), Brightness.ToString());

        // Write contrast setting.
        writer.WriteElementString(nameof(Contrast), Contrast.ToString());
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Process image.
    /// </summary>
    /// <param name="buffer">Input image.</param>
    /// <returns>Processed output image.</returns>
    protected virtual GcBuffer ProcessImage(GcBuffer buffer)
    {
        if (!SupportedPixelFormats.Contains(buffer.PixelFormat))
            throw new NotSupportedException($"Pixel format {buffer.PixelFormat} is not supported!");

        // Convert to Mat (allocates new memory to keep raw image data unchanged).
        var mat = buffer.ToMat();

        // Convert to BGR(a) if necessary.
        if (mat.NumberOfChannels > 1)
        {
            // Get color order of input image.
            var colorOrder = GenICamHelper.GetColorOrder(buffer.PixelFormat);

            // Convert to BGR(a) as Emgu.CV uses BGR color order by default.
            // Convert 3-channel to BGR.
            if (mat.NumberOfChannels == 3 && colorOrder == ColorOrder.RGB)
                CvInvoke.CvtColor(mat, mat, ColorConversion.Rgb2Bgr);

            // Convert 4-channel image to 3 (BGR).
            if (mat.NumberOfChannels == 4)
                if (colorOrder == ColorOrder.RGB)
                    CvInvoke.CvtColor(mat, mat, ColorConversion.Rgba2Bgr); // Convert RGBA to BGR (skip alpha channel). 
                else CvInvoke.CvtColor(mat, mat, ColorConversion.Bgra2Bgr);  // Skip alpha channel.
        }

        // Normalize (to 8 bits).
        if (EmguHelper.GetBitDepth(mat.Depth) > 8)
            CvInvoke.Normalize(src: mat, dst: mat, alpha: 0, beta: 255, normType: NormType.MinMax, dType: DepthType.Cv8U);

        // Adjust brightness.
        mat += 5.1 * Brightness - 255;
        //mat *= Contrast / 50d;
        mat *= Contrast * Contrast * 4d / 10000;

        // Flip (if requested).
        if (FlipHorizontal)
            CvInvoke.Flip(mat, mat, FlipType.Horizontal);
        if (FlipVertical)
            CvInvoke.Flip(mat, mat, FlipType.Vertical);

        // Instantiate new output buffer (allocates new memory!).
        var output = new GcBuffer(mat, mat.NumberOfChannels == 3 ? PixelFormat.BGR8 : PixelFormat.Mono8, buffer.FrameID, buffer.TimeStamp);

        // Dipose mat.
        mat.Dispose();

        return output;
    }

    /// <summary>
    /// Handles a buffer transfer notification by queuing the transferred buffer for asynchronous processing when buffer
    /// processing is enabled.
    /// </summary>
    /// <remarks>Returns without action when buffer processing is disabled or when no buffer is provided.
    /// Starts queue processing only when no processing operation is currently running.</remarks>
    /// <param name="sender">Source of the buffer transfer event.</param>
    /// <param name="e">Event data that contains the transferred buffer.</param>
    internal void OnBufferTransferred(object sender, BufferTransferredEventArgs e)
    {
        if (_isBufferProcessingEnabled == false || e.Buffer == null)
            return;

        // Queue buffer for processing.
        _pendingBuffers.Enqueue(e.Buffer);

        // Start processing queue if not already running.
        if (Interlocked.CompareExchange(ref _isProcessingQueue, 1, 0) == 0)
            _ = Task.Run(ProcessQueuedBuffersAsync);
    }

    /// <summary>
    /// Enables acceptance of incoming buffers for processing.
    /// </summary>
    internal void StartBufferProcessing()
    {
        _isBufferProcessingEnabled = true;
    }

    /// <summary>
    /// Disables acceptance of incoming buffers and discards queued buffers.
    /// </summary>
    internal void StopBufferProcessing()
    {
        _isBufferProcessingEnabled = false;

        while (_pendingBuffers.TryDequeue(out _))
        {
        }
    }

    /// <summary>
    /// Processes queued buffers sequentially on a background worker.
    /// </summary>
    private void ProcessQueuedBuffersAsync()
    {
        try
        {
            // Process all queued buffers.
            while (_isBufferProcessingEnabled && _pendingBuffers.TryDequeue(out var buffer))
                RawImage = buffer;
        }
        finally
        {
            // Reset processing flag to allow future queue processing.
            Interlocked.Exchange(ref _isProcessingQueue, 0);

            // If new buffers were queued while processing, start processing again.
            if (_isBufferProcessingEnabled && _pendingBuffers.IsEmpty == false && Interlocked.CompareExchange(ref _isProcessingQueue, 1, 0) == 0)
                _ = Task.Run(ProcessQueuedBuffersAsync);
        }
    }

    #endregion
}