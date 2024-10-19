using Serilog.Events;

namespace ImagerViewer.Utilities.Messages;

/// <summary>
/// A log message for the status bar.
/// </summary>
/// <remarks>
/// Creates a new log message for the status bar.
/// </remarks>
/// <param name="text">Text to be displayed.</param>
/// <param name="logEventLevel">Log event level.</param>
internal sealed class StatusBarLogMessage(string text, LogEventLevel logEventLevel = LogEventLevel.Information)
{

    /// <summary>
    /// Log message.
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// Log event level.
    /// </summary>
    public LogEventLevel Level { get; } = logEventLevel;
}