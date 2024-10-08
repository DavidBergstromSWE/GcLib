using System;

namespace GcLib;

/// <summary>
/// Provides data for events signaling an error.
/// </summary>
/// <remarks>
/// Creates new event data for an error.
/// </remarks>
/// <param name="errorMessage">Description about error.</param>
/// <param name="exception">(optional) Exception that was thrown when the error occurred.</param>
public class ErrorEventArgs(string errorMessage, Exception exception = null) : EventArgs
{
    /// <summary>
    /// Description about error.
    /// </summary>
    public string ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Exception that was thrown when the error occurred.
    /// </summary>
    public Exception Exception { get; } = exception;
}