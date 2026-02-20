using System;

namespace GcLib;

/// <summary>
/// Provides data for an <see cref="GcBufferWriter.WritingAborted"/> event.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the <see cref="GcBufferWriter.WritingAborted"/> event.
/// </remarks>
/// <param name="errorMessage">Description about error.</param>
/// <param name="exception">(optional) Exception that was thrown when the error occurred.</param>
public sealed class WritingAbortedEventArgs(string errorMessage, Exception exception = null) : ErrorEventArgs(errorMessage, exception) { }