using CommunityToolkit.Mvvm.ComponentModel;
using ImagerViewer.UserControls;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for handling histogram-related options.
/// </summary>
internal sealed class OptionsHistogramViewModel : IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly HistogramPlotType _selectedPlotType;
    private readonly int _selectedHistogramSize;
    private readonly bool _showGrid;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public string Name => "Histogram";

    /// <summary>
    /// Reference to parent view model.
    /// </summary>
    public HistogramViewModel HistogramViewModel { get; init; }

    #endregion

    #region Constructor

    /// <summary>
    /// Instantiates a new view model for handling histogram-related options.
    /// </summary>
    public OptionsHistogramViewModel(HistogramViewModel histogramViewModel)
    {
        HistogramViewModel = histogramViewModel;

        // Store initial settings.
        _selectedPlotType = HistogramViewModel.SelectedPlotType;
        _selectedHistogramSize = HistogramViewModel.SelectedHistogramSize;
        _showGrid = HistogramViewModel.ShowGrid;
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        HistogramViewModel.SelectedHistogramSize = _selectedHistogramSize;
        HistogramViewModel.ShowGrid = _showGrid;
        HistogramViewModel.SelectedPlotType = _selectedPlotType;
    }

    #endregion
}