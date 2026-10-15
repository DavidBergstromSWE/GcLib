using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Basler.Pylon;

namespace GcLib;

public sealed partial class PylonCam : GcDevice, IDeviceEnumerator
{
    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        var p = _camera.Parameters[(BooleanName)parameterName];
        if (p.IsReadable == false)
            return Parameters[parameterName] as GcBoolean; // Return cached value if parameter is not readable.
        return ToGcBoolean(p);
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        return ToGcCommand(_camera.Parameters[(CommandName)parameterName]);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        var p = _camera.Parameters[(EnumName)parameterName];
        if (p.IsReadable == false)
            return Parameters[parameterName] as GcEnumeration; // Return cached value if parameter is not readable.
        return ToGcEnumeration(p);
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        var p = _camera.Parameters[(FloatName)parameterName];
        if (p.IsReadable == false)
            return Parameters[parameterName] as GcFloat; // Return cached value if parameter is not readable.
        return ToGcFloat(p);
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        var p = _camera.Parameters[(IntegerName)parameterName];
        if (p.IsReadable == false)
            return Parameters[parameterName] as GcInteger; // Return cached value if parameter is not readable.
        return ToGcInteger(p);
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        var p = _camera.Parameters[(StringName)parameterName];
        if (p.IsReadable == false)
            return Parameters[parameterName] as GcString; // Return cached value if parameter is not readable.
        return ToGcString(p);
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        var parameter = GetTypedParameter(parameterName);

        if (parameter == null)
            return null;

        if (parameter.IsEmpty || parameter.IsReadable == false)
            return Parameters[parameterName].ToString(); // Return cached value if parameter is empty or not readable.

