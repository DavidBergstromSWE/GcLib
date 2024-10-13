using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FusionViewer.Utilities.Imaging;
using ScottPlot;
using Theme = FusionViewer.Utilities.Themes.Theme;

namespace FusionViewer.UserControls;

/// <summary>
/// Type of plot for drawing histogram bins.
/// </summary>
public enum HistogramPlotType
{
    /// <summary>
    /// Bins are drawn as lines.
    /// </summary>
    Line,
    /// <summary>
    /// Bins are drawn as (empty) bars.
    /// </summary>
    Bar,
    /// <summary>
    /// Bins are drawn as filled bars.
    /// </summary>
    Fill
}

/// <summary>
/// Control for displaying image histograms.
/// </summary>
public partial class HistogramViewer : UserControl, INotifyPropertyChanged
{
    #region Private fields

    // backing-fields
    private bool _isMultiChannel;
    private bool _showRed;
    private bool _showGreen;
    private bool _showBlue;
    private bool _containsData;

    /// <summary>
    /// Array of BGR colors.
    /// </summary>
    private readonly System.Drawing.Color[] BGR = [System.Drawing.Color.Blue, System.Drawing.Color.Green, System.Drawing.Color.Red,];

    /// <summary>
    /// Histogram data, stored as a two-dimensional array with image channel as first dimension (BGR) and histogram data as second dimension.
    /// </summary>
    private double[,] _histogramData;

    /// <summary>
    /// Maximum value of histogram range.
    /// </summary>
    private uint _histogramMaxValue = 255;

    /// <summary>
    /// Number of image channels in histogram data.
    /// </summary>
    private int _numChannels;

    /// <summary>
    /// Timer used for rendering histogram in UI.
    /// </summary>
    private readonly DispatcherTimer _renderingTimer;

    /// <summary>
    /// Rendering interval in milliseconds. (make dependencyproperty?)
    /// </summary>
    private readonly int _renderingInterval = 40;

    /// <summary>
    /// True if new data has been received and plotted.
    /// </summary>
    private bool _isPlotted;

    /// <summary>
    /// Foreground color.
    /// </summary>
    private System.Drawing.Color _foregroundColor;

    /// <summary>
    /// Background color.
    /// </summary>
    private System.Drawing.Color _backgroundColor;

    #endregion

    #region Public fields

