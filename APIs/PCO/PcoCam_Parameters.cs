using System;
using System.Collections.Generic;
using System.Reflection;

namespace GcLib;

public sealed partial class PcoCam
{
    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        _genApi.GetParameterList(out List<GcParameter> parameterList, out List<string> failedParameterList);
        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    /// <inheritdoc/>
    /// <exception cref="MissingMemberException"></exception>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        bool parameterFound = Parameters.IsImplemented(parameterName);
        if (parameterFound == false)
            throw new MissingMemberException(GetType().Name, parameterName);

        // Get matched parameter (using Reflection).
        PropertyInfo propertyInfo = _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public);

        var parameter = propertyInfo.GetValue(_genApi, null);

        try
        {
            // Check type of parameter and set new value.
            switch (parameter)
            {
                case GcString gcString:
                    propertyInfo.PropertyType.GetProperty(nameof(GcString.Value)).SetValue(parameter, parameterValue, null);
                    break;

                case GcInteger gcInteger:
                    propertyInfo.PropertyType.GetProperty(nameof(GcInteger.Value)).SetValue(parameter, Convert.ToInt64(parameterValue), null);
                    break;

                case GcFloat gcFloat:
                    propertyInfo.PropertyType.GetProperty(nameof(GcFloat.Value)).SetValue(parameter, Convert.ToDouble(parameterValue), null);
                    break;

                case GcBoolean gcBoolean:
                    propertyInfo.PropertyType.GetProperty(nameof(GcBoolean.Value)).SetValue(parameter, Convert.ToBoolean(parameterValue), null);
                    break;

                case GcEnumeration gcEnumeration:
                    propertyInfo.PropertyType.GetProperty(nameof(GcEnumeration.StringValue)).SetValue(parameter, parameterValue, null);
                    break;

                case GcCommand gcCommand:
                    break;

                default:
                    throw new ArgumentException($"{parameterName} is not a {nameof(GcParameter)}!", paramName: parameterName);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        if (Parameters[parameterName].IsImplemented == false)
            return null;

        return Parameters[parameterName].Type switch
        {
            GcParameterType.Integer => GetInteger(parameterName).ToString(),
            GcParameterType.Float => GetFloat(parameterName).ToString(),
            GcParameterType.String => GetString(parameterName),
            GcParameterType.Boolean => GetBoolean(parameterName).ToString(),
            GcParameterType.Enumeration => GetEnumeration(parameterName).ToString(),
            GcParameterType.Command => null,
            _ => null,
        };
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        (Parameters[parameterName] as GcCommand).Execute();
    }

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        return _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public).GetValue(_genApi) as GcBoolean;
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        return _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public).GetValue(_genApi) as GcCommand;
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        return _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public).GetValue(_genApi) as GcEnumeration;
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        return _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public).GetValue(_genApi) as GcFloat;
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        return _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public).GetValue(_genApi) as GcInteger;
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        return _genApi.GetType().GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public).GetValue(_genApi) as GcString;
    }
}