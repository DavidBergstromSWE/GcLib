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
        // Return null if parameter is not implemented in collection.
        if (Parameters[parameterName].IsImplemented == false)
            return null;

        // Get matched property from GenApi.
        PropertyInfo propertyInfo = typeof(GenApi).GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public);

        // Retrieve property casted as a parameter.
        var parameter = propertyInfo.GetValue(_genApi, null) as GcParameter;

        // Return parameter value.
        return parameter.ToString();
    }

    /// <inheritdoc/>
    /// <exception cref="MissingMemberException"></exception>
    protected override void SetParameterValue(string parameterName, string newParameterValue)
    {
        // Throw exception if parameter is not implemented in collection.
        if (Parameters.IsImplemented(parameterName) == false)
            throw new MissingMemberException(GetType().Name, parameterName);

        // Get matched property from GenApi.
        PropertyInfo propertyInfo = typeof(GenApi).GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public);

        // Retrieve property casted as a parameter.
        var parameter = propertyInfo.GetValue(_genApi, null) as GcParameter;

        // Return if change is unnecessary.
        if (newParameterValue == parameter.ToString())
            return;

        try
        {
            // Check type of parameter and set new value.
            switch (parameter)
            {
                case GcString gcString:
                    propertyInfo.PropertyType.GetProperty(nameof(GcString.Value)).SetValue(parameter, newParameterValue, null);
                    break;

                case GcInteger gcInteger:
                    propertyInfo.PropertyType.GetProperty(nameof(GcInteger.Value)).SetValue(parameter, Convert.ToInt64(newParameterValue), null);
                    break;

                case GcFloat gcFloat:
                    propertyInfo.PropertyType.GetProperty(nameof(GcFloat.Value)).SetValue(parameter, Convert.ToDouble(newParameterValue), null);
                    break;

                case GcBoolean gcBoolean:
                    propertyInfo.PropertyType.GetProperty(nameof(GcBoolean.Value)).SetValue(parameter, Convert.ToBoolean(newParameterValue), null);
                    break;

                case GcEnumeration gcEnumeration:
                    propertyInfo.PropertyType.GetProperty(nameof(GcEnumeration.StringValue)).SetValue(parameter, newParameterValue, null);
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