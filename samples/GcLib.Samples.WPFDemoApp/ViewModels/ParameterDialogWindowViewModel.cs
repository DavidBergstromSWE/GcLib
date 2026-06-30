using CommunityToolkit.Mvvm.ComponentModel;
using GcLib;
using GcLib.Utilities.Collections;
using MahApps.Metro.Controls.Dialogs;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for displaying and editing parameters.
/// </summary>
/// <remarks>
/// Creates a new view model for displaying and editing parameters.
/// </remarks>
/// <param name="viewTitle">Title of view.</param>
/// <param name="parameterCollection">Collection of parameters.</param>
/// <param name="visibility">Parameter visibility level.</param>
/// <param name="toolbarVisibility">Visibility of filtering toolbar.</param>
/// <param name="timeDelay">Time delay before updating a parameter after changing value (in milliseconds).</param>
internal sealed partial class ParameterDialogWindowViewModel(string viewTitle, IReadOnlyParameterCollection parameterCollection, GcVisibility visibility = GcVisibility.Guru, System.Windows.Visibility toolbarVisibility = System.Windows.Visibility.Visible, uint timeDelay = 400) : ObservableObject
{
    #region Properties

    /// <summary>
    /// Result of dialog.
    /// </summary>
    [ObservableProperty]
    public partial MessageDialogResult DialogResult { get; set; }

    /// <summary>
    /// Title of view.
    /// </summary>
    public string Title { get; } = viewTitle;

    /// <summary>
    /// Collection of parameters.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyParameterCollection ParameterCollection { get; private set; } = parameterCollection;

    /// <summary>
    /// Parameter visibility level selected.
    /// </summary>
    [ObservableProperty]
    public partial GcVisibility SelectedVisibility { get; private set; } = visibility;

    /// <summary>
    /// Parameter filtering toolbar visibility.
    /// </summary>
    [ObservableProperty]
    public partial System.Windows.Visibility ToolbarVisibility { get; private set; } = toolbarVisibility;

    /// <summary>
    /// Time delay before updating a parameter after changing value (in milliseconds).
    /// </summary>
    [ObservableProperty]
    public partial uint UpdateTimeDelay { get; set; } = timeDelay;

    #endregion
}