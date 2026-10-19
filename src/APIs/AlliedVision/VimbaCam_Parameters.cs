using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VmbNET;

namespace GcLib;

public sealed partial class VimbaCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    protected override GcBoolean GetBoolean(string parameterName)
    {
        var feature = (IBoolFeature)GetFeature(parameterName);
        return feature.IsReadable() ? ToGcBoolean(feature) : Parameters[parameterName] as GcBoolean;
    }

    protected override GcCommand GetCommand(string parameterName)
    {
        return ToGcCommand((ICommandFeature)GetFeature(parameterName));
    }

    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        var feature = (IEnumFeature)GetFeature(parameterName);
        return feature.IsReadable() ? ToGcEnumeration(feature) : Parameters[parameterName] as GcEnumeration;
    }

    protected override GcFloat GetFloat(string parameterName)
    {
        var feature = (IFloatFeature)GetFeature(parameterName);
        return feature.IsReadable() ? ToGcFloat(feature) : Parameters[parameterName] as GcFloat;
    }

    protected override GcInteger GetInteger(string parameterName)
    {
        var feature = (IIntFeature)GetFeature(parameterName);
        return feature.IsReadable() ? ToGcInteger(feature) : Parameters[parameterName] as GcInteger;
    }

    protected override GcString GetString(string parameterName)
    {
        var feature = (IStringFeature)GetFeature(parameterName);
        return feature.IsReadable() ? ToGcString(feature) : Parameters[parameterName] as GcString;
    }

    protected override string GetParameterValue(string parameterName)
    {
        var feature = GetFeature(parameterName);
        if (feature == null)
            return null;

        if (!feature.IsReadable())
            return Parameters[parameterName].ToString();

        return feature.Type switch
        {
            IFeature.TypeValue.Int => ((IIntFeature)feature).Value.ToString(CultureInfo.CurrentCulture),
            IFeature.TypeValue.Float => ((IFloatFeature)feature).Value.ToString("F3", CultureInfo.CurrentCulture),
            IFeature.TypeValue.Enum => ((IEnumFeature)feature).Value,
            IFeature.TypeValue.String => ((IStringFeature)feature).Value,
            IFeature.TypeValue.Bool => ((IBoolFeature)feature).Value.ToString(),
            IFeature.TypeValue.Command => Parameters[parameterName].ToString(),
            _ => null
        };
    }

    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        var feature = GetFeature(parameterName);
        if (feature == null || !feature.IsWriteable())
            return;

        switch (feature.Type)
        {
            case IFeature.TypeValue.Int:
                ((IIntFeature)feature).Value = long.Parse(parameterValue, CultureInfo.CurrentCulture);
                break;
            case IFeature.TypeValue.Float:
                ((IFloatFeature)feature).Value = double.Parse(parameterValue, CultureInfo.CurrentCulture);
                break;
            case IFeature.TypeValue.Enum:
                ((IEnumFeature)feature).Value = parameterValue;
                break;
            case IFeature.TypeValue.String:
                ((IStringFeature)feature).Value = parameterValue;
                break;
            case IFeature.TypeValue.Bool:
                ((IBoolFeature)feature).Value = bool.Parse(parameterValue);
                break;
        }
    }

    protected override void ExecuteParameterCommand(string parameterName)
    {
        if (GetFeature(parameterName) is ICommandFeature command)
            command.Run();
    }

    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameters = new List<GcParameter>();
        var failedParameters = new List<string>();

        foreach (var dynamicFeature in _camera.Features)
        {
            IFeature feature = dynamicFeature;
            try
            {
                GcParameter parameter = feature.Type switch
                {
                    IFeature.TypeValue.Int when feature.IsReadable() => ToGcInteger((IIntFeature)feature),
                    IFeature.TypeValue.Float when feature.IsReadable() => ToGcFloat((IFloatFeature)feature),
                    IFeature.TypeValue.String when feature.IsReadable() => ToGcString((IStringFeature)feature),
                    IFeature.TypeValue.Bool when feature.IsReadable() => ToGcBoolean((IBoolFeature)feature),
                    IFeature.TypeValue.Enum when feature.IsReadable() => ToGcEnumeration((IEnumFeature)feature),
                    IFeature.TypeValue.Command when feature.Visibility != IFeature.VisibilityValue.Invisible => ToGcCommand((ICommandFeature)feature),
                    _ => null
                };

                if (parameter != null)
                    parameters.Add(parameter);
            }
            catch
            {
                failedParameters.Add(feature.Name);
            }
        }

        return new GcDeviceParameterCollection(this, parameters, failedParameters);
    }

    private IFeature GetFeature(string parameterName)
    {
        try
        {
            IFeature feature = _camera.Features[parameterName];
            return feature;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static GcInteger ToGcInteger(IIntFeature feature)
    {
        var validValues = feature.HasValidValues ? feature.ValidValues.ToList() : [];
        bool hasListIncrement = validValues.Count > 0;

        return new GcInteger(feature.Name,
                             GetCategory(feature),
                             feature.Value,
                             feature.Minimum,
                             feature.Maximum,
                             feature.Increment,
                             hasListIncrement ? EIncMode.listIncrement : EIncMode.fixedIncrement,
                             hasListIncrement ? validValues : null,
                             feature.Unit,
                             feature.IsReadable(),
                             feature.IsWriteable(),
                             GetVisibility(feature),
                             feature.Description);
    }

    private static GcFloat ToGcFloat(IFloatFeature feature)
    {
        return new GcFloat(feature.Name,
                           GetCategory(feature),
                           feature.Value,
                           feature.Minimum,
                           feature.Maximum,
                           feature.HasIncrement ? feature.Increment : 0,
                           feature.Unit,
                           isReadable: feature.IsReadable(),
                           isWritable: feature.IsWriteable(),
                           visibility: GetVisibility(feature),
                           description: feature.Description);
    }

    private static GcString ToGcString(IStringFeature feature)
    {
        return new GcString(feature.Name,
                            GetCategory(feature),
                            feature.Value,
                            feature.MaximumLength,
                            feature.IsReadable(),
                            feature.IsWriteable(),
                            GetVisibility(feature),
                            feature.Description);
    }

    private static GcBoolean ToGcBoolean(IBoolFeature feature)
    {
        return new GcBoolean(feature.Name,
                             GetCategory(feature),
                             feature.Value,
                             feature.IsReadable(),
                             feature.IsWriteable(),
                             GetVisibility(feature),
                             feature.Description);
    }

    private static GcCommand ToGcCommand(ICommandFeature feature)
    {
        return new GcCommand(feature.Name,
                             GetCategory(feature),
                             feature.Run,
                             isWritable: feature.IsWriteable(),
                             visibility: GetVisibility(feature),
                             description: feature.Description);
    }

    private static GcEnumeration ToGcEnumeration(IEnumFeature feature)
    {
        var entries = feature.EnumEntriesByName.Values
            .Where(entry => entry.IsAvailable)
            .Select(entry => new GcEnumEntry(entry.Name, entry.IntValue))
            .ToList();

        var currentEntry = feature.EnumEntry;
        if (entries.All(entry => entry.ValueString != feature.Value))
            entries.Add(new GcEnumEntry(feature.Value, currentEntry.IntValue));

        return new GcEnumeration(feature.Name,
                                 GetCategory(feature),
                                 entries.First(entry => entry.ValueString == feature.Value),
                                 entries,
                                 feature.IsReadable(),
                                 feature.IsWriteable(),
                                 GetVisibility(feature),
                                 feature.Description);
    }

    private static string GetCategory(IFeature feature)
    {
        var category = feature.Category;
        if (string.IsNullOrEmpty(category))
            return "Root";

        var parts = category.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : "Root";
    }

    private static GcVisibility GetVisibility(IFeature feature)
    {
        return feature.Visibility switch
        {
            IFeature.VisibilityValue.Expert => GcVisibility.Expert,
            IFeature.VisibilityValue.Guru => GcVisibility.Guru,
            IFeature.VisibilityValue.Invisible => GcVisibility.Invisible,
            _ => GcVisibility.Beginner
        };
    }
}
