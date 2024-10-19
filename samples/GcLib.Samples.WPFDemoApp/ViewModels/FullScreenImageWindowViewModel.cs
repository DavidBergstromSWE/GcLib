using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for showing images in full screen mode.
/// </summary>
internal sealed class FullScreenImageWindowViewModel : ObservableObject
{
    #region Fields

    private bool _showTitleBar;

    #endregion

    #region Properties

    /// <summary>
    /// True if title bar is visible.
    /// </summary>
    public bool ShowTitleBar
    {
        get => _showTitleBar;
        set => SetProperty(ref _showTitleBar, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relay a command to toggle the title bar.
    /// </summary>
    public IRelayCommand ToggleTitleBarCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new view model for showing images in full screen mode.
    /// </summary>
    public FullScreenImageWindowViewModel()
    {
        ToggleTitleBarCommand = new RelayCommand(() => ShowTitleBar = !ShowTitleBar);
    }

    #endregion
}