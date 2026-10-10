using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WPFDemoApp.Utilities.Messages;
using WPFDemoApp.Utilities.Services;

namespace WPFDemoApp.ViewModels;

/// <summary>
/// View model for the main window.
/// </summary>
internal sealed partial class MainWindowViewModel : ObservableRecipient
{
    #region Fields

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    /// <summary>
    /// Service providing themes.
    /// </summary>
    private readonly IThemeService _themeService;

    /// <summary>
    /// Service providing access to application settings.
    /// </summary>
    private readonly ISettingsService _settingsService;

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
    [ObservableProperty]
    public partial StatusBarLogMessage StatusMessage { get; private set; }

    /// <summary>
    /// Available UI themes.
    /// </summary>
    public List<Theme> Themes { get; }

    /// <summary>
    /// Currently selected UI theme.
    /// </summary>
    public Theme SelectedTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _themeService.SetTheme(field);
                _settingsService.Current.Theme = field.Name;
            }
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
        Theme invertedTheme = (Theme)(SelectedTheme.BaseColor == "Light"
            ? _themeService.GetTheme(SelectedTheme.Name.Replace(SelectedTheme.BaseColor, "Dark"))
            : _themeService.GetTheme(SelectedTheme.Name.Replace(SelectedTheme.BaseColor, "Light")));

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
    public MainWindowViewModel(IMetroWindowService windowService, IThemeService themeService, ISettingsService settingsService)
    {
        _windowService = windowService;
        _themeService = themeService;
        _settingsService = settingsService;

        // Instantiate commands.
        OpenOptionsWindowCommand = new RelayCommand(OpenOptionsWindow);
        OpenAboutWindowCommand = new RelayCommand(OpenAboutWindow);
        OpenLogDialogWindowCommand = new RelayCommand(OpenLogDialogWindow);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenShortcutWindowCommand = new RelayCommand(OpenShortcutWindow);

        // Available accent colors.
        Themes = [.. _themeService.Themes];

        // Set current theme.
        SelectedTheme = (Theme)(_themeService.GetTheme(_settingsService.Current.Theme) is null
            ? _themeService.GetTheme()
            : (Theme)_themeService.GetTheme(_settingsService.Current.Theme));

        IsActive = true;
    }

    #endregion
}