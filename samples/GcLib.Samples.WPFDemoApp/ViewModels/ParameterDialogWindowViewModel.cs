using CommunityToolkit.Mvvm.ComponentModel;
using GcLib;
using GcLib.Utilities.Collections;
using MahApps.Metro.Controls.Dialogs;

namespace ImagerViewerApp.ViewModels;

/// <summary>
/// View model for displaying and editing parameters.
/// </summary>
internal sealed class ParameterDialogWindowViewModel : ObservableObject
{
    #region Fields

    //private readonly IParameterCollection _initialParameterCollection;

    // backing-fields
    private IReadOnlyParameterCollection _parameterCollection;
    private GcVisibility _selectedVisibility;
    private System.Windows.Visibility _toolbarVisibility;
    private uint _parameterUpdateDelay;
    private MessageDialogResult _dialogResult;

    #endregion

    #region Properties

    /// <summary>
    /// Result of dialog.
    /// </summary>
    public MessageDialogResult DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }

    /// <summary>
    /// Title of view.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Collection of parameters.
    /// </summary>
    public IReadOnlyParameterCollection ParameterCollection
    {
        get => _parameterCollection;
        private set => SetProperty(ref _parameterCollection, value);
    }

    /// <summary>
    /// Parameter visibility level selected.
    /// </summary>
    public GcVisibility SelectedVisibility
    {
        get => _selectedVisibility;
        private set => SetProperty(ref _selectedVisibility, value);
    }

    /// <summary>
    /// Parameter filtering toolbar visibility.
    /// </summary>
    public System.Windows.Visibility ToolbarVisibility
    {
        get => _toolbarVisibility;
        private set => SetProperty(ref _toolbarVisibility, value);
    }

    /// <summary>
    /// Time delay before updating a parameter after changing value (in milliseconds).
    /// </summary>
    public uint UpdateTimeDelay
    {
        get => _parameterUpdateDelay;
        set => SetProperty(ref _parameterUpdateDelay, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new view model for displaying and editing parameters.
    /// </summary>
    /// <param name="viewTitle">Title of view.</param>
    /// <param name="parameterCollection">Collection of parameters.</param>
    /// <param name="visibility">Parameter visibility level.</param>
    /// <param name="toolbarVisibility">Visibility of filtering toolbar.</param>
    /// <param name="timeDelay">Time delay before updating a parameter after changing value (in milliseconds).</param>
    public ParameterDialogWindowViewModel(string viewTitle, IReadOnlyParameterCollection parameterCollection, GcVisibility visibility = GcVisibility.Guru, System.Windows.Visibility toolbarVisibility = System.Windows.Visibility.Visible, uint timeDelay = 400)
    {
        Title = viewTitle;

        ParameterCollection = parameterCollection;
        SelectedVisibility = visibility;
        ToolbarVisibility = toolbarVisibility;
        UpdateTimeDelay = timeDelay;

        // make copy of initial collection? (for cancellation button)
    }

    #endregion
}