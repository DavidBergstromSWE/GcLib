using System;
using System.Collections.Generic;
using System.Linq;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the IDSImaging Peak API from IDS Imaging Development Systems.
/// </summary>
public sealed partial class IdsCam
{
    private NodeMap _nodeMap;

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();

        foreach (var parameter in _nodeMap.Nodes())
        {
            if (parameter.IsAvailable() == false)
            {
                failedParameterList.Add(parameter.Name());
                continue;
            }

            try
            {
                switch(parameter.Type())
                {
                    case NodeType.Boolean:
                        var booleanNode = _nodeMap.FindNode<BooleanNode>(parameter.Name());
                        parameterList.Add(new GcBoolean(name: booleanNode.Name(),
                                                        category: _nodeMap.FindNodeCategory(booleanNode.Name()).Name(),
                                                        value: booleanNode.Value(),
                                                        isReadable: booleanNode.IsReadable(),
                                                        isWritable: booleanNode.IsWriteable(),
                                                        visibility: (GcVisibility)booleanNode.Visibility(),
                                                        description: booleanNode.Description()));
                        break;
                    case NodeType.Command:
                        var commandNode = _nodeMap.FindNode<CommandNode>(parameter.Name());
                        parameterList.Add(new GcCommand(name: commandNode.Name(),
                                                        category: _nodeMap.FindNodeCategory(commandNode.Name()).Name(),
                                                        method: commandNode.Execute,
                                                        visibility: (GcVisibility)commandNode.Visibility(),
                                                        description: commandNode.Description()));
                        break;
                    case NodeType.Enumeration:
                        var enumerationNode = _nodeMap.FindNode<EnumerationNode>(parameter.Name());
                        parameterList.Add(new GcEnumeration(name: enumerationNode.Name(),
                                                            category: _nodeMap.FindNodeCategory(enumerationNode.Name()).Name(),
                                                            gcEnumEntry: new GcEnumEntry(enumerationNode.CurrentEntry().SymbolicValue(), enumerationNode.CurrentEntry().Value(), enumerationNode.CurrentEntry().NumericValue()),
                                                            gcEnumEntries: enumerationNode.Entries().Select(entry => new GcEnumEntry(entry.SymbolicValue(), entry.Value(), entry.NumericValue())).ToList(),
                                                            isReadable: enumerationNode.IsReadable(),
                                                            isWritable: enumerationNode.IsWriteable(),
                                                            visibility: (GcVisibility)enumerationNode.Visibility(),
                                                            description: enumerationNode.Description()));
                        break;
                    case NodeType.Float:
                        var floatNode = _nodeMap.FindNode<FloatNode>(parameter.Name());
                        parameterList.Add(new GcFloat(name: floatNode.Name(),
                                                      category: _nodeMap.FindNodeCategory(floatNode.Name()).Name(),
                                                      value: floatNode.Value(),
                                                      min: floatNode.Minimum(),
                                                      max: floatNode.Maximum(),
                                                      isReadable: floatNode.IsReadable(),
                                                      isWritable: floatNode.IsWriteable(),
                                                      visibility: (GcVisibility)floatNode.Visibility(),
                                                      description: floatNode.Description()));
                        break;
                    case NodeType.Integer:
                        var integerNode = _nodeMap.FindNode<IntegerNode>(parameter.Name());
                        parameterList.Add(new GcInteger(name: integerNode.Name(),
                                                        category: _nodeMap.FindNodeCategory(integerNode.Name()).Name(),
                                                        value: integerNode.Value(),
                                                        min: integerNode.Minimum(),
                                                        max: integerNode.Maximum(),
                                                        isReadable: integerNode.IsReadable(),
                                                        isWritable: integerNode.IsWriteable(),
                                                        visibility: (GcVisibility)integerNode.Visibility(),
                                                        description: integerNode.Description()));

                        break;
                    case NodeType.String:
                        var stringNode = _nodeMap.FindNode<StringNode>(parameter.Name());
                        parameterList.Add(new GcString(name: stringNode.Name(),
                                                       category: _nodeMap.FindNodeCategory(stringNode.Name()).Name(),
                                                       value: stringNode.Value(),
                                                       maxLength: stringNode.MaximumLength(),
                                                       isReadable: stringNode.IsReadable(),
                                                       isWritable: stringNode.IsWriteable(),
                                                       visibility: (GcVisibility)stringNode.Visibility(),
                                                       description: stringNode.Description()));
                        break;
                }
            }
            catch (Exception)
            {
                failedParameterList.Add(parameter.Name());
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        var param = _nodeMap.FindNode<BooleanNode>(parameterName);
        return new GcBoolean(name: param.Name(),
                             category: _nodeMap.FindNodeCategory(param.Name()).Name(),
                             value: param.Value(),
                             isReadable: param.IsReadable(),
                             isWritable: param.IsWriteable(),
                             visibility: (GcVisibility)param.Visibility(),
                             description: param.Description());
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        var param = _nodeMap.FindNode<CommandNode>(parameterName);
        return new GcCommand(name: param.Name(),
                             category: _nodeMap.FindNodeCategory(param.Name()).Name(),
                             method: param.Execute,
                             visibility: (GcVisibility)param.Visibility(),
                             description: param.Description());
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        var param = _nodeMap.FindNode<EnumerationNode>(parameterName);
        return new GcEnumeration(name: param.Name(),
                                 category: _nodeMap.FindNodeCategory(param.Name()).Name(),
                                 gcEnumEntry: new GcEnumEntry(param.CurrentEntry().SymbolicValue(), param.CurrentEntry().Value(), param.CurrentEntry().NumericValue()),
                                 gcEnumEntries: [.. param.Entries().Select(entry => new GcEnumEntry(entry.SymbolicValue(), entry.Value(), entry.NumericValue()))],
                                 isReadable: param.IsReadable(),
                                 isWritable: param.IsWriteable(),
                                 visibility: (GcVisibility)param.Visibility(),
                                 description: param.Description());
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        var param = _nodeMap.FindNode<FloatNode>(parameterName);
        return new GcFloat(name: param.Name(),
                           category: _nodeMap.FindNodeCategory(param.Name()).Name(),
                           value: param.Value(),
                           min: param.Minimum(),
                           max: param.Maximum(),
                           increment: param.Increment(),
                           unit: param.Unit(),
                           displayPrecision: param.DisplayPrecision(),
                           isReadable: param.IsReadable(),
                           isWritable: param.IsWriteable(),
                           visibility: (GcVisibility)param.Visibility(),
                           description: param.Description());
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        var param = _nodeMap.FindNode<IntegerNode>(parameterName);
        return new GcInteger(name: param.Name(),
                             category: _nodeMap.FindNodeCategory(param.Name()).Name(),
                             value: param.Value(),
                             min: param.Minimum(),
                             max: param.Maximum(),
                             increment: param.Increment(),
                             isReadable: param.IsReadable(),
                             isWritable: param.IsWriteable(),
                             visibility: (GcVisibility)param.Visibility(),
                             description: param.Description());
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        var param = _nodeMap.FindNode<StringNode>(parameterName);
        return new GcString(name: param.Name(),
                            category: _nodeMap.FindNodeCategory(param.Name()).Name(),
                            value: param.Value(),
                            maxLength: param.MaximumLength(),
                            isReadable: param.IsReadable(),
                            isWritable: param.IsWriteable(),
                            visibility: (GcVisibility)param.Visibility(),
                            description: param.Description());
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        switch (Parameters[parameterName].Type)
        {
            case GcParameterType.Boolean:
                var booleanNode = _nodeMap.FindNode<BooleanNode>(parameterName);
                return booleanNode.IsReadable()? booleanNode.Value().ToString() : null;
            case GcParameterType.Enumeration:
                var enumerationNode = _nodeMap.FindNode<EnumerationNode>(parameterName);
                return enumerationNode.IsReadable()? enumerationNode.CurrentEntry().SymbolicValue() : null;
            case GcParameterType.Float:
                var floatNode = _nodeMap.FindNode<FloatNode>(parameterName);
                return floatNode.IsReadable()? floatNode.Value().ToString() : null;
            case GcParameterType.Integer:
                var integerNode = _nodeMap.FindNode<IntegerNode>(parameterName);
                return integerNode.IsReadable()? integerNode.Value().ToString() : null;
            case GcParameterType.String:
                var stringNode = _nodeMap.FindNode<StringNode>(parameterName);
                return stringNode.IsReadable()? stringNode.Value() : null;
            default:
                return string.Empty;
        }
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        switch (Parameters[parameterName].Type)
        {
            case GcParameterType.Boolean:
                var booleanNode = _nodeMap.FindNode<BooleanNode>(parameterName);
                if (booleanNode.IsWriteable())
                    booleanNode.SetValue(parameterValue == bool.TrueString);
                break;
            case GcParameterType.Enumeration:
                var enumerationNode = _nodeMap.FindNode<EnumerationNode>(parameterName);
                if (enumerationNode.IsWriteable())
                    enumerationNode.SetCurrentEntry(parameterValue);
                break;
            case GcParameterType.Float:
                var floatNode = _nodeMap.FindNode<FloatNode>(parameterName);
                if (floatNode.IsWriteable())
                    floatNode.SetValue(float.Parse(parameterValue));
                break;
            case GcParameterType.Integer:
                var integerNode = _nodeMap.FindNode<IntegerNode>(parameterName);
                if (integerNode.IsWriteable())
                    integerNode.SetValue(int.Parse(parameterValue));
                break;
            case GcParameterType.String:
                var stringNode = _nodeMap.FindNode<StringNode>(parameterName);
                if (stringNode.IsWriteable())
                    stringNode.SetValue(parameterValue);
                break;
            default:
                break;
        }
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        if (Parameters[parameterName].Type == GcParameterType.Command)
        {
            var commandParam = _nodeMap.FindNode<CommandNode>(parameterName);
            if (commandParam.IsAvailable()) commandParam.Execute();
        }
    }
}
