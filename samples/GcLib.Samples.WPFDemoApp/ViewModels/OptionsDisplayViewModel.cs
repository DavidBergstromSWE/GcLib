using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FusionViewer.ViewModels;

/// <summary>
/// View model for handling display-related options.
/// </summary>
internal sealed class OptionsDisplayViewModel : ObservableObject, IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly BitmapScalingMode _selectedBitmapScalingMode;
    private readonly bool _showPixelInspector;
    private readonly bool _limitFPS;
    private readonly uint _targetFPS;
    private readonly bool _showFullScreenFrameInfo;
    private readonly bool _showFullScreenChannelInfo;
    private readonly bool _synchronizeViews;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public string Name => "Display";

    /// <summary>
    /// Reference to parent view model.
    /// </summary>
    public ImageDisplayViewModel DisplayViewModel { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new view model for handling display-related options.
    /// </summary>
    public OptionsDisplayViewModel(ImageDisplayViewModel imageDisplayViewModel)
    {
        DisplayViewModel = imageDisplayViewModel;

        // Store initial settings.
        _limitFPS = DisplayViewModel.LimitFPS;
        _targetFPS = DisplayViewModel.TargetFPS;
        _selectedBitmapScalingMode = DisplayViewModel.SelectedBitmapScalingMode;
        _showPixelInspector = DisplayViewModel.ShowPixelInspector;
        _showFullScreenFrameInfo = DisplayViewModel.ShowFullScreenFrameInfo;
        _showFullScreenChannelInfo = DisplayViewModel.ShowFullScreenChannelInfo;
        _synchronizeViews = DisplayViewModel.SynchronizeViews;
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        DisplayViewModel.SelectedBitmapScalingMode = _selectedBitmapScalingMode;
        DisplayViewModel.ShowPixelInspector = _showPixelInspector;
        DisplayViewModel.LimitFPS = _limitFPS;
        DisplayViewModel.TargetFPS = _targetFPS;
        DisplayViewModel.ShowFullScreenFrameInfo = _showFullScreenFrameInfo;
        DisplayViewModel.ShowFullScreenChannelInfo = _showFullScreenChannelInfo;
        DisplayViewModel.SynchronizeViews = _synchronizeViews;
    }

    #endregion
}