using System;

namespace GcLib.Utilities.Collections;

/// <summary>
/// Extension/helper methods to <see cref="IReadOnlyParameterCollection"/> interface.
/// </summary>
public static class IReadOnlyParameterCollectionExtensions
{
    /// <summary>
    /// Retrieves parameter casted as a <see cref="GcInteger"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcInteger"/> (or null if parameter does not exist in collection or if it is of incorrect type).</returns>
    public static GcInteger GetInteger(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) ? parameters[parameterName] as GcInteger : null;
    }

    /// <summary>
    /// Reads integer value of <see cref="GcInteger"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Value read from parameter.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static long GetIntegerValue(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) && parameters[parameterName] is GcInteger gcInteger ? gcInteger.Value : throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Writes integer value to <see cref="GcInteger"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="value">Value written to parameter.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetIntegerValue(this IReadOnlyParameterCollection parameters, string parameterName, long value)
    {
        if (parameters.IsImplemented(parameterName) && parameters[parameterName] is GcInteger gcInteger)
            gcInteger.Value = value;
        else throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Retrieves parameter casted as a <see cref="GcFloat"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcFloat"/> (or null if parameter does not exist in collection or if it is of incorrect type).</returns>
    public static GcFloat GetFloat(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) ? parameters[parameterName] as GcFloat : null;
    }

    /// <summary>
    /// Reads float value of <see cref="GcFloat"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Value read from parameter.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static double GetFloatValue(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) && parameters[parameterName] is GcFloat gcFloat ? gcFloat.Value : throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Writes float value to <see cref="GcFloat"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="value">Value written to parameter.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetFloatValue(this IReadOnlyParameterCollection parameters, string parameterName, double value)
    {
        if (parameters.IsImplemented(parameterName) && parameters[parameterName] is GcFloat gcFloat)
            gcFloat.Value = value;
        else throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Retrieves parameter casted as a <see cref="GcString"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcString"/> (or null if parameter does not exist in collection or if it is of incorrect type).</returns>
    public static GcString GetString(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) ? parameters[parameterName] as GcString : null;
    }

    /// <summary>
    /// Reads string value of <see cref="GcString"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Value read from parameter.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string GetStringValue(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) && parameters[parameterName] is GcString gcString ? gcString.Value : throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Writes float value to <see cref="GcString"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="value">Value written to parameter.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetStringValue(this IReadOnlyParameterCollection parameters, string parameterName, string value)
    {
        if (parameters.IsImplemented(parameterName) && parameters[parameterName] is GcString gcString)
            gcString.Value = value;
        else throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Retrieves parameter casted as a <see cref="GcEnumeration"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcEnumeration"/> (or null if parameter does not exist in collection or if it is of incorrect type).</returns>
    public static GcEnumeration GetEnumeration(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) ? parameters[parameterName] as GcEnumeration : null;
    }

    /// <summary>
    /// Reads integer value of <see cref="GcEnumeration"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Value read from parameter.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static long GetEnumValueAsInt(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) && parameters[parameterName] is GcEnumeration gcEnumeration ? gcEnumeration.IntValue : throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Reads string value of <see cref="GcEnumeration"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Value read from parameter.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string GetEnumValueAsString(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) && parameters[parameterName] is GcEnumeration gcEnumeration ? gcEnumeration.StringValue : throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Writes enumeration value (as an int) to <see cref="GcEnumeration"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="value">Value written to parameter.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetEnumValue(this IReadOnlyParameterCollection parameters, string parameterName, long value)
    {
        if (parameters.IsImplemented(parameterName) && parameters[parameterName] is GcEnumeration gcEnumeration)
            gcEnumeration.IntValue = value;
        else throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Writes enumeration value (as a string) to <see cref="GcEnumeration"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="value">Value written to parameter.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetEnumValue(this IReadOnlyParameterCollection parameters, string parameterName, string value)
    {
        if (parameters.IsImplemented(parameterName) && parameters[parameterName] is GcEnumeration gcEnumeration)
            gcEnumeration.StringValue = value;
        else throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Retrieves parameter casted as a <see cref="GcBoolean"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcBoolean"/> (or null if parameter does not exist in collection or if it is of incorrect type).</returns>
    public static GcBoolean GetBoolean(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) ? parameters[parameterName] as GcBoolean : null;
    }

    /// <summary>
    /// Reads boolean value of <see cref="GcBoolean"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Value read from parameter.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static bool GetBooleanValue(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) && parameters[parameterName] is GcBoolean gcBoolean ? gcBoolean.Value : throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Writes boolean value to <see cref="GcBoolean"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="value">Value written to parameter.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetBooleanValue(this IReadOnlyParameterCollection parameters, string parameterName, bool value)
    {
        if (parameters.IsImplemented(parameterName) && parameters[parameterName] is GcBoolean gcBoolean)
            gcBoolean.Value = value;
        else throw new InvalidOperationException($"Parameter {parameterName} not found in collection!");
    }

    /// <summary>
    /// Retrieves parameter casted as a <see cref="GcCommand"/> parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcCommand"/> (or null if parameter does not exist in collection or if it is of incorrect type).</returns>
    public static GcCommand GetCommand(this IReadOnlyParameterCollection parameters, string parameterName)
    {
        return parameters.IsImplemented(parameterName) ? parameters[parameterName] as GcCommand : null;
    }
}