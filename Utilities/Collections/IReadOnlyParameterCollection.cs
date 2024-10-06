using System.Collections.Generic;

namespace GcLib.Utilities.Collections;

/// <summary>
/// Interface for a read-only collection of parameters of type <see cref="GcParameter"/>.
/// </summary>
public interface IReadOnlyParameterCollection : IReadOnlyCollection<GcParameter>
{
    /// <summary>
    /// Indexer to allow parameter retrieval using [] notation.
    /// </summary>
    /// <param name="i">Index of parameter in collection.</param>
    /// <returns>Parameter.</returns>
    /// 
    GcParameter this[int i] { get; }

    /// <summary>
    /// Indexer to allow parameter retrieval using [] notation.
    /// </summary>
    /// <param name="parameterName">Name of parameter.</param>
    /// <returns>Parameter.</returns>
    GcParameter this[string parameterName] { get; }

    /// <summary>
    /// Name of collection.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Retrieves a list of all parameters in the collection.
    /// </summary>
    /// <returns>List of parameters.</returns>
    IReadOnlyList<GcParameter> ToList();

    /// <summary>
    /// Retrieve a list of parameters in the collection visible up to the specified user visibility.
    /// </summary>
    /// <param name="visibility">Visibility level.</param>
    /// <returns>List of parameters.</returns>
    IReadOnlyList<GcParameter> ToList(GcVisibility visibility = GcVisibility.Guru);

    /// <summary>
    /// Returns a simplified list of parameter name/value string pairs from collection, up to the specified user visibility.
    /// </summary>
    /// <param name="visibility">Visibility level.</param>
    /// <returns>List of <i>key</i>/<i>value</i> pairs, where <i>key</i> is parameter name and <i>value</i> is parameter value.</returns>
    IReadOnlyList<KeyValuePair<string, string>> GetPropertyList(GcVisibility visibility = GcVisibility.Guru);

    /// <summary>
    /// Retrieve parameter categories in collection visible up to the specified user visibility.
    /// </summary>
    /// <param name="visibility">Visibility level.</param>
    /// <returns>Parameter categories.</returns>
    IEnumerable<string> GetCategories(GcVisibility visibility);

    /// <summary>
    /// Determines whether parameter collection contains a specific parameter.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns><see langword="true"/> if collection contains parameter.</returns>
    bool IsImplemented(string parameterName);

    /// <summary>
    /// Get value of parameter.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter value as string or <see langword="null"/> if parameter is not found, readable or available.</returns>
    string GetParameterValue(string parameterName);

    /// <summary>
    /// Set parameter to new value.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="parameterValue">New parameter value as string.</param>
    void SetParameterValue(string parameterName, string parameterValue);

    /// <summary>
    /// Execute parameter command.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    void ExecuteParameterCommand(string parameterName);

    /// <summary>
    /// Retrieves parameter casted in the corresponding primitive data type.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter or <see langword="null"/> if parameter is not found.</returns>
    GcParameter GetParameter(string parameterName);

    /// <summary>
    /// Updates the parameter collection with current parameter values.
    /// </summary>
    void Update();
}