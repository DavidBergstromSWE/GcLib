using System;
using System.Collections.Generic;
using System.Linq;
using Basler.Pylon;

namespace GcLib;

public sealed partial class PylonCam : GcDevice, IDeviceEnumerator
{
    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        return ToGcBoolean(_camera.Parameters[(BooleanName)parameterName]);
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        return ToGcCommand(_camera.Parameters[(CommandName)parameterName]);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        return ToGcEnumeration(_camera.Parameters[(EnumName)parameterName]);
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        return ToGcFloat(_camera.Parameters[(FloatName)parameterName]);
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        return ToGcInteger(_camera.Parameters[(IntegerName)parameterName]);
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        return ToGcString(_camera.Parameters[(StringName)parameterName]);
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        var parameter = GetTypedParameter(parameterName);
        if (parameter == null)
            return null;

        return parameter.IsReadable ? parameter.ToString() : Parameters[parameterName].ToString();
    }

    /// <summary>
    /// Retrieves a (non-command) parameter from the camera using the type of the cached parameter.
    /// </summary>
    private IParameter GetTypedParameter(string parameterName)
    {
        return Parameters[parameterName].Type switch
        {
            GcParameterType.Integer => _camera.Parameters[(IntegerName)parameterName],
            GcParameterType.Float => _camera.Parameters[(FloatName)parameterName],
            GcParameterType.String => _camera.Parameters[(StringName)parameterName],
            GcParameterType.Enumeration => _camera.Parameters[(EnumName)parameterName],
            GcParameterType.Boolean => _camera.Parameters[(BooleanName)parameterName],
            _ => null
        };
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        if (Parameters[parameterName].Type == GcParameterType.Command)
        {
            var command = _camera.Parameters[(CommandName)parameterName];
            if (command.IsWritable) command.Execute();
        }
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        var parameter = GetTypedParameter(parameterName);
        if (parameter == null)
            return;

        if (parameter.IsWritable)
            parameter.ParseAndSetValue(parameterValue);
    }

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();

        // The parameter collection enumerates all user-visible parameters of the camera.
        foreach (var parameter in _camera.Parameters)
        {
            var name = GetProperty(parameter, "Name");
            try
            {
                switch (parameter)
                {
                    case IIntegerParameter p when p.IsReadable:
                        parameterList.Add(ToGcInteger(p));
                        break;
                    case IFloatParameter p when p.IsReadable:
                        parameterList.Add(ToGcFloat(p));
                        break;
                    case IStringParameter p when p.IsReadable:
                        parameterList.Add(ToGcString(p));
                        break;
                    case IBooleanParameter p when p.IsReadable:
                        parameterList.Add(ToGcBoolean(p));
                        break;
                    case IEnumParameter p when p.IsReadable:
                        parameterList.Add(ToGcEnumeration(p));
                        break;
                    case ICommandParameter p when GetVisibility(p) != GcVisibility.Invisible:
                        parameterList.Add(ToGcCommand(p));
                        break;
                }
            }
            catch (Exception)
            {
                failedParameterList.Add(name);
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    #region Conversion helpers

    private static GcInteger ToGcInteger(IIntegerParameter p)
    {
        var readable = p.IsReadable;
        return new GcInteger(name: GetProperty(p, "Name"),
                             category: GetCategory(p),
                             value: readable ? p.GetValue() : 0,
                             min: readable ? p.GetMinimum() : 0,
                             max: readable ? p.GetMaximum() : 0,
                             increment: readable ? p.GetIncrement() : 1,
                             isReadable: readable,
                             isWritable: p.IsWritable,
                             visibility: GetVisibility(p),
                             description: GetProperty(p, "Description"));
    }

    private static GcFloat ToGcFloat(IFloatParameter p)
    {
        var readable = p.IsReadable;
        return new GcFloat(name: GetProperty(p, "Name"),
                           category: GetCategory(p),
                           value: readable ? p.GetValue() : 0,
                           min: readable ? p.GetMinimum() : 0,
                           max: readable ? p.GetMaximum() : 0,
                           increment: (double)(readable ? p.GetIncrement() : 0),
                           isReadable: readable,
                           isWritable: p.IsWritable,
                           visibility: GetVisibility(p),
                           description: GetProperty(p, "Description"));
    }

    private static GcString ToGcString(IStringParameter p)
    {
        return new GcString(name: GetProperty(p, "Name"),
                            category: GetCategory(p),
                            value: p.IsReadable ? p.GetValue() : string.Empty,
                            maxLength: 100,
                            isReadable: p.IsReadable,
                            isWritable: p.IsWritable,
                            visibility: GetVisibility(p),
                            description: GetProperty(p, "Description"));
    }

    private static GcBoolean ToGcBoolean(IBooleanParameter p)
    {
        return new GcBoolean(name: GetProperty(p, "Name"),
                             category: GetCategory(p),
                             value: p.IsReadable && p.GetValue(),
                             isReadable: p.IsReadable,
                             isWritable: p.IsWritable,
                             visibility: GetVisibility(p),
                             description: GetProperty(p, "Description"));
    }

    private static GcCommand ToGcCommand(ICommandParameter p)
    {
        return new GcCommand(name: GetProperty(p, "Name"),
                             category: GetCategory(p),
                             method: p.Execute,
                             isReadable: p.IsReadable,
                             isWritable: p.IsWritable,
                             visibility: GetVisibility(p),
                             description: GetProperty(p, "Description"));
    }

    private static GcEnumeration ToGcEnumeration(IEnumParameter p)
    {
        // Pylon enumerations are string-based, so entries are numbered by their position.
        var entries = p.GetAllValues().Select((symbolic, index) => new GcEnumEntry(symbolic, index)).ToList();
        var current = p.IsReadable ? p.GetValue() : string.Empty;
        var currentEntry = entries.FirstOrDefault(e => e.ValueString == current) ?? new GcEnumEntry(current, -1);

        return new GcEnumeration(name: GetProperty(p, "Name"),
                                 category: GetCategory(p),
                                 gcEnumEntry: currentEntry,
                                 gcEnumEntries: entries,
                                 isReadable: p.IsReadable,
                                 isWritable: p.IsWritable,
                                 visibility: GetVisibility(p),
                                 description: GetProperty(p, "Description"));
    }

    private static string GetProperty(IParameter parameter, string key)
    {
        return parameter.Advanced?.GetPropertyOrDefault(key, string.Empty) ?? string.Empty;
    }

    private static string GetCategory(IParameter parameter)
    {
        var category = GetProperty(parameter, "Category");
        return string.IsNullOrEmpty(category) ? "Root" : category;
    }

    private static GcVisibility GetVisibility(IParameter parameter)
    {
        return GetProperty(parameter, "Visibility") switch
        {
            "Expert" => GcVisibility.Expert,
            "Guru" => GcVisibility.Guru,
            "Invisible" => GcVisibility.Invisible,
            _ => GcVisibility.Beginner
        };
    }

    #endregion
}
