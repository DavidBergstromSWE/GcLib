using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GcLib;

/// <summary>
/// Primitive data type representing a parameter of floating-point (<see cref="double"/>) type.
/// </summary>
public sealed class GcFloat : GcParameter
{
    #region Fields

    // backing-fields
    private double _value;

    #endregion

    #region Properties

    /// <inheritdoc/>
    [Browsable(true)]
    public override string Name { get; }

    /// <summary>
    /// Value of parameter.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Browsable(false)]
    public double Value
    {
        get => _value;
        set
        {
            if (!IsImplemented)
                throw new InvalidOperationException($"{Name} is not implemented!");

            if (value != _value)
            {
                // Boundary checking.
                if (value > Max)
                {
                    _value = Max;
                    OnPropertyChanged();
                    return;
                }
                if (value < Min)
                {
                    _value = Min;
                    OnPropertyChanged();
                    return;
                }

                // Increment checking.
                if (Increment != 0)
                {
                    double rem = (double)Convert.ToDecimal((value - Min) % Increment);
                    if (rem != 0) // round to nearest valid increment value
                    {
                        if (rem < Increment / 2)
                            _value = value - rem;
                        else
                            _value = value + (Increment - rem);

                        // Round to decimal specified by increment.
                        _value = Math.Round(_value, 1 + (int)Math.Round(Math.Log10(1 / Increment)));

                        OnPropertyChanged();
                        return;
                    }
                }

                _value = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Minimum value of parameter.
    /// </summary>
    [Browsable(true)]
    public double Min { get; private set; }

    /// <summary>
    /// Maximum value of parameter.
    /// </summary>
    [Browsable(true)]
    public double Max { get; private set; }

    /// <summary>
    /// Increment of parameter.
    /// </summary>
    [Browsable(true)]
    public double Increment { get; }

    /// <summary>
    /// Unit of parameter.
    /// </summary>
    [Browsable(true)]
    public string Unit { get; }

    /// <summary>
    /// Display precision of parameter.
    /// </summary>
    [Browsable(false)]
    public long DisplayPrecision { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new floating-point (<see cref="double"/>) type parameter.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="category">Category of parameter.</param>
    /// <param name="value">Value of parameter.</param>
    /// <param name="min">Minimum value of parameter.</param>
    /// <param name="max">Maximum value of parameter.</param>
    /// <param name="increment">Increment of parameter.</param>
    /// <param name="unit">Unit of parameter.</param>
    /// <param name="displayPrecision">Display precision of parameter.</param>
    /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
    /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
    /// <param name="visibility">Visibility of parameter.</param>
    /// <param name="description">Description of parameter.</param>
    /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
    /// <param name="selectingParameters">Parameters selecting this parameter.</param>
    /// <param name="selectedParameters">Parameters selected by this parameter.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcFloat(string name, string category, double value, double min, double max, double increment = 0, string unit = "", long displayPrecision = 3, bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null)
    {
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException($"Parameter name cannot contain any whitespace characters!", name);

        if (min > max)
            throw new ArgumentException($"Minimum value ({min}) must be smaller or equal to maximum value ({max})!", name);

        if (value < min)
            throw new ArgumentException($"Value ({value}) must be larger or equal to minimum value ({min})!", name);

        if (value > max)
            throw new ArgumentException($"Value ({value}) must be smaller or equal to maximum value ({max})!", name);

        Name = name;
        Category = category;
        Description = description;
        IsReadable = isReadable;
        IsWritable = isWritable;
        Visibility = visibility;
        IsSelector = isSelector;
        SelectedParameters = selectedParameters ?? [];
        SelectingParameters = selectingParameters ?? [];
        Type = GcParameterType.Float;

        Min = min;
        Max = max;

        Unit = unit;
        Increment = increment;
        DisplayPrecision = displayPrecision;

        Value = value;
    }

    /// <summary>
    /// Instantiates a non-implemented floating-point (<see cref="double"/>) type parameter.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public GcFloat(string name)
    {
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Parameter name cannot contain any whitespace characters!", nameof(name));

        Name = name;
        IsImplemented = false;
    }

    #endregion

    #region Operators

    /// <summary>
    /// Implicit type conversion operator from <see cref="GcFloat"/> to <see cref="double"/>.
    /// </summary>
    /// <param name="gcFloat">GcFloat to be converted.</param>
    public static implicit operator double(GcFloat gcFloat)
    {
        return gcFloat.Value;
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    public void ImposeMin(long int64Value)
    {
        Min = int64Value;
        OnPropertyChanged(nameof(Min));

        if (Value < Min)
            Value = Min;
    }

    /// <inheritdoc/>
    public void ImposeMax(long int64Value)
    {
        Max = int64Value;
        OnPropertyChanged(nameof(Max));

        if (Value > Max)
            Value = Max;
    }

    /// <summary>
    /// Returns the same value in integer type.
    /// </summary>
    /// <returns>Current value as integer type.</returns>
    public GcInteger GetIntAlias()
    {
        return new GcInteger(name: Name,
                             category: Category,
                             value: (long)Value,
                             min: (long)Min,
                             max: (long)Max,
                             increment: (long)Increment,
                             incrementMode: EIncMode.fixedIncrement,
                             listOfValidValue: null,
                             unit: Unit,
                             isReadable: IsReadable,
                             isWritable: IsWritable,
                             visibility: Visibility,
                             description: Description); // truncates fractional part!
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsImplemented ? _value.ToString("0." + new string('#', (int)DisplayPrecision)) : $"{Name} is not implemented!"; // old implementation
    }

    /// <summary>
    /// Get the parameter value as a string.
    /// </summary>
    /// <param name="cultureInfo">An object that supplies culture-specific formatting information.</param>
    /// <returns>String representation of parameter value.</returns>
    public string ToString(IFormatProvider formatProvider)
    {
        return IsImplemented ? _value.ToString("0." + new string('#', (int)DisplayPrecision), formatProvider) : $"{Name} is not implemented!";
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="OverflowException"></exception>
    public override void FromString(string valueString)
    {
        if (IsImplemented == false)
            throw new InvalidOperationException($"{Name} is not implemented!");

        Value = double.Parse(valueString); // old implementation
    }

    /// <summary>
    /// Set the parameter value from a string.
    /// </summary>
    /// <param name="valueString">A string representing the new value to set the parameter to.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="OverflowException"></exception>
    public void FromString(string valueString, IFormatProvider formatProvider)
    {
        if (IsImplemented == false)
            throw new InvalidOperationException($"{Name} is not implemented!");

        Value = double.Parse(valueString, formatProvider);
    }

    /// <inheritdoc/>
    public override GcFloat Copy()
    {
        return new GcFloat(Name, Category, Value, Min, Max, Increment, Unit, DisplayPrecision, IsReadable, IsWritable, Visibility, Description, IsSelector, [.. SelectingParameters], [.. SelectedParameters]);
    }

    #endregion
}
