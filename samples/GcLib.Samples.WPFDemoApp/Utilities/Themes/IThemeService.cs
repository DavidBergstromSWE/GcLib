using System.Collections.Generic;

namespace ImagerViewerApp.Utilities.Themes;

/// <summary>
/// Interface for a service providing themes for the user interface of the application.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Retrieve the currently used theme.
    /// </summary>
    /// <returns>Theme currently used.</returns>
    Theme GetTheme();

    /// <summary>
    /// Retrieve a named theme.
    /// </summary>
    /// <param name="name">Name of theme.</param>
    /// <returns>Theme.</returns>
    Theme GetTheme(string name);

    /// <summary>
    /// Change the currently used theme.
    /// </summary>
    /// <param name="theme">New theme.</param>
    void SetTheme(Theme theme);

    /// <summary>
    /// Available themes.
    /// </summary>
    IReadOnlyCollection<Theme> Themes { get; }
}