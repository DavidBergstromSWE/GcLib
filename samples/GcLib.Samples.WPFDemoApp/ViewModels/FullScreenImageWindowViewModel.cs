using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for showing images in full screen mode.
/// </summary>
internal sealed partial class FullScreenImageWindowViewModel : ObservableObject
{
    #region Properties

    /// <summary>
    /// True if title bar is visible.
    /// </summary>
    [ObservableProperty]
    public partial bool ShowTitleBar { get; set; }

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