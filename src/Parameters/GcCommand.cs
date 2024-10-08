using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GcLib;

/// <summary>
/// Primitive data type representing a parameter of command (<see cref="Action"/>) type.
/// </summary>
public sealed class GcCommand : GcParameter
{
    #region Fields

    /// <summary>
    /// True if execution of command is done.
    /// </summary>
    private bool _isDone;

    /// <summary>
    /// Encapsulates the method used when command is executed.
    /// </summary>
    private readonly Action _executeMethod;

    #endregion

    #region Properties

    /// <summary>
    /// Name of parameter.
    /// </summary>
    [Browsable(true)]
    public override string Name { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new command (<see cref="Action"/>) type parameter.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="category">Category of parameter.</param>
    /// <param name="method">Method to execute.</param>
    /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
    /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
    /// <param name="visibility">Visibility of parameter.</param>
    /// <param name="description">Description of parameter.</param>
    /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
    /// <param name="selectingParameters">Parameters selecting this parameter.</param>
    /// <param name="selectedParameters">Parameters selected by this parameter.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcCommand(string name, string category, Action method, bool isReadable = false, bool isWritable = false, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null)
    {
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Parameter name cannot contain any whitespace characters!", name);

        Name = name;
        Category = category;
        IsWritable = isWritable;
        IsReadable = isReadable;
        Visibility = visibility;
        Description = description;
        IsSelector = isSelector;
        SelectedParameters = selectedParameters ?? [];
        SelectingParameters = selectingParameters ?? [];

        Type = GcParameterType.Command;

        _executeMethod = method;
        _isDone = false;
    }

    /// <summary>
    /// Instantiates a non-implemented command (<see cref="Action"/>) type parameter.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public GcCommand(string name)
    {
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Parameter name cannot contain any whitespace characters!", name);

        Name = name;
        IsImplemented = false;
    }

    #endregion

    #region Methods

    // ToDo: Add ExecuteAsync to GcCommand?

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"></exception>
    public void Execute()
    {
        if (IsImplemented == false)
            throw new InvalidOperationException($"{Name} is not implemented!");

        _isDone = false;
        _executeMethod();
        _isDone = true;
    }

    /// <inheritdoc/>
    public bool IsDone()
    {
        return _isDone;
    }

    /// <summary>
    /// Returns the name of the parameter command.
    /// </summary>
    /// <returns>Name of command.</returns>
    public override string ToString()
    {
        return IsImplemented ? Name : $"{Name} is not implemented!";
    }

    /// <summary>
    /// Not applicable for this class.
    /// </summary>
    /// <param name="valueString">Not applicable for this class.</param>
    public override void FromString(string valueString = null)
    {
        return;
    }

    /// <inheritdoc/>
    public override GcCommand Copy()
    {
        return new GcCommand(Name, Category, _executeMethod, IsReadable, IsWritable, Visibility, Description, IsSelector, new List<string>(SelectingParameters), new List<string>(SelectedParameters));
    }

    

    #endregion
}