using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;

namespace WPFDemoApp.Utilities.Services;

/// <summary>
/// Service providing themes for the user interface.
/// </summary>
internal sealed class ThemeService : IThemeService
{
    /// <summary>
    /// Allows for detection and alteration of a theme.
    /// </summary>
    private readonly ThemeManager _themeManager;

    /// <summary>
    /// Creates a new theme providing service.
    /// </summary>
    public ThemeService()
    {
        // Retrieve current theme manager instance.
        _themeManager = ThemeManager.Current;
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<Theme> Themes => [.. _themeManager.Themes.Select(a => new Theme(schemeColor: a.ColorScheme,
                                                                                               name: a.Name,
                                                                                               baseColor: a.BaseColorScheme,
                                                                                               brush: a.ShowcaseBrush,
                                                                                               foregroundBrush: a.Resources["MahApps.Brushes.ThemeForeground"] as SolidColorBrush,
                                                                                               backgroundBrush: a.Resources["MahApps.Brushes.ThemeBackground"] as SolidColorBrush))];

    /// <inheritdoc/>
    public Theme? GetTheme()
    {
        var currentTheme = _themeManager.DetectTheme(Application.Current);
        if (currentTheme != null)
            return Themes.Single(t => t.Name == currentTheme.Name);
        else return null;
    }

    /// <inheritdoc/>
    public Theme? GetTheme(string name)
    {
        if (Themes.ToList().Exists(t => t.Name == name))
            return Themes.Single(t => t.Name == name);
        else return null;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"/>
    public void SetTheme(Theme theme)
    {
        if (Themes.Contains(theme))
            _themeManager.ChangeTheme(Application.Current, _themeManager.Themes.First(t => t.Name == theme.Name));
        else throw new InvalidOperationException($"Theme with name {theme.Name} was not found in the collection of available ones!");
    }
}