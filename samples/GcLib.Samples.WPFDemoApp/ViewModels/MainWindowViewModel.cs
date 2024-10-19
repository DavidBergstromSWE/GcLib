using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ImagerViewer.Utilities.Messages;
using ImagerViewer.Utilities.Services;
using ImagerViewer.Utilities.Themes;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for the main window.
/// </summary>
internal sealed class MainWindowViewModel : ObservableRecipient
{
    #region Fields

    // backing-fields
    private StatusBarLogMessage _statusMessage;
    private Theme _selectedTheme;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Service providing themes.
    /// </summary>
    private readonly IThemeService _themeService;

    #endregion

    #region Properties

    /// <summary>
    /// Application title.
    /// </summary>
    public static string Title => "ImageViewer";

    /// <summary>
    /// Application version string.
    /// </summary>
    public static string MajorMinorVersion
    {
        get
        {
            var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    /// <summary>
    /// Message shown in status bar.
    /// </summary>
    public StatusBarLogMessage StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Available UI themes.
    /// </summary>
    public List<Theme> Themes { get; }

    /// <summary>
    /// Currently selected UI theme.
    /// </summary>
    public Theme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
                _themeService.SetTheme(_selectedTheme);
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a request invoked by a UI command to open Options window.
    /// </summary>
    public IRelayCommand OpenOptionsWindowCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to open About window.
    /// </summary>
    public IRelayCommand OpenAboutWindowCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to open a window showing the logging information of current application session.
    /// </summary>
    public IRelayCommand OpenLogDialogWindowCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to toggle application theme (between light and dark modes).
    /// </summary>
    public IRelayCommand ToggleThemeCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to toggle application theme (between light and dark modes).
    /// </summary>
    public IRelayCommand OpenShortcutWindowCommand { get; }

    #endregion

    #region Private methods

    /// <summary>
    /// Opens a new About window, showing application and author info.
    /// </summary>
    private void OpenAboutWindow()
    {
        _windowService.ShowWindow<AboutWindowViewModel>();
    }

    /// <summary>
    /// Opens options view for displaying and setting application options.
    /// </summary>
    private void OpenOptionsWindow()
    {
        _windowService.ShowWindow<OptionsWindowViewModel>();
    }

    /// <summary>
    /// Opens window for displaying logging information about current application session.
    /// </summary>
    private void OpenLogDialogWindow()
    {
        _windowService.ShowWindow<LogWindowViewModel>();
    }

    /// <summary>
    /// Opens window for displaying available shortcut keybindings.
    /// </summary>
    private void OpenShortcutWindow()
    {
        _windowService.ShowWindow<ShortcutWindowViewModel>();
    }

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    private void ToggleTheme()
    {
        // Retrieve inverted theme.
        Theme invertedTheme = SelectedTheme.BaseColor == "Light"
            ? _themeService.GetTheme(SelectedTheme.Name.Replace(SelectedTheme.BaseColor, "Dark"))
            : _themeService.GetTheme(SelectedTheme.Name.Replace(SelectedTheme.BaseColor, "Light"));

        // Change theme.
        SelectedTheme = invertedTheme;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        // Register as recipient of messages for updating status bar.
        Messenger.Register<StatusBarLogMessage>(this, (sender, msg) =>
        {
            StatusMessage = msg;
        });
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new view model for the main window.
    /// </summary>
    public MainWindowViewModel(IMetroWindowService windowService, IThemeService themeService)
    {
        _windowService = windowService;
        _themeService = themeService;

        // Instantiate commands.
        OpenOptionsWindowCommand = new RelayCommand(OpenOptionsWindow);
        OpenAboutWindowCommand = new RelayCommand(OpenAboutWindow);
        OpenLogDialogWindowCommand = new RelayCommand(OpenLogDialogWindow);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenShortcutWindowCommand = new RelayCommand(OpenShortcutWindow);

        // Available accent colors.
        Themes = [.. _themeService.Themes];

        // Detect current theme.
        SelectedTheme = _themeService.GetTheme();

        IsActive = true;
    }

    #endregion
}