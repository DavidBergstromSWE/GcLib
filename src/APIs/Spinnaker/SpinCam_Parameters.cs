using SpinnakerNET.GenApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GcLib;

public sealed partial class SpinCam : GcDevice, IDeviceEnumerator
{
    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        var param = _nodeMap.GetNode<BoolNode>(parameterName);
        return new GcBoolean(name: param.Name,
                             category: param.NameSpace.ToString(),
                             value: param.Value,
                             isReadable: param.IsReadable,
                             isWritable: param.IsWritable,
                             visibility: (GcVisibility)param.Visibility,
                             description: param.Description);
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        var param = _nodeMap.GetNode<Command>(parameterName);
        return new GcCommand(name: param.Name,
                             category: param.NameSpace.ToString(),
                             method: param.Execute,
                             isReadable: param.IsReadable,
                             isWritable: param.IsWritable,
                             visibility: (GcVisibility)param.Visibility,
                             description: param.Description);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        var param = _nodeMap.GetNode<Enumeration>(parameterName);
        return new GcEnumeration(name: param.Name,
                                 category: param.NameSpace.ToString(),
                                 gcEnumEntry: new GcEnumEntry(param.Value.String, param.Value.Int),
                                 gcEnumEntries: GetEnumEntries(param),
                                 isReadable: param.IsReadable,
                                 isWritable: param.IsWritable,
                                 visibility: (GcVisibility)param.Visibility,
                                 description: param.Description);
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        var param = _nodeMap.GetNode<Float>(parameterName);
        return new GcFloat(name: param.Name,
                           category: param.NameSpace.ToString(),
                           value: param.Value,
                           min: param.Min,
                           max: param.Max,
                           increment: param.GetType().GetMethod("Increment") != null ? param.Increment : 0,
                           isReadable: param.IsReadable,
                           isWritable: param.IsWritable,
                           visibility: (GcVisibility)param.Visibility,
                           description: param.Description);
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        var param = _nodeMap.GetNode<Integer>(parameterName);
        return new GcInteger(name: param.Name,
                             category: param.NameSpace.ToString(),
                             value: param.Value,
                             min: param.Min,
                             max: param.Max,
                             increment: param.Increment,
                             isReadable: param.IsReadable,
                             isWritable: param.IsWritable,
                             visibility: (GcVisibility)param.Visibility,
                             description: param.Description);
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        var param = _nodeMap.GetNode<StringNode>(parameterName);
        return new GcString(name: param.Name,
                            category: param.NameSpace.ToString(),
                            value: param.Value,
                            maxLength: 100,
                            isReadable: param.IsReadable,
                            isWritable: param.IsWritable,
                            visibility: (GcVisibility)param.Visibility,
                            description: param.Description);
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        switch (Parameters[parameterName].Type)
        {
            case GcParameterType.Integer:
                var intParam = _nodeMap.GetNode<Integer>(parameterName);
                return intParam.IsReadable ? intParam.Value.ToString() : Parameters[parameterName].ToString();
            case GcParameterType.Float:
                var floatParam = _nodeMap.GetNode<Float>(parameterName);
                return floatParam.IsReadable ? floatParam.Value.ToString() : Parameters[parameterName].ToString();
            case GcParameterType.String:
                var stringParam = _nodeMap.GetNode<StringNode>(parameterName);
                return stringParam.IsReadable ? stringParam.Value : Parameters[parameterName].ToString();
            case GcParameterType.Enumeration:
                var enumParam = _nodeMap.GetNode<Enumeration>(parameterName);
                return enumParam.IsReadable ? _nodeMap.GetNode<Enumeration>(parameterName).Value.String : Parameters[parameterName].ToString();
            case GcParameterType.Boolean:
                var boolParam = _nodeMap.GetNode<BoolNode>(parameterName);
                return boolParam.IsReadable ? _nodeMap.GetNode<BoolNode>(parameterName).Value.ToString() : Parameters[parameterName].ToString();
            case GcParameterType.Command:
            default:
                return null;
        }
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        if (Parameters[parameterName].Type == GcParameterType.Command)
        {
            var commandParam = _nodeMap.GetNode<Command>(parameterName);
            if (commandParam.IsAvailable) commandParam.Execute();
        }
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        switch (Parameters[parameterName].Type)
        {
            case GcParameterType.Integer:
                var intParam = _nodeMap.GetNode<Integer>(parameterName);
                if (intParam.IsWritable) intParam.FromString(parameterValue);
                break;
            case GcParameterType.Float:
                var floatParam = _nodeMap.GetNode<Float>(parameterName);
                if (floatParam.IsWritable) floatParam.FromString(parameterValue);
                break;
            case GcParameterType.String:
                var stringParam = _nodeMap.GetNode<StringNode>(parameterName);
                if (stringParam.IsWritable) stringParam.FromString(parameterValue);
                break;
            case GcParameterType.Enumeration:
                var enumParam = _nodeMap.GetNode<Enumeration>(parameterName);
                if (enumParam.IsWritable) enumParam.FromString(parameterValue);
                break;
            case GcParameterType.Boolean:
                var boolParam = _nodeMap.GetNode<BoolNode>(parameterName);
                if (boolParam.IsWritable) boolParam.FromString(parameterValue);
                break;
            case GcParameterType.Command:
            default:
                break;
        }

