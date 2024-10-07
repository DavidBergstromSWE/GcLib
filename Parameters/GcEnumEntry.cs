using System;

namespace GcLib;

/// <summary>
/// Represents an enumeration member in a parameter of <see cref="GcEnumeration"/> type.
/// </summary>
public sealed class GcEnumEntry
{
    #region Properties

    /// <summary>
    /// Integer value of enumeration member.
    /// </summary>
    public long ValueInt { get; }

    /// <summary>
    /// String value of enumeration member.
    /// </summary>
    public string ValueString { get; }

    /// <summary>
    /// Numeric value of enumeration member.
    /// </summary>
    public double NumericValue { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new enumeration member, using symbolic name, integer value and (optional) numerical value.
    /// </summary>
    /// <param name="symbolicName">String value of member.</param>
    /// <param name="value">Integer value of member.</param>
    /// <param name="numericValue">(Optional) Numeric value of member.</param>
    public GcEnumEntry(string symbolicName, long value, double? numericValue = null)
    {
        ValueString = symbolicName;
        ValueInt = value;
        if (numericValue != null)
            NumericValue = (double)numericValue;
        else NumericValue = value;
    }

    #endregion

    #region Operators

    public static bool operator ==(GcEnumEntry c1, GcEnumEntry c2)
    {
        return c1.Equals(c2);
    }

    public static bool operator !=(GcEnumEntry c1, GcEnumEntry c2)
    {
        return !c1.Equals(c2);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Returns string value of enumeration member.
    /// </summary>
    /// <returns>String value.</returns>
    public override string ToString()
    {
        return ValueString;
    }

    public override bool Equals(object obj)
    {
        return obj is GcEnumEntry enumEntry && Equals(enumEntry);
    }

    public bool Equals(GcEnumEntry enumEntry)
    {
        return ValueString == enumEntry.ValueString && ValueInt == enumEntry.ValueInt;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ValueInt, ValueString, NumericValue);
    }

    /// <summary>
    /// Creates a deep copy of the enumeration member.
    /// </summary>
    /// <returns></returns>
    public GcEnumEntry DeepCopy()
    {
        return new GcEnumEntry(ValueString, ValueInt, NumericValue);
    }

    #endregion
}