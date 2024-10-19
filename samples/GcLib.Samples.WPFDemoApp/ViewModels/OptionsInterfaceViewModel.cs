using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImagerViewerApp.Utilities.Themes;

namespace ImagerViewerApp.ViewModels;

/// <summary>
/// View model for handling user interface related options.
/// </summary>
internal sealed class OptionsInterfaceViewModel : ObservableObject, IOptionsSubViewModel
{
    #region Fields

    private ThemeBaseColor _selectedBaseColor;
    private Theme _selectedTheme;
    private readonly MainWindowViewModel _mainWindowViewModel;

    // Initial settings.
    private readonly Theme _initialTheme;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public string Name => "Interface";

    /// <summary>
    /// Available UI color schemes.
    /// </summary>
    public List<Theme> Themes { get; }

    /// <summary>
    /// Base color of selected theme.
    /// </summary>
    public ThemeBaseColor SelectedBaseColor
    {
        get => _selectedBaseColor;
        set 
        { 
            if (SetProperty(ref _selectedBaseColor, value))
                _mainWindowViewModel.SelectedTheme = _mainWindowViewModel.Themes.Find(t => t.SchemeColor == _selectedTheme.SchemeColor && t.BaseColor == SelectedBaseColor.ToString());
        }
    }

    /// <summary>
    /// Currently selected theme.
    /// </summary>
    public Theme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
                _mainWindowViewModel.SelectedTheme = _mainWindowViewModel.Themes.Find(t => t.SchemeColor == _selectedTheme.SchemeColor && t.BaseColor == SelectedBaseColor.ToString());
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Changes the base color of the UI theme.
    /// </summary>
    public IRelayCommand<ThemeBaseColor> ChangeThemeBaseColorCommand { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Instantiates a new view model for handling user interface related options.
    /// </summary>
    public OptionsInterfaceViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        // Store initial theme setting.
        _initialTheme = _mainWindowViewModel.SelectedTheme;

        // Initialize settings.
        Themes = _mainWindowViewModel.Themes.DistinctBy(t => t.SchemeColor).ToList();
        _selectedBaseColor = Enum.Parse<ThemeBaseColor>(_initialTheme.BaseColor);
        _selectedTheme = Themes.Find(t => t.SchemeColor == _initialTheme.SchemeColor);
        ChangeThemeBaseColorCommand = new RelayCommand<ThemeBaseColor>(ChangeThemeBaseColor, (t) => t.Equals(ThemeBaseColor.Light) || t.Equals(ThemeBaseColor.Dark));
    }

    /// <summary>
    /// Change the base color of the currently selected theme.
    /// </summary>
    /// <param name="color">Base color.</param>
    private void ChangeThemeBaseColor(ThemeBaseColor color)
    {
        SelectedBaseColor = color;
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial theme settings.
        _mainWindowViewModel.SelectedTheme = _initialTheme;
    }

    #endregion
}