    public static readonly DependencyProperty HistogramProperty = DependencyProperty.Register(nameof(Histogram), typeof(ImageHistogram), typeof(HistogramViewer), new PropertyMetadata(null, OnHistogramChanged));
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(HistogramViewer), new PropertyMetadata(true, OnPlotSettingsChanged));
    public static readonly DependencyProperty SelectedPlotTypeProperty = DependencyProperty.Register(nameof(SelectedPlotType), typeof(HistogramPlotType), typeof(HistogramViewer), new PropertyMetadata(HistogramPlotType.Fill, OnPlotSettingsChanged));
    public static readonly DependencyProperty SelectedHistSizeProperty = DependencyProperty.Register(nameof(SelectedHistSize), typeof(int), typeof(HistogramViewer), new PropertyMetadata(64, OnPlotSettingsChanged));
    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(nameof(Theme), typeof(Theme), typeof(HistogramViewer), new PropertyMetadata(new Theme(), OnThemeChanged));

    #endregion

    #region DependencyProperties

    /// <summary>
    /// Image histogram.
    /// </summary>
    public ImageHistogram Histogram
    {
        get { return (ImageHistogram)GetValue(HistogramProperty); }
        set { SetValue(HistogramProperty, value); }
    }

    /// <summary>
    /// Enables/disables grid lines in histogram view.
    /// </summary>
    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// Selected histogram plot type.
    /// </summary>
    public HistogramPlotType SelectedPlotType
    {
        get => (HistogramPlotType)GetValue(SelectedPlotTypeProperty);
        set => SetValue(SelectedPlotTypeProperty, value);
    }

    /// <summary>
    /// Selected histogram size (number of bins).
    /// </summary>
    public int SelectedHistSize
    {
        get => (int)GetValue(SelectedHistSizeProperty);
        set => SetValue(SelectedHistSizeProperty, value);
    }

    /// <summary>
    /// Theme to be used when plotting.
    /// </summary>
    public Theme Theme
    {
        get { return (Theme)GetValue(ThemeProperty); }
        set { SetValue(ThemeProperty, value); }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Available histogram plot types.
    /// </summary>
    public static HistogramPlotType[] PlotTypes => Enum.GetValues<HistogramPlotType>();

    /// <summary>
    /// Available histogram sizes (number of bins).
    /// </summary>
    public static List<int> HistogramSizes => [32, 64, 128, 256];

    /// <summary>
    /// Indicates that histogram contains multi-channel data.
    /// </summary>
    public bool IsMultiChannel
    {
        get => _isMultiChannel;
        private set
        {
            _isMultiChannel = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Enables/disables red color channel in histogram view.
    /// </summary>
    public bool ShowRed
    {
        get => _showRed;
        set
        {
            _showRed = value;
            OnPropertyChanged();
            RefreshPlot();
        }
    }

    /// <summary>
    /// Enables/disables green color channel in histogram view.
    /// </summary>
    public bool ShowGreen
    {
        get => _showGreen;
        set
        {
            _showGreen = value;
            OnPropertyChanged();
            RefreshPlot();
        }
    }

    /// <summary>
    /// Enables/disables blue color channel in histogram view.
    /// </summary>
    public bool ShowBlue
    {
        get => _showBlue;
        set
        {
            _showBlue = value;
            OnPropertyChanged();
            RefreshPlot();
        }
    }

    /// <summary>
    /// True if histogram contains data.
    /// </summary>
    public bool ContainsData
    {
        get => _containsData;
        private set
        {
            _containsData = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates new control for displaying image histograms.
    /// </summary>
    public HistogramViewer()
    {
        InitializeComponent();

        // Initialize timer for rendering histogram plots.
        _renderingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_renderingInterval) };

        // Hook eventhandler to timer tick events.
        _renderingTimer.Tick += OnDispatcherTimerTick;

        // Initialize data.
        _histogramData = new double[3, HistogramSizes.Max()];

        // Show all RGB channels by default.
        ShowRed = ShowGreen = ShowBlue = true;

        // Initialize histogram control.
        wpfPlot.Configuration.DoubleClickBenchmark = true;
        wpfPlot.Configuration.Pan = false;
        wpfPlot.Configuration.Zoom = false;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Refreshes histogram plot using current settings.
    /// </summary>
    private void RefreshPlot()
    {
        if (Histogram != null)
        {
            // Clear previous plot.
            wpfPlot.Plot.Clear();

            // Plot histogram from current data.
            PlotHistogram();

            wpfPlot.Refresh();

            // Do not re-render this data (as it has been plotted). 
            _isPlotted = true;
        }
    }

    /// <summary>
    /// Plots currently stored histogram data.
    /// </summary>
    private void PlotHistogram()
    {
        // Prevent new rendering (if plotting is done from separate thread).
        wpfPlot.Plot.RenderLock();

        // Specify bin locations.
        double[] xs = DataGen.Consecutive<double>(pointCount: SelectedHistSize, spacing: (_histogramMaxValue + 1) / SelectedHistSize, offset: (_histogramMaxValue + 1) / 2 / SelectedHistSize);

        // Plot histogram data for all image channels.
        for (int i = 0; i < Math.Min(_numChannels, 3); i++)
        {
            // Skip if channel is not selected.
            if (IsMultiChannel && ((i == 0 && ShowBlue == false) || (i == 1 && ShowGreen == false) || (i == 2 && ShowRed == false)))
                continue;

            // Select plot color for channel (foreground color if monochrome, BGR otherwise).
            System.Drawing.Color plotColor = _numChannels == 1 ? _foregroundColor : BGR[i];

            // Extract single-channel data.
            double[] ys = new double[SelectedHistSize];
            for (int j = 0; j < ys.Length; j++)
                ys[j] = _histogramData[i, j];

            PlotData(wpfPlot.Plot, xs, ys, plotColor, SelectedPlotType);            
        }

        FormatPlot(wpfPlot.Plot);

        // Enable new rendering.
        wpfPlot.Plot.RenderUnlock();
    }

    /// <summary>
    /// Plots data using selected plot type and color.
    /// </summary>
    /// <param name="xs">Data along x axis.</param>
    /// <param name="ys">Data along y axis.</param>
    /// <param name="plotColor">Color to use for plotting data.</param>
    /// <param name="histogramPlotType">Type of plot.</param>
    private void PlotData(Plot plot, double[] xs, double[] ys, System.Drawing.Color plotColor, HistogramPlotType histogramPlotType)
    {
        // Select plot type based on user-selected option.
        switch (histogramPlotType)
        {
            case HistogramPlotType.Fill:
            default:
                ScottPlot.Plottable.Polygon fillPlot = plot.AddFill(xs: xs, ys: ys, color:plotColor);
                fillPlot.FillColor = System.Drawing.Color.FromArgb(64, plotColor);
                fillPlot.LineWidth = 2;
                break;

            case HistogramPlotType.Bar:
                ScottPlot.Plottable.BarPlot barPlot = plot.AddBar(positions: xs, values: ys, color: plotColor);
                barPlot.BarWidth = (_histogramMaxValue + 1) / ys.Length;
                barPlot.BorderLineWidth = 1.2f;
                break;

            case HistogramPlotType.Line:
                ScottPlot.Plottable.SignalPlot linePlot = plot.AddSignal(ys: ys, sampleRate: (double)ys.Length / (_histogramMaxValue + 1), color: plotColor);
                //ScottPlot.Plottable.SignalPlotConst<double> linePlot = plot.AddSignalConst(ys, (double)ys.Length / (_histogramMaxValue + 1), plotColor);
                linePlot.OffsetX = ((float)_histogramMaxValue + 1) / 2 / ys.Length;
                linePlot.MarkerSize = 0;
                linePlot.LineStyle = LineStyle.Solid;
                linePlot.LineWidth = 2;
                break;
        }
    }

    /// <summary>
    /// Formats plot.
    /// </summary>
    private void FormatPlot(Plot plot)
    {
        // Format plot.
        _ = plot.XAxis.Label("DN", size: (float)wpfPlot.FontSize, bold: true);
        plot.XAxis.Ticks(major: true, minor: true);
        plot.XAxis.TickLabelStyle(fontSize: (float)wpfPlot.FontSize, fontBold: true);
        plot.XAxis.TickLabelNotation(multiplier: true);
        plot.YAxis.Ticks(enable: false);
        plot.YAxis.Hide(hidden: true);

        // Hide frame borders.
        plot.YAxis.Line(false); // left
        plot.XAxis2.Line(false); // top
        plot.YAxis2.Line(false); // right
        plot.XAxis.Line(true); // bottom

        plot.Layout(padding: 0); // use minimum padding around plot.
        plot.XAxis.MajorGrid(enable: ShowGrid, color: System.Drawing.Color.DarkGray, lineStyle: LineStyle.Solid, lineWidth: 1);
        plot.YAxis.MajorGrid(enable: false);

        plot.AxisAuto(verticalMargin: 0); // add some margin to x-axis, but use a tight y-axis fit.

        // Coloring.
        plot.XAxis.Color(color: _foregroundColor);
        plot.YAxis.Color(color: _foregroundColor);
        plot.Style(figureBackground: _backgroundColor, dataBackground: _backgroundColor);
    }

    #endregion

    #region Events

    /// <summary>
    /// Eventhandler method to property changes of <see cref="Histogram"/>.
    /// </summary>
    private static void OnHistogramChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as HistogramViewer;

        if (e.NewValue == null)
        {
            control.ContainsData = false;
            return;
        }

        if (control.IsEnabled == false || e.NewValue == e.OldValue)
            return;

        var histogram = (ImageHistogram)e.NewValue;

        // Update data for plotting.
        control._histogramData = histogram.Data;
        control._histogramMaxValue = histogram.Maximum;
        control._numChannels = (int)histogram.NumChannels;
        control.IsMultiChannel = histogram.NumChannels > 1;
        control.ContainsData = true;
        control._isPlotted = false;
    }

    /// <summary>
    /// Eventhandler method to histogram plot settings changes.
    /// </summary>
    private static void OnPlotSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == e.OldValue)
            return;

        var control = d as HistogramViewer;

        // Update histogram plot with new settings.
        control.RefreshPlot();
    }

    /// <summary>
    /// Eventhandler method to theme changes.
    /// </summary>
    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == e.OldValue)
            return;

        var control = d as HistogramViewer;
        var theme = (Theme)e.NewValue;

        // Update foreground and background colors.
        control._foregroundColor = WindowsColorConverter.MediaToDrawing(((SolidColorBrush)theme.ForegroundBrush).Color);
        control._backgroundColor = WindowsColorConverter.MediaToDrawing(((SolidColorBrush)theme.BackgroundBrush).Color);

        // Update histogram plot with new settings.
        control.RefreshPlot();
    }

    /// <summary>
    /// Eventhandler method to IsEnabled changes, starting/stopping rendering timer when user control is enabled/disabled.
    /// </summary>
    private void HistogramViewerControl_IsEnabledChanged(object _, DependencyPropertyChangedEventArgs e)
    {
        // Enable/disable timer.
        _renderingTimer.IsEnabled = (bool)e.NewValue;
    }

    /// <summary>
    /// Event-handling method to <see cref="DispatcherTimer.Tick"/> events, plotting and rendering new histogram data in UI.
    /// </summary>
    private void OnDispatcherTimerTick(object sender, EventArgs e)
    {
        // Only plot/render if data has not already been plotted.
        if (_isPlotted == false)
            RefreshPlot();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Invokes the PropertyChanged event when a property value has been changed.
    /// </summary>
    /// <param name="propertyName">Name of property that has been changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}