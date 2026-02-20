using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace GcLib.Utilities.Collections;

/// <summary>
/// Represents a read-only collection of parameters of type <see cref="GcParameter"/>.
/// </summary>
/// <remarks>
/// Creates a new read-only collection of parameters.
/// </remarks>
/// <param name="collectionName">Name of collection.</param>
/// <param name="parameters">Parameters to include in collection.</param>
public sealed class ReadOnlyParameterCollection(string collectionName, IEnumerable<GcParameter> parameters) : IReadOnlyParameterCollection
{
    /// <summary>
    /// List of parameters in the collection.
    /// </summary>
    private readonly IReadOnlyList<GcParameter> _parameters = [.. parameters];

    /// <inheritdoc/>
    public string Name { get; set; } = collectionName;

    /// <inheritdoc/>
    public IReadOnlyList<GcParameter> ToList()
    {
        return _parameters;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GcParameter> ToList(GcVisibility visibility = GcVisibility.Guru)
    {
        return [.. _parameters.Where(p => p.Visibility <= visibility)];
    }

    /// <summary>
    /// Number of parameters in collection.
    /// </summary>
    public int Count => _parameters.Count;

    /// <inheritdoc/>
    public GcParameter this[int i] => _parameters[i];

    /// <inheritdoc/>
    public GcParameter this[string parameterName] => _parameters.FirstOrDefault(p => p.Name == parameterName) ?? throw new KeyNotFoundException($"Device does not implement a parameter with name {parameterName}!");

    /// <inheritdoc/>
    public string GetParameterValue(string parameterName)
    {
        return IsImplemented(parameterName) ? this[parameterName].ToString() : null;
    }

    /// <inheritdoc/>
    public void SetParameterValue(string parameterName, string parameterValue)
    {
        if (IsImplemented(parameterName) && this[parameterName].Type != GcParameterType.Command)
        {
            try
            {
                this[parameterName].FromString(parameterValue);

                // Log debugging info.
                if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
                    GcLibrary.Logger.LogDebug("{parameterName} set to {getValue} in {container}", parameterName, GetParameterValue(parameterName), Name);
            }
            catch (Exception ex)
            {
                if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                    GcLibrary.Logger.LogError(ex, "Failed to set {parameterName} in {container}", parameterName, Name);
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public void ExecuteParameterCommand(string parameterName)
    {
        if (IsImplemented(parameterName))
        {
            try
            {
                if (this[parameterName] is GcCommand parameter)
                {
                    parameter.Execute();

                    // Log debugging info.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
                        GcLibrary.Logger.LogDebug("{parameterName} executed in {container}", parameterName, Name);
                }
            }
            catch (Exception ex)
            {
                if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                    GcLibrary.Logger.LogError(ex, "Failed to execute {parameterName} in {container}", parameterName, Name);
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public GcParameter GetParameter(string parameterName)
    {
        if (IsImplemented(parameterName))
        {
            return this[parameterName].Type switch
            {
                GcParameterType.Integer => this[parameterName] as GcInteger,
                GcParameterType.Float => this[parameterName] as GcFloat,
                GcParameterType.String => this[parameterName] as GcString,
                GcParameterType.Enumeration => this[parameterName] as GcEnumeration,
                GcParameterType.Boolean => this[parameterName] as GcBoolean,
                GcParameterType.Command => this[parameterName] as GcCommand,
                _ => null,
            };
        }
        else
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public void Update()
    {
        return;
    }

    /// <inheritdoc/>
    public IEnumerator<GcParameter> GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    public bool IsImplemented(string parameterName)
    {
        return _parameters.FirstOrDefault(p => p.Name == parameterName) != null;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetCategories(GcVisibility visibility)
    {
        return _parameters.Where(p => p.Visibility <= visibility).Select(p => p.Category).Distinct();
    }

    /// <inheritdoc/>
    public IReadOnlyList<KeyValuePair<string, string>> GetPropertyList(GcVisibility visibility = GcVisibility.Guru)
    {
        var propertyList = new List<KeyValuePair<string, string>>();
        foreach (GcParameter gcParameter in this)
        {
            if (gcParameter is not GcCommand && gcParameter.Visibility <= visibility)
                propertyList.Add(new KeyValuePair<string, string>(gcParameter.Name, gcParameter.ToString()));
        }
        return propertyList;
    }
}