using System;
using ImagerViewer.Models;
using Serilog.Core;
using Serilog.Events;

namespace ImagerViewer.Utilities.Logging;

/// <summary>
/// Sink where received log events are forwarded to an application log data store. 
/// </summary>
internal class LogModelSink : ILogEventSink
{
    /// <summary>
    /// Creates a new sink for received log events, to be forwarded to an application log data store. 
    /// </summary>
    /// <param name="logModel">Storage for application log data.</param>
    /// <param name="minimumLevel">Minimum log level to forward.</param>
    /// <param name="formatProvider">Format provider.</param>
    public LogModelSink(LogModel logModel, LogEventLevel minimumLevel, IFormatProvider formatProvider)
    {
        _logModel = logModel;
        _logModel.DefaultMinimumLogLevel = minimumLevel;
        _minimumLevel = minimumLevel;
        _formatProvider = formatProvider;
    }

    /// <summary>
    /// Storage for application log data.
    /// </summary>
    private readonly LogModel _logModel;

    /// <summary>
    /// Format provider.
    /// </summary>
    private readonly IFormatProvider _formatProvider;

    /// <summary>
    /// Minimum log level to forward.
    /// </summary>
    private readonly LogEventLevel _minimumLevel;

    /// <inheritdoc/>
    public void Emit(Serilog.Events.LogEvent logEvent)
    {
        // Emit/forward the provided log event if the level is equal or higher than minimum level.
        if (logEvent.Level >= _minimumLevel)
        {
            // Send log event message to log data storage.
            _logModel.AddLogEvent(new LogEvent(logEvent.Timestamp.LocalDateTime, logEvent.Level, logEvent.RenderMessage(_formatProvider).Replace("\"", "")));
        }
    }
}