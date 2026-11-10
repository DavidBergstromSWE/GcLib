using System;
using System.Collections.Generic;
using System.Reflection;

namespace GcLib;

public sealed partial class VirtualCam
{
    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        _genApi.GetParameterList(out List<GcParameter> parameterList, out List<string> failedParameterList);
        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        var parameter = Parameters.GetParameter(parameterName);

        // Return null if parameter is missing or not implemented in collection.
        if (parameter is null || parameter.IsImplemented == false)
            return null;

        // Return matched GenApi parameter value.
        return GetGenApiParameter<GcParameter>(parameterName).ToString();
    }

    /// <inheritdoc/>
    /// <exception cref="MissingMemberException"></exception>
    protected override void SetParameterValue(string parameterName, string newParameterValue)
    {
        // Throw exception if parameter is not implemented in collection.
        if (Parameters.IsImplemented(parameterName) == false)
            throw new MissingMemberException(GetType().Name, parameterName);

        var parameter = GetGenApiParameter<GcParameter>(parameterName);

        // Return if change is unnecessary.
        if (newParameterValue == parameter.ToString())
            return;

        // Check type of parameter and set new value.
        switch (parameter)
        {
            case GcString gcString:
                gcString.Value = newParameterValue;
                break;

            case GcInteger gcInteger:
                gcInteger.Value = Convert.ToInt64(newParameterValue);
                break;

            case GcFloat gcFloat:
                gcFloat.Value = double.Parse(newParameterValue, System.Globalization.CultureInfo.InvariantCulture);
                break;

            case GcBoolean gcBoolean:
                gcBoolean.Value = Convert.ToBoolean(newParameterValue);
                break;

            case GcEnumeration gcEnumeration:
                gcEnumeration.StringValue = newParameterValue;
                break;

            case GcCommand:
                break;

            default:
                throw new ArgumentException($"{parameterName} is not a {nameof(GcParameter)}!", paramName: parameterName);
        }
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        GetGenApiParameter<GcCommand>(parameterName).Execute();
    }

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        return GetGenApiParameter<GcBoolean>(parameterName);
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        return GetGenApiParameter<GcCommand>(parameterName);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        return GetGenApiParameter<GcEnumeration>(parameterName);
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        return GetGenApiParameter<GcFloat>(parameterName);
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        return GetGenApiParameter<GcInteger>(parameterName);
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        return GetGenApiParameter<GcString>(parameterName);
    }

    /// <summary>
    /// Gets a GenApi parameter by name as the specified parameter type.
    /// </summary>
    /// <typeparam name="TParameter">The GenApi parameter type to return. Must derive from <see cref="GcParameter"/>.</typeparam>
    /// <param name="parameterName">The name of the GenApi parameter property.</param>
    /// <returns>The GenApi parameter instance that matches <paramref name="parameterName"/> and <typeparamref
    /// name="TParameter"/>.</returns>
    /// <exception cref="MissingMemberException">No member with the specified name can be returned as <typeparamref name="TParameter"/> from <see
    /// cref="GenApi"/>.</exception>
    private TParameter GetGenApiParameter<TParameter>(string parameterName) where TParameter : GcParameter
    {
        var propertyInfo = typeof(GenApi).GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public) ?? throw new MissingMemberException(nameof(GenApi), parameterName);

        if (propertyInfo.GetValue(_genApi, null) is TParameter parameter)
            return parameter;

        throw new MissingMemberException(nameof(GenApi), parameterName);
    }
}