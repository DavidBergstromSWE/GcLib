using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GcLib;

/// <summary>
/// Primitive data type representing a parameter of integer (<see cref="long"/>) type.
/// </summary>
public sealed class GcInteger : GcParameter
{
    #region Fields

    // backing-fields
    private long _value;

    #endregion

    #region Properties

    /// <inheritdoc/> ///
    [Browsable(true)]
    public override string Name { get; }

    /// <summary>
    /// Value of parameter.
    /// </summary>
    /// <exception cref="InvalidOperationException"
    [Browsable(false)]
    public long Value
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
                if (IncrementMode == EIncMode.fixedIncrement && Increment != 0)
                {
                    long rem = (value - Min) % Increment;
                    if (rem != 0) // round to nearest valid increment value
                    {
                        if (rem < Increment / 2)
                            _value = value - rem;
                        else
                            _value = value + (Increment - rem);

                        OnPropertyChanged();
                        return;
                    }
                }
                if (IncrementMode == EIncMode.listIncrement)
                {
                    if (ListOfValidValue.Contains(value) == false)
                    {
                        _value = ListOfValidValue.Aggregate((x, y) => Math.Abs(x - value) < Math.Abs(y - value) ? x : y);
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
    public long Min { get; private set; }

    /// <summary>
    /// Maximum value of parameter.
    /// </summary>
    [Browsable(true)]
    public long Max { get; private set; }

    /// <summary>
    /// Increment value of parameter.
    /// </summary>
    [Browsable(true)]
    public long Increment { get; }

    /// <summary>
    /// Increment mode of parameter.
    /// </summary>
    [Browsable(false)]
    public EIncMode IncrementMode { get; }

    /// <summary>
    /// List of valid values for parameter.
    /// </summary>
    [Browsable(false)]
    public List<long> ListOfValidValue { get; private set; }

    /// <summary>
    /// Unit of parameter.
    /// </summary>
    [Browsable(true)]
    public string Unit { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new integer (<see cref="long"/>) type parameter.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="category">Category of parameter.</param>
    /// <param name="value">Value of parameter.</param>
    /// <param name="min">Minimum value of parameter.</param>
    /// <param name="max">Maximum value of parameter.</param>
    /// <param name="increment">Increment of parameter.</param>
    /// <param name="incrementMode">Increment mode of parameter.</param>
    /// <param name="listOfValidValue">List of valid values for parameter.</param>
    /// <param name="unit">Unit of parameter.</param>
    /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
    /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
    /// <param name="visibility">Visibility of parameter.</param>
    /// <param name="description">Description of parameter.</param>
    /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
    /// <param name="selectingParameters">Parameters selecting this parameter.</param>
    /// <param name="selectedParameters">Parameters selected by this parameter.</param>
    /// <exception cref="ArgumentException"></exception>
    public GcInteger(string name, string category, long value, long min, long max, long increment = 1, EIncMode incrementMode = EIncMode.fixedIncrement, List<long> listOfValidValue = null, string unit = "", bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null) : base(name)
    {
        if (min > max)
            throw new ArgumentException($"Minimum ({min}) must be smaller or equal to maximum value ({max})!", name);

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
        Type = GcParameterType.Integer;
        Unit = unit;

        Min = min;
        Max = max;

        Increment = increment;
        IncrementMode = incrementMode;
        if (incrementMode == EIncMode.listIncrement)
        {
            if (listOfValidValue.Min() < min)
                throw new ArgumentException("List of valid values contains numbers less than minimum!", nameof(listOfValidValue));

            if (listOfValidValue.Max() > max)
                throw new ArgumentException("List of valid values contains numbers larger than maximum!", nameof(listOfValidValue));

            ListOfValidValue = listOfValidValue ?? [];
        }


        Value = value;
    }

    /// <summary>
    ///  Instantiates a non-implemented integer (<see cref="long"/>) type parameter.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public GcInteger(string name) : base(name)
    {
        Name = name;
        IsImplemented = false;
    }

    #endregion

    #region Operators

    /// <summary>
    /// Implicit type conversion operator from <see cref="GcInteger"/> to <see cref="long"/>.
    /// </summary>
    /// <param name="gcInteger">GcInteger type parameter.</param>
    public static implicit operator long(GcInteger gcInteger)
    {
        return gcInteger.Value;
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    public void ImposeMin(long int64Value)
    {
        Min = int64Value;
        OnPropertyChanged(nameof(Min));

        if (Max < Min)
            ImposeMax(int64Value);

        if (Value < Min)
            Value = Min;
    }

    /// <inheritdoc/>
    public void ImposeMax(long int64Value)
    {
        Max = int64Value;
        OnPropertyChanged(nameof(Max));

        if (Min > Max)
            ImposeMin(int64Value);

        if (Value > Max)
            Value = Max;
    }

    /// <inheritdoc/>
    public GcFloat GetFloatAlias()
    {
        return new GcFloat(name: Name,
                           category: Category,
                           value: Value,
                           min: Min,
                           max: Max,
                           increment: Increment,
                           unit: Unit,
                           displayPrecision: 0,
                           isReadable: IsReadable,
                           isWritable: IsWritable,
                           visibility: Visibility,
                           description: Description,
                           isSelector: IsSelector,
                           selectedParameters: SelectedParameters,
                           selectingParameters: SelectingParameters);
    }

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

        Value = long.Parse(valueString);
    }

    /// <inheritdoc/>
    public override GcInteger Copy()
    {
        return new GcInteger(name: Name,
                             category: Category,
                             value: Value,
                             min: Min,
                             max: Max,
                             increment: Increment,
                             incrementMode: IncrementMode,
                             listOfValidValue: ListOfValidValue != null ? [.. ListOfValidValue] : null,
                             unit: Unit,
                             isReadable: IsReadable,
                             isWritable: IsWritable,
                             visibility: Visibility,
                             description: Description,
                             isSelector: IsSelector,
                             selectingParameters: [.. SelectingParameters],
                             selectedParameters: [.. SelectedParameters]);
    }

    #endregion
}
