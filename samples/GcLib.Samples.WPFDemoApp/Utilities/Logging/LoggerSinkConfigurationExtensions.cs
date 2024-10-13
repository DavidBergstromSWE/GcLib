using System;
using CommunityToolkit.Mvvm.Messaging;
using FusionViewer.Models;
using FusionViewer.Utilities.Messages;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace FusionViewer.Utilities.Logging;

/// <summary>
/// Extension class for <see cref="LoggerSinkConfiguration"/>, providing configurations for custom sinks.
/// </summary>
internal static class LoggerSinkConfigurationExtensions
{
    /// <summary>
    /// Provides configuration for a sink where received log events are forwarded as messages of type <see cref="StatusBarLogMessage"/>. 
    /// </summary>
    /// <param name="loggerConfiguration">Existing logger configuration.</param>
    /// <param name="messenger">Messenger instance to be used.</param>
    /// <param name="minimumLevel">Minimum log level to forward as message.</param>
    /// <param name="formatProvider">Format provider.</param>
    /// <returns>Updated logger configuration.</returns>
    public static LoggerConfiguration StatusBarSink(this LoggerSinkConfiguration loggerConfiguration, IMessenger messenger, LogEventLevel minimumLevel = LogEventLevel.Information, IFormatProvider formatProvider = null)
    {
        return loggerConfiguration.Sink(new StatusBarSink(messenger, minimumLevel, formatProvider));
    }

    /// <summary>
    /// Provides configuration for a sink where received log events are forwarded to an application log data store. 
    /// </summary>
    /// <param name="loggerConfiguration">Existing logger configuration.</param>
    /// <param name="logModel">Storage of application log data.</param>
    /// <param name="minimumLevel">Minimum log level to forward as message.</param>
    /// <param name="formatProvider">Format provider.</param>
    /// <returns></returns>
    public static LoggerConfiguration LogModelSink(this LoggerSinkConfiguration loggerConfiguration, LogModel logModel, LogEventLevel minimumLevel = LogEventLevel.Information, IFormatProvider formatProvider = null)
    {
        return loggerConfiguration.Sink(new LogModelSink(logModel, minimumLevel, formatProvider));
    }
}