        return parameter is IFloatParameter floatParameter ? floatParameter.GetValue().ToString(CultureInfo.CurrentCulture) // Use current culture for float formatting
             : parameter is IBooleanParameter booleanParameter ? booleanParameter.GetValue().ToString() // pylon uses 0 and 1 for boolean values, but we want true/false
             : parameter.ToString();
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
            command.Execute();
        }
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        var parameter = GetTypedParameter(parameterName);
        if (parameter == null)
            return;

        if (parameter.IsWritable)
        {
            if (parameter is IBooleanParameter booleanParameter)
            {
                booleanParameter.SetValue(bool.Parse(parameterValue));
            }
            else
            {
                parameter.ParseAndSetValue(parameterValue);
            }
        }
    }

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameterDict = new Dictionary<string, GcParameter>();
        var failedParameterList = new List<string>();

        
        // The parameter collection enumerates all user-visible parameters of the camera. 
        foreach (var parameter in _camera.Parameters)
        {
            var name = parameter.Name;
            try
            {
                switch (parameter)
                {
                    case IIntegerParameter p when p.IsReadable:
                        parameterDict.Add(p.Name, ToGcInteger(p));
                        break;
                    case IFloatParameter p when p.IsReadable:
                        parameterDict.Add(p.Name, ToGcFloat(p));
                        break;
                    case IStringParameter p when p.IsReadable:
                        parameterDict.Add(p.Name, ToGcString(p));
                        break;
                    case IBooleanParameter p when p.IsReadable:
                        parameterDict.Add(p.Name, ToGcBoolean(p));
                        break;
                    case IEnumParameter p when p.IsReadable:
                        parameterDict.Add(p.Name, ToGcEnumeration(p));
                        break;
                    case ICommandParameter p when GetVisibility(p) != GcVisibility.Invisible:
                        parameterDict.Add(p.Name, ToGcCommand(p));
                        break;
                }
            }
            catch (Exception)
            {
                failedParameterList.Add(name);
            }
        }

        return new GcDeviceParameterCollection(this, [.. parameterDict.Values], failedParameterList);
    }

    /// <summary>
    /// Imports parameters and features implemented by device.
    /// Can be used when <see cref="IParameterCollection"/> enumerator is broken (and throws an exception when iterating over the collection).
    /// </summary>
    /// <param name="useReflection">Reserved for selecting reflection-based parameter discovery.</param>
    /// <returns>Collection of parameters.</returns>
    private GcDeviceParameterCollection ImportParameters(bool useReflection)
    {
        if (useReflection == false)
            return ImportParameters();

        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();

        IParameterCollection parameters = _camera.Parameters;

        // We use reflection on PLCamera to get all known, valid parameter names.
        // This completely avoids using the broken enumerator in IParameterCollection.
        PropertyInfo[] properties = typeof(PLCamera).GetProperties(BindingFlags.Public | BindingFlags.Static);

        foreach (PropertyInfo property in properties)
        {
            object value = property.GetValue(null);

            switch (value)
            {
                case IntegerName integerName:
                    if (parameters.Contains(GetPropertyName(integerName.Name)))
                    {
                        IParameter parameter = parameters[integerName];
                        if (parameter != null && !parameter.IsEmpty && parameter.IsReadable)
                            parameterList.Add(ToGcInteger((IIntegerParameter)parameter));
                    }
                    break;
                case FloatName floatName:
                    if (parameters.Contains(GetPropertyName(floatName.Name)))
                    {
                        IParameter parameter = parameters[floatName];
                        if (parameter != null && !parameter.IsEmpty && parameter.IsReadable)
                            parameterList.Add(ToGcFloat((IFloatParameter)parameter));
                    }
                    break;
                case StringName stringName:
                    if (parameters.Contains(GetPropertyName(stringName.Name)))
                    {
                        IParameter parameter = parameters[stringName];
                        if (parameter != null && !parameter.IsEmpty && parameter.IsReadable)
                            parameterList.Add(ToGcString((IStringParameter)parameter));
                    }
                    break;
                case EnumName enumName:
                    if (parameters.Contains(GetPropertyName(enumName.Name)))
                    {
                        IParameter parameter = parameters[enumName];
                        if (parameter != null && !parameter.IsEmpty && parameter.IsReadable)
                            parameterList.Add(ToGcEnumeration((IEnumParameter)parameter));
                    }
                    break;
                case BooleanName booleanName:
                    if (parameters.Contains(GetPropertyName(booleanName.Name)))
                    {
                        IParameter parameter = parameters[booleanName];
                        if (parameter != null && !parameter.IsEmpty && parameter.IsReadable)
                            parameterList.Add(ToGcBoolean((IBooleanParameter)parameter));
                    }
                    break;
                case CommandName commandName:
                    if (parameters.Contains(GetPropertyName(commandName.Name)))
                    {
                        IParameter parameter = parameters[commandName];
                        if (parameter != null && parameter.IsEmpty == false)
                            parameterList.Add(ToGcCommand((ICommandParameter)parameter));
                    }
                    break;
                default:
                    continue; // Skip unknown parameter types
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    #region Conversion helpers

    private static GcInteger ToGcInteger(IIntegerParameter p)
    {
        return new GcInteger(name: p.Name,
                             category: GetCategory(p),
                             value: p.GetValue(),
                             min: p.GetMinimum(),
                             max: p.GetMaximum(),
                             increment: p.GetIncrement(),
                             isReadable: p.IsReadable,
                             isWritable: p.IsWritable,
                             visibility: GetVisibility(p),
                             description: GetProperty(p, "Description"));
    }

    private static GcFloat ToGcFloat(IFloatParameter p)
    {
        return new GcFloat(name: p.Name,
                           category: GetCategory(p),
                           value: p.GetValue(),
                           min: p.GetMinimum(),
                           max: p.GetMaximum(),
                           increment: (double)(p.GetIncrement() != null? p.GetIncrement() : 0),
                           isReadable: p.IsReadable,
                           isWritable: p.IsWritable,
                           visibility: GetVisibility(p),
                           description: GetProperty(p, "Description"));
    }

    private static GcString ToGcString(IStringParameter p)
    {
        return new GcString(name: p.Name,
                            category: GetCategory(p),
                            value: p.GetValue(),
                            maxLength: p.GetMaxLength(),
                            isReadable: p.IsReadable,
                            isWritable: p.IsWritable,
                            visibility: GetVisibility(p),
                            description: GetProperty(p, "Description"));
    }

    private static GcBoolean ToGcBoolean(IBooleanParameter p)
    {
        return new GcBoolean(name: p.Name,
                             category: GetCategory(p),
                             value: p.GetValue(),
                             isReadable: p.IsReadable,
                             isWritable: p.IsWritable,
                             visibility: GetVisibility(p),
                             description: GetProperty(p, "Description"));
    }

    private static GcCommand ToGcCommand(ICommandParameter p)
    {
        return new GcCommand(name: p.Name,
                             category: GetCategory(p),
                             method: p.Execute,
                             visibility: GetVisibility(p),
                             description: GetProperty(p, "Description"));
    }

    private static GcEnumeration ToGcEnumeration(IEnumParameter p)
    {
        // Pylon enumerations are string-based, so entries are numbered by their position.
        var entries = p.GetAllValues().Select((symbolic, index) => new GcEnumEntry(symbolic, index)).ToList();
        var current = p.IsReadable ? p.GetValue() : string.Empty;
        var currentEntry = entries.FirstOrDefault(e => e.ValueString == current) ?? new GcEnumEntry(current, -1);

        return new GcEnumeration(name: p.Name,
                                 category: GetCategory(p),
                                 gcEnumEntry: currentEntry,
                                 gcEnumEntries: entries,
                                 isReadable: p.IsReadable,
                                 isWritable: p.IsWritable,
                                 visibility: GetVisibility(p),
                                 description: GetProperty(p, "Description"));
    }

    /// <summary>
    /// Gets the property name from a slash-separated name.
    /// </summary>
    /// <param name="fullName">The full name to extract the property name from.</param>
    /// <returns>The last non-empty component of <paramref name="fullName"/>, or <paramref name="fullName"/> if it contains no
    /// non-empty components.</returns>
    private static string GetPropertyName(string fullName)
    {
        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : fullName;
    }

    /// <summary>
    /// Gets the value of a property.
    /// </summary>
    /// <param name="parameter">The parameter whose property is retrieved.</param>
    /// <param name="key">The key of the property to retrieve.</param>
    /// <returns>The property value, or an empty string if the property is unavailable or not found.</returns>
    private static string GetProperty(IParameter parameter, string key)
    {
        return parameter.Advanced?.GetPropertyOrDefault(key, string.Empty) ?? string.Empty;
    }

    /// <summary>
    /// Gets the category of a parameter.
    /// </summary>
    /// <param name="parameter">The parameter whose category is retrieved.</param>
    /// <returns>The parameter's category, or "Root" if no category is specified.</returns>
    private static string GetCategory(IParameter parameter)
    {
        var parts = parameter.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // The last segment is the parameter name, the one before it is its category.
        return parts.Length >= 2 ? parts[^2].Substring(1) : "Root";
    }

    /// <summary>
    /// Gets the visibility level for a parameter.
    /// </summary>
    /// <param name="parameter">The parameter whose visibility level is retrieved.</param>
    /// <returns>The visibility level specified for the parameter, or <see cref="GcVisibility.Beginner"/> if none of the
    /// recognized levels is specified.</returns>
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
