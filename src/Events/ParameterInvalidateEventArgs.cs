using System;

namespace GcLib;

/// <summary>
/// Provides data for a <see cref="GcDevice.ParameterInvalidate"/> event.
/// </summary>
/// <remarks>
/// Creates a new instance of the class, providing data for the <see cref="GcDevice.ParameterInvalidate"/> event.
/// </remarks>
/// <param name="parameterName">Name of parameter that was changed.</param>
public sealed class ParameterInvalidateEventArgs(string parameterName) : EventArgs
{
    /// <summary>
    /// Name of parameter that was changed.
    /// </summary>
    public string ParameterName { get; } = parameterName;
}