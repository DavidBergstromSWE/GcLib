using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Events;

namespace FusionViewer.Models;

/// <summary>
/// Stores logging data for the application.
/// </summary>
internal sealed class LogModel
{
    /// <summary>
    /// Log events.
    /// </summary>
    private readonly List<LogEvent> _logEvents;

    /// <summary>
    /// Default log event level to be used for filtering.
    /// </summary>
    public LogEventLevel DefaultMinimumLogLevel { get; set; } = LogEventLevel.Debug;

    /// <summary>
    /// Creates a new store of logging data for the application.
    /// </summary>
    public LogModel()
    {
        _logEvents = [];
    }

    /// <summary>
    /// Adds a new log event to the logging data store.
    /// </summary>
    /// <param name="logEvent">Log event to be added.</param>
    public void AddLogEvent(LogEvent logEvent)
    {
        _logEvents.Add(logEvent);

        // Raise event.
        OnLogEventAdded(this, EventArgs.Empty);
    }

    /// <summary>
    /// Retrieves a list of current log events, filtered to specified log level.
    /// </summary>
    /// <param name="logEventLevel">Highest log level that will be retrieved.</param>
    /// <returns>List of log events.</returns>
    public IEnumerable<LogEvent> RetrieveLogs(LogEventLevel logEventLevel = LogEventLevel.Verbose)
    {
        // Create temporary list (to avoid receiving changes while enumerating).
        var logs = _logEvents.ToList();

        // Return logs with level equal to or higher than requested.
        return new List<LogEvent>(logs.Where(l => l.Level >= logEventLevel));
    }

    /// <summary>
    /// Event announcing that a new log event has been added.
    /// </summary>
    public event EventHandler LogEventAdded;

    /// <summary>
    /// Event-invoking method, announcing that a new log event has been added.
    /// </summary>
    private void OnLogEventAdded(object sender, EventArgs eventArgs)
    {
        LogEventAdded?.Invoke(this, eventArgs);
    }
}