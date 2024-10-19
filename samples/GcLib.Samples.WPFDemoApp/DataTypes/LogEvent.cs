using System;
using Serilog.Events;

namespace ImagerViewerApp;

/// <summary>
/// Represents a logging event, described by a timestamped message and an importance level.
/// </summary>
/// <remarks>
/// Creates a new log event.
/// </remarks>
/// <param name="TimeStamp">Timestamp of log event.</param>
/// <param name="LogEventLevel">Importance level of log event.</param>
/// <param name="Message">Log event message.</param>
internal record LogEvent(DateTime TimeStamp, LogEventLevel LogEventLevel, string Message)
{
    /// <summary>
    /// Timestamp of log event.
    /// </summary>
    public DateTime Timestamp { get; } = TimeStamp;

    /// <summary>
    /// Log event level (importance).
    /// </summary>
    public LogEventLevel Level { get; } = LogEventLevel;

    /// <summary>
    /// Log event message.
    /// </summary>
    public string Message { get; } = Message;
}