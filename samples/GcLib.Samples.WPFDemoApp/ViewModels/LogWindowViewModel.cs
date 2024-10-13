using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FusionViewer.Models;
using Serilog.Events;

namespace FusionViewer.ViewModels;

/// <summary>
/// Model for a view displaying application logging information.
/// </summary>
internal sealed class LogWindowViewModel : ObservableObject
{
    #region Fields

    // backing_fields
    private ObservableCollection<LogEvent> _logEvents;
    private LogEventLevel _minimumLogLevel;

    /// <summary>
    /// Log data store.
    /// </summary>
    private readonly LogModel _logModel;

    #endregion

    #region Properties

    /// <summary>
    /// Displayed log events.
    /// </summary>
    public ObservableCollection<LogEvent> LogEvents 
    {
        get => _logEvents; 
        private set => SetProperty(ref _logEvents, value);
    }

    /// <summary>
    /// Available log levels.
    /// </summary>
    public static ObservableCollection<LogEventLevel> LogLevels => new(Enum.GetValues<LogEventLevel>());

    /// <summary>
    /// Selected minimum log level.
    /// </summary>
    public LogEventLevel MinimumLogLevel
    {
        get => _minimumLogLevel;
        set
        {
            if (SetProperty(ref _minimumLogLevel, value))
            {
                // Persist selected log level (to be used as default when re-opening window).
                _logModel.DefaultMinimumLogLevel = value;

                // Refresh view with new filter settings.
                RefreshLogView();
            }
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new model for a view displaying logging information about application events.
    /// </summary>
    /// <param name="logModel">Log data store.</param>
    public LogWindowViewModel(LogModel logModel)
    {
        _logModel = logModel;
        MinimumLogLevel = _logModel.DefaultMinimumLogLevel;
        _logModel.LogEventAdded += OnLogEventAdded;
        RefreshLogView();
    }

    /// <summary>
    /// Handler to events raised when a new log event has been added.
    /// </summary>
    private void OnLogEventAdded(object sender, EventArgs e)
    {
        // Refresh view with an updated list of log events.
        RefreshLogView();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Refreshes log view based on current filter settings.
    /// </summary>
    private void RefreshLogView()
    {
        // Retrieve new list of log events, filtered to selected minimum level.
        LogEvents = new(_logModel.RetrieveLogs(MinimumLogLevel));
    }

    #endregion
}