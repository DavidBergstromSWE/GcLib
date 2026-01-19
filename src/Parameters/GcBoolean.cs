using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GcLib;

/// <summary>
/// Primitive data type representing a parameter of <see cref="bool"/> type.
/// </summary>
public sealed class GcBoolean : GcParameter
{
    #region Fields

    // backing-field
    private bool _value;

    #endregion

    #region Properties

    /// <inheritdoc/> ///
    [Browsable(true)]
    public override string Name { get; }

    /// <summary>
    /// Value of parameter.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Browsable(false)]
    public bool Value
    {
        get => _value;
        set
        {
            if (value != _value)
            {
                if (!IsImplemented)
                    throw new InvalidOperationException($"{Name} is not implemented!");

                _value = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new <see cref="bool"/> type parameter.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="category">Category of parameter.</param>
    /// <param name="value">Value of parameter.</param>
    /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
    /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
    /// <param name="visibility">Visibility of parameter.</param>
    /// <param name="description">Description of parameter.</param>
    /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
    /// <param name="selectingParameters">Parameters selecting this parameter.</param>
    /// <param name="selectedParameters">Parameters selected by this parameter.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcBoolean(string name, string category, bool value, bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null) : base(name)
    {
        Name = name;
        Category = category;
        IsWritable = isWritable;
        IsReadable = isReadable;
        Visibility = visibility;
        Description = description;
        IsSelector = isSelector;
        SelectedParameters = selectedParameters ?? [];
        SelectingParameters = selectingParameters ?? [];

        Type = GcParameterType.Boolean;

        Value = value;
    }

    /// <summary>
    /// Instantiates a non-implemented <see cref="bool"/> type parameter.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public GcBoolean(string name) : base(name)
    {
        Name = name;
        IsImplemented = false;
    }

    #endregion

    #region Operators

    /// <summary>
    /// Implicit type conversion operator from <see cref="GcBoolean"/> to <see cref="bool"/>.
    /// </summary>
    public static implicit operator bool(GcBoolean gcBoolean)
    {
        return gcBoolean.Value;
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsImplemented ? Value.ToString() : $"{Name} is not implemented!";
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    public override void FromString(string valueString)
    {
        if (IsImplemented == false)
            throw new InvalidOperationException($"{Name} is not implemented!");

        Value = bool.Parse(valueString);
    }

    /// <inheritdoc/>
    public override GcBoolean Copy()
    {
        return new GcBoolean(Name, Category, Value, IsReadable, IsWritable, Visibility, Description, IsSelector, [.. SelectingParameters], [.. SelectedParameters]);
    }

    #endregion
}