        // Update list.
        //Parameters.Update();
    }

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();

        // Traverse node map by finding categories and adding children parameters to list.
        foreach (var categoryNode in _nodeMap.Values.Where(n => n.GetType() == typeof(Category) && n.IsReadable))
        {
            var category = _nodeMap.GetNode<Category>(categoryNode.Name);
            foreach (var feature in category.Features)
            {
                try
                {
                    if (feature.GetType() == typeof(Integer))
                    {
                        var parameter = _nodeMap.GetNode<Integer>(feature.Name);
                        if (parameter.IsReadable)
                        {
                            var gcParameter = new GcInteger(name: parameter.Name,
                                                        category: category.Name,
                                                        value: parameter.Value,
                                                        min: parameter.Min,
                                                        max: parameter.Max,
                                                        increment: parameter.Increment,
                                                        isReadable: parameter.IsReadable,
                                                        isWritable: parameter.IsWritable,
                                                        visibility: (GcVisibility)parameter.Visibility,
                                                        description: parameter.Description);
                            parameterList.Add(gcParameter);
                        }
                    }

                    if (feature.GetType() == typeof(Float))
                    {
                        var param = _nodeMap.GetNode<Float>(feature.Name); param.Updated += Param_Updated;
                        if (param.IsReadable)
                        {
                            var gcParam = new GcFloat(name: param.Name,
                                                      category: category.Name,
                                                      value: param.Value,
                                                      min: param.Min,
                                                      max: param.Max,
                                                      increment: param.GetType().GetMethod("Increment") != null ? param.Increment : 0,
                                                      isReadable: param.IsReadable,
                                                      isWritable: param.IsWritable,
                                                      visibility: (GcVisibility)param.Visibility,
                                                      description: param.Description);
                            parameterList.Add(gcParam);
                        }
                    }

                    if (feature.GetType() == typeof(StringNode))
                    {
                        var param = _nodeMap.GetNode<StringNode>(feature.Name);
                        if (param.IsReadable)
                        {
                            var gcParam = new GcString(name: param.Name,
                                                       category: category.Name,
                                                       value: param.Value,
                                                       maxLength: 100,
                                                       isReadable: param.IsReadable,
                                                       isWritable: param.IsWritable,
                                                       visibility: (GcVisibility)param.Visibility,
                                                       description: param.Description);
                            parameterList.Add(gcParam);
                        }
                    }

                    if (feature.GetType() == typeof(BoolNode))
                    {
                        var param = _nodeMap.GetNode<BoolNode>(feature.Name);
                        if (param.IsReadable)
                        {
                            var gcParam = new GcBoolean(name: param.Name,
                                                        category: category.Name,
                                                        value: param.Value,
                                                        isReadable: param.IsReadable,
                                                        isWritable: param.IsWritable,
                                                        visibility: (GcVisibility)param.Visibility,
                                                        description: param.Description);
                            parameterList.Add(gcParam);
                        }
                    }

                    if (feature.GetType() == typeof(Command))
                    {
                        var param = _nodeMap.GetNode<Command>(feature.Name);
                        if (param.Visibility != Visibility.Invisible)
                        {
                            var gcParam = new GcCommand(name: param.Name,
                                                        category: category.Name,
                                                        method: param.Execute,
                                                        isReadable: param.IsReadable,
                                                        isWritable: param.IsWritable,
                                                        visibility: (GcVisibility)param.Visibility,
                                                        description: param.Description);
                            parameterList.Add(gcParam);
                        }
                    }

                    if (feature.GetType() == typeof(Enumeration))
                    {
                        var param = _nodeMap.GetNode<Enumeration>(feature.Name);
                        if (param.IsReadable)
                        {
                            var gcParam = new GcEnumeration(name: param.Name,
                                                            category: category.Name,
                                                            gcEnumEntry: new GcEnumEntry(param.Value.String, param.Value.Int),
                                                            gcEnumEntries: GetEnumEntries(param),
                                                            isReadable: param.IsReadable,
                                                            isWritable: param.IsWritable,
                                                            visibility: (GcVisibility)param.Visibility,
                                                            description: param.Description);
                            parameterList.Add(gcParam);
                        }
                    }

                }
                catch (Exception)
                {
                    failedParameterList.Add(feature.Name);
                }
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    private void Param_Updated(INode node)
    {
        // Update parameter collection here?
    }

    /// <summary>
    /// Converts <see cref="Enumeration"/> to a list of possible <see cref="GcEnumEntry"/> entries (for a <see cref="GcEnumeration"/> object.
    /// </summary>
    /// <param name="enumeration">Enumeration.</param>
    /// <returns>List of possible enum entries.</returns>
    private static List<GcEnumEntry> GetEnumEntries(Enumeration enumeration)
    {
        var list = new List<GcEnumEntry>(enumeration.Entries.Length);
        for (int i = 0; i < enumeration.Entries.Length; i++)
            list.Add(new GcEnumEntry(enumeration.Entries[i].Symbolic, enumeration.Entries[i].Value));
        return list;
    }
}
