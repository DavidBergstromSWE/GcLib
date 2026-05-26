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
    /// <summary>
    /// GenAPI node map containing all features implemented by the device.
    /// </summary>
    private readonly NodeMap _nodeMap;

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();

        // First, find all categories in the node map to associate them with parameters as we loop through the nodes. This is necessary because the IDS Peak API does not have a direct category association for nodes, but instead has category nodes that invalidate other nodes when they are changed. So we can check which category nodes invalidate a given parameter node to determine its category.
        var categories = new List<string>();
        foreach (var parameter in _nodeMap.Nodes().Where(n => n.Type() == NodeType.Category))
        {
            if (parameter.Name() != "Root" && categories.Contains(parameter.Name()) == false)
                categories.Add(parameter.Name());
        }

        // Loop through all nodes in the node map and create corresponding GcParameter objects for those that are available. If any errors occur while processing a parameter, add its name to the failed parameter list.
        foreach (var parameter in _nodeMap.Nodes())
        {
            // Skip nodes that are not parameters (e.g. categories) or that are not available, not readable, or invisible.
            if (parameter.IsAvailable() == false || parameter.IsFeature() == false || parameter.Visibility() == NodeVisibility.Invisible || parameter.IsReadable() == false)
            {
                failedParameterList.Add(parameter.Name());
                continue;
            }

            // Skip parameters that do not belong to a category, to avoid cluttering with parameters that are not relevant for the user.
            string categoryName;

            try
            {
                switch(parameter.Type())
                {
                    case NodeType.Boolean:
                        var booleanNode = _nodeMap.FindNode<BooleanNode>(parameter.Name());
                        categoryName = categories.Find(category => booleanNode.InvalidatedNodes().Select(node => node.Name()).Contains(category)) ?? "Uncategorized";
                        if (categoryName != "Uncategorized")
                            parameterList.Add(new GcBoolean(name: booleanNode.Name(),
                                                            category: categoryName,
                                                            value: booleanNode.Value(),
                                                            isReadable: booleanNode.IsReadable(),
                                                            isWritable: booleanNode.IsWriteable(),
                                                            visibility: (GcVisibility)booleanNode.Visibility(),
                                                            description: booleanNode.Description()));
                        break;
                    case NodeType.Command:
                        var commandNode = _nodeMap.FindNode<CommandNode>(parameter.Name());
                        categoryName = categories.Find(category => commandNode.InvalidatedNodes().Select(node => node.Name()).Contains(category)) ?? "Uncategorized";
                        if (categoryName != "Uncategorized")
                            parameterList.Add(new GcCommand(name: commandNode.Name(),
                                                            category: categoryName,
                                                            method: commandNode.Execute,
                                                            visibility: (GcVisibility)commandNode.Visibility(),
                                                            description: commandNode.Description()));
                        break;
                    case NodeType.Enumeration:
                        var enumerationNode = _nodeMap.FindNode<EnumerationNode>(parameter.Name());
                        categoryName = categories.Find(category => enumerationNode.InvalidatedNodes().Select(node => node.Name()).Contains(category)) ?? "Uncategorized";
                        if (categoryName != "Uncategorized")
                            parameterList.Add(new GcEnumeration(name: enumerationNode.Name(),
                                                                category: categoryName,
                                                                gcEnumEntry: new GcEnumEntry(enumerationNode.CurrentEntry().SymbolicValue(), enumerationNode.CurrentEntry().Value(), enumerationNode.CurrentEntry().NumericValue()),
                                                                gcEnumEntries: enumerationNode.Entries().Select(entry => new GcEnumEntry(entry.SymbolicValue(), entry.Value(), entry.NumericValue())).ToList(),
                                                                isReadable: enumerationNode.IsReadable(),
                                                                isWritable: enumerationNode.IsWriteable(),
                                                                visibility: (GcVisibility)enumerationNode.Visibility(),
                                                                description: enumerationNode.Description()));
                        break;
                    case NodeType.Float:
                        var floatNode = _nodeMap.FindNode<FloatNode>(parameter.Name());
                        categoryName = categories.Find(category => floatNode.InvalidatedNodes().Select(node => node.Name()).Contains(category)) ?? "Uncategorized";
                        if (categoryName != "Uncategorized")
                            parameterList.Add(new GcFloat(name: floatNode.Name(),
                                                          category: categoryName,
                                                          value: floatNode.Value(),
                                                          min: floatNode.Minimum(),
                                                          max: floatNode.Maximum(),
                                                          increment: floatNode.Increment(),
                                                          unit: floatNode.Unit(),
                                                          displayPrecision: floatNode.DisplayPrecision(),
                                                          isReadable: floatNode.IsReadable(),
                                                          isWritable: floatNode.IsWriteable(),
                                                          visibility: (GcVisibility)floatNode.Visibility(),
                                                          description: floatNode.Description()));
                        break;
                    case NodeType.Integer:
                        var integerNode = _nodeMap.FindNode<IntegerNode>(parameter.Name());
                        categoryName = categories.Find(category => integerNode.InvalidatedNodes().Select(node => node.Name()).Contains(category)) ?? "Uncategorized";
                        if (categoryName != "Uncategorized")
                            parameterList.Add(new GcInteger(name: integerNode.Name(),
                                                            category: categoryName,
                                                            value: integerNode.Value(),
                                                            min: integerNode.Minimum(),
                                                            max: integerNode.Maximum(),
                                                            increment: integerNode.Increment(),
                                                            unit: integerNode.Unit(),
                                                            isReadable: integerNode.IsReadable(),
                                                            isWritable: integerNode.IsWriteable(),
                                                            visibility: (GcVisibility)integerNode.Visibility(),
                                                            description: integerNode.Description()));

                        break;
                    case NodeType.String:
                        var stringNode = _nodeMap.FindNode<StringNode>(parameter.Name());
                        categoryName = categories.Find(category => stringNode.InvalidatedNodes().Select(node => node.Name()).Contains(category)) ?? "Uncategorized";
                        if (categoryName != "Uncategorized")
                            parameterList.Add(new GcString(name: stringNode.Name(),
                                                           category: categoryName,
                                                           value: stringNode.Value(),
                                                           maxLength: stringNode.MaximumLength(),
                                                           isReadable: stringNode.IsReadable(),
                                                           isWritable: stringNode.IsWriteable(),
                                                           visibility: (GcVisibility)stringNode.Visibility(),
                                                           description: stringNode.Description()));
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error occurred while processing parameter '{parameter.Name()}': {ex.Message}");
                failedParameterList.Add(parameter.Name());
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        if (Parameters[parameterName] is GcBoolean gcBoolean)
        {
            var node = _nodeMap.FindNode<BooleanNode>(parameterName);

            return new GcBoolean(name: gcBoolean.Name,
                                 category: gcBoolean.Category,
                                 value: node.Value(),
                                 isReadable: node.IsReadable(),
                                 isWritable: node.IsWriteable(),
                                 visibility: gcBoolean.Visibility,
                                 description: gcBoolean.Description);
        }
        else return null;
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        if (Parameters[parameterName] is GcCommand gcCommand)
        {
            var node = _nodeMap.FindNode<CommandNode>(parameterName);

            return new GcCommand(name: gcCommand.Name,
                                 category: gcCommand.Category,
                                 method: node.Execute,
                                 visibility: gcCommand.Visibility,
                                 description: gcCommand.Description);
        }
        else return null;
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        if (Parameters[parameterName] is GcEnumeration gcEnum)
        {
            var node = _nodeMap.FindNode<EnumerationNode>(parameterName);

            return new GcEnumeration(name: gcEnum.Name,
                                     category: gcEnum.Category,
                                     gcEnumEntry: gcEnum.CurrentEntry,
                                     gcEnumEntries: gcEnum.Entries,
                                     isReadable: node.IsReadable(),
                                     isWritable: node.IsWriteable(),
                                     visibility: gcEnum.Visibility,
                                     description: gcEnum.Description);
        }
        else return null;
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        if (Parameters[parameterName] is GcFloat gcFloat)
        {
            var node = _nodeMap.FindNode<FloatNode>(parameterName);

            return new GcFloat(name: gcFloat.Name,
                               category: gcFloat.Category,
                               value: node.Value(),
                               min: node.Minimum(),
                               max: node.Maximum(),
                               increment: node.Increment(),
                               unit: gcFloat.Unit,
                               displayPrecision: gcFloat.DisplayPrecision,
                               isReadable: node.IsReadable(),
                               isWritable: node.IsWriteable(),
                               visibility: gcFloat.Visibility,
                               description: gcFloat.Description);
        }
        else return null;
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        if (Parameters[parameterName] is GcInteger gcInteger)
        {
            var node = _nodeMap.FindNode<IntegerNode>(parameterName);

            return new GcInteger(name: gcInteger.Name,
                                 category: gcInteger.Category,
                                 value: node.Value(),
                                 min: node.Minimum(),
                                 max: node.Maximum(),
                                 increment: node.Increment(),
                                 unit: gcInteger.Unit,
                                 isReadable: node.IsReadable(),
                                 isWritable: node.IsWriteable(),
                                 visibility: gcInteger.Visibility,
                                 description: gcInteger.Description);
        }
        else return null;
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        if (Parameters[parameterName] is GcString gcString)
        {
            var node = _nodeMap.FindNode<StringNode>(parameterName);

            return new GcString(name: gcString.Name,
                                category: gcString.Category,
                                value: node.Value(),
                                maxLength: gcString.MaxLength,
                                isReadable: node.IsReadable(),
                                isWritable: node.IsWriteable(),
                                visibility: gcString.Visibility,
                                description: gcString.Description);
        }
        else return null;
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
