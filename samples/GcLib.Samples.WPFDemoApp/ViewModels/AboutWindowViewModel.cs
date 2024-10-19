using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model of a window displaying info about the application.
/// </summary>
internal sealed class AboutWindowViewModel : ObservableRecipient
{
    #region Fields

    /// <summary>
    /// Assembly version.
    /// </summary>
    private readonly Version _version;

    /// <summary>
    /// Build version (number of days since 2000-01-01).
    /// </summary>
    private readonly string _buildVersion;

    /// <summary>
    /// Revision version (number of seconds since midnight divided by two).
    /// </summary>
    private readonly string _revisionVersion;

    /// <summary>
    /// Build date.
    /// </summary>
    private readonly DateOnly _buildDate;

    #endregion

    #region Properties

    /// <summary>
    /// Window title.
    /// </summary>
    public static string Title => $"About IMAGEVIEWER";

    /// <summary>
    /// Version string.
    /// </summary>
    public string VersionString => $"{_buildDate:yyyy-MM-dd} (Build: {_buildVersion}, Rev. {_revisionVersion})";

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new view model for an about dialog window.
    /// </summary>
    public AboutWindowViewModel()
    {
        // Retrieve assembly version.
        _version = Assembly.GetEntryAssembly().GetName().Version;

        _buildDate = new DateOnly(2000, 1, 1).AddDays(_version.Build);
        _buildVersion = _version.Build.ToString();
        _revisionVersion = _version.Revision.ToString();
    }

    #endregion
}