using System;

namespace GcLib;

/// <summary>
/// Provides data for a <see cref="GcDevice.AcquisitionAborted"/> event.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the <see cref="GcDevice.AcquisitionAborted"/> event.
/// </remarks>
/// <param name="errorMessage">Description about error.</param>
/// <param name="exception">(optional) Exception that was thrown when the error occurred.</param>
public sealed class AcquisitionAbortedEventArgs(string errorMessage, Exception exception = null) : ErrorEventArgs(errorMessage, exception) {}