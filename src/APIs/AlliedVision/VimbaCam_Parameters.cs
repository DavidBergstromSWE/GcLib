using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VmbNET;

namespace GcLib;

public sealed partial class VimbaCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Protected Methods

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        if (_camera.Features[parameterName] is not IBoolFeature feature)
            return null; // Return null if the feature is not found or not a boolean feature.
        return feature.IsReadable() ? ToGcBoolean(feature) : Parameters[parameterName] as GcBoolean;
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        if (_camera.Features[parameterName] is not ICommandFeature feature)
            return null; // Return null if the feature is not found or not a command feature.
        return ToGcCommand(feature);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        if (_camera.Features[parameterName] is not IEnumFeature feature)
            return null; // Return null if the feature is not found or not an enumeration feature.
        return feature.IsReadable() ? ToGcEnumeration(feature) : Parameters[parameterName] as GcEnumeration;
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        if (_camera.Features[parameterName] is not IFloatFeature feature)
            return null; // Return null if the feature is not found or not a float feature.
        return feature.IsReadable() ? ToGcFloat(feature) : Parameters[parameterName] as GcFloat;
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        if (_camera.Features[parameterName] is not IIntFeature feature)
            return null; // Return null if the feature is not found or not an integer feature.
        return feature.IsReadable() ? ToGcInteger(feature) : Parameters[parameterName] as GcInteger;
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        if (_camera.Features[parameterName] is not IStringFeature feature)
            return null; // Return null if the feature is not found or not a string feature.
        return feature.IsReadable() ? ToGcString(feature) : Parameters[parameterName] as GcString;
    }
    
    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        // Retrieve the feature associated with the parameter name.
        var feature = GetFeature(parameterName);
        if (feature == null || feature is ICommandFeature)
            return null; // Return null if the feature is not found or if it is a command feature.

        // If the feature is not readable, return the stored parameter value.
        if (feature.IsReadable() == false)
            return Parameters[parameterName].ToString();

        // Return the string representation of the feature's value based on its type.
        return feature.Type switch
        {
            IFeature.TypeValue.Int => ((IIntFeature)feature).Value.ToString(CultureInfo.CurrentCulture),
            IFeature.TypeValue.Float => ((IFloatFeature)feature).Value.ToString("F3", CultureInfo.CurrentCulture),
            IFeature.TypeValue.Enum => ((IEnumFeature)feature).Value,
            IFeature.TypeValue.String => ((IStringFeature)feature).Value,
            IFeature.TypeValue.Bool => ((IBoolFeature)feature).Value.ToString(),
            _ => null
        };
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        // Retrieve the feature associated with the parameter name.
        var feature = GetFeature(parameterName);

        // Return if the feature is not found, not writable, or is a command feature (which cannot be set).
        if (feature == null || feature.IsWriteable() == false || feature.Type == IFeature.TypeValue.Command)
            return;

        // Set the feature's value based on its type, parsing the provided string value accordingly.
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

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        // Retrieve the feature associated with the parameter name and execute it if it is a command feature.
        if (GetFeature(parameterName) is ICommandFeature command)
            command.Run();
    }

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameters = new List<GcParameter>();
        var failedParameters = new List<string>();

        // Iterate through all features of the camera and convert them to GcParameter instances.
        foreach (var dynamicFeature in _camera.Features)
        {
            IFeature feature = dynamicFeature; // Get the feature from the dynamic feature.
            if (feature.Visibility == IFeature.VisibilityValue.Invisible || feature.IsReadable() == false)
                continue; // Skip invisible or non-readable features.
            try
            {
                GcParameter parameter = feature.Type switch
                {
                    IFeature.TypeValue.Int => ToGcInteger((IIntFeature)feature),
                    IFeature.TypeValue.Float => ToGcFloat((IFloatFeature)feature),
                    IFeature.TypeValue.String => ToGcString((IStringFeature)feature),
                    IFeature.TypeValue.Bool => ToGcBoolean((IBoolFeature)feature),
                    IFeature.TypeValue.Enum => ToGcEnumeration((IEnumFeature)feature),
                    IFeature.TypeValue.Command => ToGcCommand((ICommandFeature)feature),
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

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the camera feature identified by the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that identifies the feature to retrieve.</param>
    /// <returns>The matching <see cref="IFeature"/> instance if found; otherwise, <see langword="null"/>.</returns>
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

    /// <summary>
    /// Converts an <see cref="IIntFeature"/> into a <see cref="GcInteger"/>.
    /// </summary>
    /// <param name="feature">Integer feature to convert.</param>
    /// <returns><see cref="GcInteger"/> representing the feature.</returns>
    private static GcInteger ToGcInteger(IIntFeature feature)
    {
        var validValues = feature.HasValidValues ? feature.ValidValues.ToList() : []; // Convert valid values to a list if available, otherwise use an empty list.
        bool hasListIncrement = validValues.Count > 0; // Determine if the feature has a list of valid values.

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

    /// <summary>
    /// Converts an <see cref="IFloatFeature"/> into a <see cref="GcFloat"/>.
    /// </summary>
    /// <param name="feature">Floating-point feature to convert.</param>
    /// <returns><see cref="GcFloat"/> representing the feature.</returns>
    private static GcFloat ToGcFloat(IFloatFeature feature)
    {
        return new GcFloat(feature.Name,
                           GetCategory(feature),
                           feature.Value,
                           feature.Minimum,
                           feature.Maximum,
                           feature.HasIncrement ? feature.Increment : 0, // Use 0 as a default increment if the feature does not have an increment.
                           feature.Unit,
                           isReadable: feature.IsReadable(),
                           isWritable: feature.IsWriteable(),
                           visibility: GetVisibility(feature),
                           description: feature.Description);
    }

    /// <summary>
    /// Converts an <see cref="IStringFeature"/> into a <see cref="GcString"/>.
    /// </summary>
    /// <param name="feature">String feature to convert.</param>
    /// <returns><see cref="GcString"/> representing the feature.</returns>
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

    /// <summary>
    /// Converts an <see cref="IBoolFeature"/> into a <see cref="GcBoolean"/>.
    /// </summary>
    /// <param name="feature">Boolean feature to convert.</param>
    /// <returns><see cref="GcBoolean"/> representing the feature.</returns>
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

    /// <summary>
    /// Converts an <see cref="ICommandFeature"/> into a <see cref="GcCommand"/>.
    /// </summary>
    /// <param name="feature">Command feature to convert.</param>
    /// <returns><see cref="GcCommand"/> representing the feature.</returns>
    private static GcCommand ToGcCommand(ICommandFeature feature)
    {
        return new GcCommand(feature.Name,
                             GetCategory(feature),
                             feature.Run,
                             isWritable: feature.IsWriteable(),
                             visibility: GetVisibility(feature),
                             description: feature.Description);
    }

    /// <summary>
    /// Converts an <see cref="IEnumFeature"/> into a <see cref="GcEnumeration"/>.
    /// </summary>
    /// <param name="feature">Enumeration feature to convert.</param>
    /// <returns><see cref="GcEnumeration"/> representing the feature.</returns>
    private static GcEnumeration ToGcEnumeration(IEnumFeature feature)
    {
        // Create a list of GcEnumEntry instances for each available enumeration entry in the feature.
        var entries = feature.EnumEntriesByName.Values 
            .Where(entry => entry.IsAvailable) // Filter only available entries
            .Select(entry => new GcEnumEntry(entry.Name, entry.IntValue))
            .ToList();

        return new GcEnumeration(feature.Name,
                                 GetCategory(feature),
                                 entries.First(entry => entry.ValueString == feature.Value), // Set the current entry based on the feature's current value.
                                 entries,
                                 feature.IsReadable(),
                                 feature.IsWriteable(),
                                 GetVisibility(feature),
                                 feature.Description);
    }

    /// <summary>
    /// Gets category of feature.
    /// </summary>
    /// <param name="feature">Feature to get the category for.</param>
    /// <returns>Category of feature or "Root" if category not found.</returns>
    private static string GetCategory(IFeature feature)
    {
        var category = feature.Category;
        if (string.IsNullOrEmpty(category))
            return "Root";

        // Split the category path by '/' and return the last non-empty segment.
        var parts = category.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : "Root";
    }

    /// <summary>
    /// Maps a feature visibility value to the corresponding <see cref="GcVisibility"/>.
    /// </summary>
    /// <param name="feature">Feature whose visibility is converted.</param>
    /// <returns>The corresponding <see cref="GcVisibility"/> value.</returns>
    private static GcVisibility GetVisibility(IFeature feature)
    {
        return feature.Visibility switch
        {
            IFeature.VisibilityValue.Expert => GcVisibility.Expert,
            IFeature.VisibilityValue.Guru => GcVisibility.Guru,
            IFeature.VisibilityValue.Invisible => GcVisibility.Invisible,
            IFeature.VisibilityValue.Beginner => GcVisibility.Beginner,
            _ => throw new Exception($"Unknown visibility value: {feature.Visibility}")
        };
    }

    #endregion
}
