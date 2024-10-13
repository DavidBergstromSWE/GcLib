using System;
using CommunityToolkit.Mvvm.Messaging;
using FusionViewer.Utilities.Messages;
using Serilog.Core;
using Serilog.Events;

namespace FusionViewer.Utilities.Logging;

/// <summary>
/// Sink where received log events are forwarded as messages of type <see cref="StatusBarLogMessage"/>. 
/// </summary>
/// <remarks>
/// Creates a new sink for received log events, to be forwarded as messages of type <see cref="StatusBarLogMessage"/>. 
/// </remarks>
/// <param name="messenger">Messenger instance to be used.</param>
/// <param name="minimumLevel">Minimum log level to forward as message.</param>
/// <param name="formatProvider">Format provider.</param>
internal class StatusBarSink(IMessenger messenger, LogEventLevel minimumLevel, IFormatProvider formatProvider) : ILogEventSink
{
    /// <summary>
    /// Messenger instance to be used.
    /// </summary>
    private readonly IMessenger _messenger = messenger;

    /// <summary>
    /// Format provider.
    /// </summary>
    private readonly IFormatProvider _formatProvider = formatProvider;

    /// <summary>
    /// Minimum log level to forward as message.
    /// </summary>
    private readonly LogEventLevel _minimumLevel = minimumLevel;

    public void Emit(Serilog.Events.LogEvent logEvent)
    {
        if (logEvent.Level >= _minimumLevel)
        {
            // Remove double quotes in log message.
            var message = logEvent.RenderMessage(_formatProvider).Replace("\"", "");

            // Send log event message to status bar.
            _messenger.Send(new StatusBarLogMessage(message, logEvent.Level));
        }   
    }
}