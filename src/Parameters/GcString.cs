using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GcLib;

/// <summary>
/// Primitive data type representing a parameter of <see cref="string"/> type.
/// </summary>
public sealed class GcString : GcParameter
{
    #region Fields

    // backing-fields
    private string _value;

    #endregion

    #region Properties

    /// <inheritdoc/> ///
    [Browsable(true)]
    public override string Name { get; }

    /// <summary>
    /// Value of parameter.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Browsable(false)]
    public string Value
    {
        get => _value;
        set
        {
            if (value != _value)
            {
                if (IsImplemented == false)
                    throw new InvalidOperationException($"{Name} is not implemented!");

                if (value.Length > MaxLength)
                    throw new ArgumentOutOfRangeException(Name, value.Length, $"String is too long! Length must be <= {MaxLength}.");

                _value = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The maximum string length permitted.
    /// </summary>
    [Browsable(true)]
    public long MaxLength { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new <see cref="string"/> type parameter.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="category">Category of parameter.</param>
    /// <param name="value">Value of parameter.</param>
    /// <param name="maxLength">The maximum string length permitted.</param>
    /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
    /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
    /// <param name="visibility">Visibility of parameter.</param>
    /// <param name="description">Description of parameter.</param>
    /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
    /// <param name="selectingParameters">Parameters selecting this parameter.</param>
    /// <param name="selectedParameters">Parameters selected by this parameter.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcString(string name, string category, string value, long maxLength, bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null) : base(name)
    {
        if (maxLength < 0)
            throw new ArgumentException($"Maximum string length ({maxLength}) must be larger than zero!", name);

        Name = name;
        Category = category;
        Description = description;
        IsReadable = isReadable;
        IsWritable = isWritable;
        Visibility = visibility;
        IsSelector = isSelector;
        SelectedParameters = selectedParameters ?? [];
        SelectingParameters = selectingParameters ?? [];
        Type = GcParameterType.String;

        MaxLength = maxLength;
        Value = value;
    }

    /// <summary>
    /// Instantiates a non-implemented <see cref="string"/> type parameter.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public GcString(string name) : base(name)
    {
        Name = name;
        IsImplemented = false;
    }

    #endregion

    #region Operators

    /// <summary>
    /// Implicit type conversion operator from <see cref="GcString"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="gcString">GcString input.</param>
    public static implicit operator string(GcString gcString)
    {
        return gcString.Value;
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
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public override void FromString(string valueString)
    {
        if (IsImplemented == false)
            throw new InvalidOperationException($"{Name} is not implemented!");

        Value = valueString;
    }

    /// <inheritdoc/>
    public override GcString Copy()
    {
        return new GcString(Name, Category, Value, MaxLength, IsReadable, IsWritable, Visibility, Description, IsSelector, [.. SelectingParameters], [.. SelectedParameters]);
    }

    #endregion
}