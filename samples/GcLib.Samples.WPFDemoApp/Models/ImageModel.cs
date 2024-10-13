using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Threading;

namespace FusionViewer.Models;

/// <summary>
/// Processes and stores acquired image data.
/// </summary>
internal class ImageModel : ObservableRecipient, IXmlSerializable
{
    #region Fields

    // backing-fields
    private GcBuffer _rawImage;
    private GcBuffer _processedImage;
    private bool _flipHorizontal;
    private bool _flipVertical;
    private double _brightness;
    private bool _invertContrast;

    #endregion

    #region Properties

    /// <summary>
    /// Image channel that data belongs to.
    /// </summary>
    public DisplayChannel ImageChannel { get; }

    /// <summary>
    /// Raw (un-processed) image data.
    /// </summary>
    public GcBuffer RawImage
    {
        get => _rawImage;
        set
        {
            if (SetProperty(ref _rawImage, value))
            {
                if (_rawImage != null)
                {
                    OnRawImageAdded(_rawImage);

                    // Process image.
                    try
                    {
                        ProcessedImage = ProcessImage(_rawImage);
                    }
                    catch (Exception ex)
                    {
                        // Raise event.
                        OnProcessingException(ex);
                    }
                }
                else
                {
                    ProcessedImage = null;
                }

                OnImagesUpdated(); // move to ProcessedImage setter?
            }
        }
    }

    /// <summary>
    /// Processed image data.
    /// </summary>
    public GcBuffer ProcessedImage
    {
        get => _processedImage;
        private set
        {
            if (SetProperty(ref _processedImage, value))
            {
                if (_processedImage != null)
                {
                    OnProcessedImageAdded(_processedImage);
                }
            }
        }
    }

    /// <summary>
    /// Horizontal flipping of image.
    /// </summary>
    public bool FlipHorizontal
    {
        get => _flipHorizontal;
        set => SetProperty(ref _flipHorizontal, value);
    }

    /// <summary>
    /// Vertical flipping of image.
    /// </summary>
    public bool FlipVertical
    {
        get => _flipVertical;
        set => SetProperty(ref _flipVertical, value);
    }

    /// <summary>
    /// Image brightness.
    /// </summary>
    public double Brightness
    {
        get => _brightness;
        set => SetProperty(ref _brightness, value);
    }

    /// <summary>
    /// Invert contrast of image.
    /// </summary>
    public bool InvertContrast
    {
        get => _invertContrast;
        set => SetProperty(ref _invertContrast, value);
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
    public ImageModel(DisplayChannel channel)
    {
        ImageChannel = channel;

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

        InvertContrast = false;
        Brightness = 0;

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
        // ToDo: Use TryParse to improve robustness while reading xml configuration file.

        // Read flip settings.
        FlipHorizontal = reader.Name == nameof(FlipHorizontal) && bool.Parse(reader.ReadElementContentAsString());
        FlipVertical = reader.Name == nameof(FlipVertical) && bool.Parse(reader.ReadElementContentAsString());

        // Read if contrast inversion was used.
        InvertContrast = reader.Name == nameof(InvertContrast) && bool.Parse(reader.ReadElementContentAsString());

        // Read brightness setting.
        Brightness = reader.Name == nameof(Brightness) ? reader.ReadElementContentAsDouble() : 0;
    }

    /// <inheritdoc/>
    public virtual void WriteXml(XmlWriter writer)
    {
        // Write flip settings.
        writer.WriteElementString(nameof(FlipHorizontal), FlipHorizontal.ToString());
        writer.WriteElementString(nameof(FlipVertical), FlipVertical.ToString());

        // Write if contrast inversion is used.
        writer.WriteElementString(nameof(InvertContrast), InvertContrast.ToString());

        // Write brightness setting.
        writer.WriteElementString(nameof(Brightness), Brightness.ToString());
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
        // Convert to Mat (allocates new memory to keep raw image data unchanged).
        var mat = buffer.ToMat();

        // Convert 4-channel image to 3. 
        // ToDo: Create class or method for color conversion?
        // ToDo: Create Dictionary containing supported image formats and conversions needed (see Accord.Net)?

        if (mat.NumberOfChannels == 4)
            CvInvoke.CvtColor(mat, mat, ColorConversion.Bgra2Bgr);

        // Normalize (to 8 bits).
        if (EmguConverter.GetBitDepth(mat.Depth) > 8)
            CvInvoke.Normalize(src: mat, dst: mat, alpha: 0, beta: 255, normType: NormType.MinMax, dType: DepthType.Cv8U);

        // Adjust brightness.
        mat += Brightness;

        // Flip (if requested).
        if (FlipHorizontal)
            CvInvoke.Flip(mat, mat, FlipType.Horizontal);
        if (FlipVertical)
            CvInvoke.Flip(mat, mat, FlipType.Vertical);

        // Instantiate new output buffer (allocates new memory!).
        var output = new GcBuffer(mat, (uint)EmguConverter.GetMax(mat.Depth), buffer.FrameID, buffer.TimeStamp);

        // Dipose mat.
        mat.Dispose();

        return output; 
    }

    #endregion
}