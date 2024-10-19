using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FusionViewer.UserControls;
using FusionViewer.Utilities.Messages;
using GcLib;
using GcLib.Utilities.Imaging;

namespace FusionViewer.ViewModels;

/// <summary>
/// View model for displaying image histograms.
/// </summary>
internal sealed class HistogramViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private bool _isEnabled;
    private bool _showLiveHistogram;
    private bool _showGrid;
    private bool _showProcessed;
    private HistogramPlotType _selectedPlotType;
    private int _selectedHistogramSize;
    private ImageHistogram _histogram;

    /// <summary>
    /// Raw image source of image histogram.
    /// </summary>
    private GcBuffer _imageSource;

    /// <summary>
    /// Processed image source of image histogram.
    /// </summary>
    private GcBuffer _imageProcessed;

    /// <summary>
    /// Pre-allocated storage of histogram data.
    /// </summary>
    private double[,] _histogramData = new double[3, HistogramViewer.HistogramSizes.Max()];

    #endregion

    #region Properties

    /// <summary>
    /// Indicates that view is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set {
            if (SetProperty(ref _isEnabled, value))
                UpdateHistogram();
        }
    }

    /// <summary>
    /// Indicates that live histogram will be shown.
    /// </summary>
    public bool ShowLiveHistogram
    {
        get => _showLiveHistogram;
        set
        {
            if (SetProperty(ref _showLiveHistogram, value))
                UpdateHistogram();
        }
    }

    /// <summary>
    /// Selected histogram size (number of bins).
    /// </summary>
    public int SelectedHistogramSize
    {
        get => _selectedHistogramSize;
        set
        {
            if (SetProperty(ref _selectedHistogramSize, value))
                UpdateHistogram();
        }
    }

    /// <summary>
    /// Selected histogram plot type.
    /// </summary>
    public HistogramPlotType SelectedPlotType
    {
        get => _selectedPlotType;
        set
        {
            if (SetProperty(ref _selectedPlotType, value))
                UpdateHistogram();
        }
    }

    /// <summary>
    /// Enables/disables grid lines in histogram view.
    /// </summary>
    public bool ShowGrid
    {
        get => _showGrid;
        set {
            if (SetProperty(ref _showGrid, value))
                UpdateHistogram();
        }
    }

    /// <summary>
    /// Toggles between displaying histograms of raw (false) and processed images (true).
    /// </summary>
    public bool ShowProcessed
    {
        get => _showProcessed;
        set
        {
            if (SetProperty(ref _showProcessed, value))
                UpdateHistogram();
        }
    }

    /// <summary>
    /// Image histogram to be displayed.
    /// </summary>
    public ImageHistogram Histogram
    {
        get => _histogram;
        private set => SetProperty(ref _histogram, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes new view model for displaying image histograms.
    /// </summary>
    public HistogramViewModel()
    {
        // Set default histogram plot type.
        SelectedPlotType = HistogramPlotType.Fill;

        // Set default histogram size.
        SelectedHistogramSize = HistogramViewer.HistogramSizes[1];

        // Set default grid option.
        ShowGrid = true;

        // Set default image histogram.
        ShowProcessed = true;

        // View is enabled by default.
        IsEnabled = true;

        // Activate message receiving.
        IsActive = true;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Updates histogram with new image buffer data.
    /// </summary>
    private void UpdateHistogram()
    {
        if (ShowLiveHistogram == false)
            return;

        var buffer = ShowProcessed ? _imageProcessed : _imageSource;

        if (buffer == null)
        {
            Histogram = null;
            ShowLiveHistogram = false;
            return;
        }

        // Calculate histogram.
        buffer.ToMat().CalculateHistogram(bins: SelectedHistogramSize, maximumValue: buffer.PixelDynamicRangeMax, histogramData: ref _histogramData);

        // Update displayed histogram.
        Histogram = new ImageHistogram(_histogramData, buffer.PixelDynamicRangeMax, buffer.NumChannels);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient for messages over the "Busy" channel.
        Messenger.Register<PropertyChangedMessage<bool>, string>(this, "Busy", BusyHandler);

        // Register as recipient for messages of image updates.
        Messenger.Register<ImagesUpdatedMessage>(this, UpdateImages);
    }

    /// <summary>
    /// Message handler to messages of type <see cref="ImagesUpdatedMessage"/>.
    /// </summary>
    private void UpdateImages(object recipient, ImagesUpdatedMessage message)
    {
        // Updated sources for image histogram.
        _imageSource = message.SourceImage;
        _imageProcessed = message.ProcessedImage;

        // Update histogram.
        UpdateHistogram();
    }

    /// <summary>
    /// Handler of <see cref="PropertyChangedMessage{T}"/> messages broadcasted over the "Busy" channel.
    /// </summary>
    private void BusyHandler(object recipient, PropertyChangedMessage<bool> message)
    {
        // Disable view while loading a playback sequence.
        if (message.PropertyName == nameof(PlayBackViewModel.IsLoading))
        {
            IsEnabled = !message.NewValue;

            // Disable reception of image updates.
            if (message.NewValue) 
                Messenger.Unregister<ImagesUpdatedMessage>(this);
        }

        if (message.PropertyName == nameof(PlayBackViewModel.IsLoaded))
        {
            if (message.NewValue)
            {
                // Disable view during playback.
                IsEnabled = false;
                Histogram = null;
                ShowLiveHistogram = false;
            }
            else
            {
                // Enable view.
                IsEnabled = true;

                // Update histogram view with currently shown image.
                UpdateHistogram();

                // Re-enable reception of image updates.
                Messenger.Register<ImagesUpdatedMessage>(this, UpdateImages);
            }
        }
    }

    #endregion
}