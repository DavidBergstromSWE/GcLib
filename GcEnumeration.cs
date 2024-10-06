using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GcLib
{
    /// <summary>
    /// Primitive data type representing a parameter of <see cref="Enum"/> type.
    /// </summary>
    public sealed class GcEnumeration : GcParameter
    {
        #region Fields

        // backing-fields
        private string _stringValue;
        private long _intValue;
        private double _numericValue;

        #endregion

        #region Properties

        /// <inheritdoc/>
        [Browsable(true)]
        public override string Name { get; }

        /// <summary>
        /// String value of parameter.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [Browsable(false)]
        public string StringValue
        {
            get => _stringValue;
            set
            {
                if (value != _stringValue)
                {
                    if (!IsImplemented)
                        throw new InvalidOperationException($"{Name} is not implemented!");

                    if (!IsDefined(value))
                        throw new InvalidOperationException($"No entry with name {value} was found in the enumeration!");

                    _stringValue = value;
                    _intValue = GetEntryByName(value).ValueInt;
                    _numericValue = GetEntryByName(value).NumericValue;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Integer value of parameter.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [Browsable(false)]
        public long IntValue
        {
            get => _intValue;
            set
            {
                if (value != _intValue)
                {
                    if (!IsImplemented)
                        throw new InvalidOperationException($"{Name} is not implemented!");

                    if (!IsDefined(value))
                        throw new InvalidOperationException($"No entry with value {value} was found in the enumeration!");

                    _intValue = value;
                    _numericValue = GetEntry(_intValue).NumericValue;
                    _stringValue = GetEntry(_intValue).ValueString;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Numeric value of parameter.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [Browsable(false)]
        public double NumericValue
        {
            get => _numericValue;
            set
            {
                if (value != _numericValue)
                {
                    if (!IsImplemented)
                        throw new InvalidOperationException($"{Name} is not implemented!");

                    if (!IsDefined(value))
                        throw new InvalidOperationException($"No entry with numeric value {value} was found in the enumeration!");

                    _numericValue = value;
                    _intValue = GetEntry(value).ValueInt;
                    _stringValue = GetEntry(value).ValueString;
                    OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc/>
        public GcEnumEntry CurrentEntry => IsImplemented ? GetEntryByName(StringValue) : throw new InvalidOperationException($"{Name} is not implemented!");

        /// <inheritdoc/>
        [Browsable(false)]
        public List<GcEnumEntry> Entries { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new <see cref="enum"/> type parameter, using a <see cref="GcEnumEntry"/> from a list of possible <see cref="GcEnumEntry"/> entries as initial value.
        /// </summary>
        /// <param name="name">Name of parameter.</param>
        /// <param name="category">Category of parameter.</param>
        /// <param name="gcEnumEntry">Value of parameter.</param>
        /// <param name="gcEnumEntries">List of valid enumeration entries.</param>
        /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
        /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
        /// <param name="visibility">Visibility of parameter.</param>
        /// <param name="description">Description of parameter.</param>
        /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
        /// <param name="selectingParameters">Parameters selecting this parameter.</param>
        /// <param name="selectedParameters">Parameters selected by this parameter.</param>
        /// <exception cref="ArgumentException"></exception>
        public GcEnumeration(string name, string category, GcEnumEntry gcEnumEntry, List<GcEnumEntry> gcEnumEntries, bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null)
        {
            if (name.Any(char.IsWhiteSpace))
                throw new ArgumentException("Parameter name cannot contain any whitespace characters!", name);

            if (gcEnumEntries.Any(entry => entry.ValueString == gcEnumEntry.ValueString) == false)
                throw new ArgumentException($"{nameof(gcEnumEntry)} is not an enumeration of {nameof(gcEnumEntries)}!", name);

            if (Enumerable.SequenceEqual(gcEnumEntries, gcEnumEntries.Distinct()) == false)
                throw new ArgumentException($"{nameof(gcEnumEntries)} do not contain unique entries!", name);

            Name = name;
            Category = category;
            IsWritable = isWritable;
            IsReadable = isReadable;
            Visibility = visibility;
            Description = description;
            IsSelector = isSelector;
            SelectedParameters = selectedParameters ?? [];
            SelectingParameters = selectingParameters ?? [];

            Type = GcParameterType.Enumeration;

            Entries = gcEnumEntries;
            StringValue = gcEnumEntry.ValueString;
        }

        /// <summary>
        /// Instantiates a new <see cref="enum"/> type parameter, using an <see cref="Enum"/> from en existing enumeration <see cref="Type"/> as initial value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="category"></param>
        /// <param name="enumEntry"></param>
        /// <param name="enumType"></param>
        /// <param name="isReadable"></param>
        /// <param name="isWritable"></param>
        /// <param name="visibility"></param>
        /// <param name="description"></param>
        /// <param name="isSelector"></param>
        /// <param name="selectingParameters"></param>
        /// <param name="selectedParameters"></param>
        /// <exception cref="ArgumentException"></exception>
        public GcEnumeration(string name, string category, Enum enumEntry, Type enumType, bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null)
        {
            if (name.Any(char.IsWhiteSpace))
                throw new ArgumentException("Parameter name cannot contain any whitespace characters!", name);

            if (enumType.IsEnum == false)
                throw new ArgumentException($"{enumType.Name} does not represent an enumeration type!", name);

            if (enumEntry.GetType() != enumType)
                throw new ArgumentException($"{enumEntry} is not an enumeration of {enumType.Name}!", name);

            if (enumType.GetEnumValues().Cast<int>().Count() != enumType.GetEnumValues().Cast<int>().Distinct().Count())
                throw new ArgumentException($"{enumType.Name} does not contain unique constants!", name);

            Name = name;
            Category = category;
            IsWritable = isWritable;
            IsReadable = isReadable;
            Visibility = visibility;
            Description = description;
            IsSelector = isSelector;
            SelectedParameters = selectedParameters ?? [];
            SelectingParameters = selectingParameters ?? [];

            Type = GcParameterType.Enumeration;

            Entries = CreateGcEnumEntryList(enumType);
            StringValue = enumEntry.ToString();
        }

        /// <summary>
        /// Instantiates a new <see cref="enum"/> type parameter, using an existing <see cref="Enum"/> from an array of valid <see cref="Enum"/> as initial value.
        /// </summary>
        /// <param name="name">Name of parameter.</param>
        /// <param name="category">Category of parameter.</param>
        /// <param name="enumEntry"><see cref="Enum"/> entry.</param>
        /// <param name="enumArray">Array of valid <see cref="Enum"/> entries.</param>
        /// <param name="isReadable">True if parameter is readable, false otherwise.</param>
        /// <param name="isWritable">True if parameter is writable, false otherwise.</param>
        /// <param name="visibility">Visibility of parameter.</param>
        /// <param name="description">Description of parameter.</param>
        /// <param name="isSelector">True if parameter is a selector of other parameters.</param>
        /// <param name="selectingParameters">Parameters selecting this parameter.</param>
        /// <param name="selectedParameters">Parameters selected by this parameter.</param>
        /// <exception cref="ArgumentException"></exception>
        public GcEnumeration(string name, string category, Enum enumEntry, Enum[] enumArray, bool isReadable = true, bool isWritable = true, GcVisibility visibility = GcVisibility.Beginner, string description = "", bool isSelector = false, List<string> selectingParameters = null, List<string> selectedParameters = null)
        {
            if (name.Any(char.IsWhiteSpace))
                throw new ArgumentException("Parameter name cannot contain any whitespace characters!", name);

            if (enumArray.Contains(enumEntry) == false)
                throw new ArgumentException($"{enumEntry} is not an enumeration member of '{nameof(enumArray)}'!", name);

            if (enumArray.Length != enumArray.Distinct().Count())
                throw new ArgumentException($"'{nameof(enumArray)}' does not contain unique entries!", name);

            Name = name;
            Category = category;
            IsWritable = isWritable;
            IsReadable = isReadable;
            Visibility = visibility;
            Description = description;
            IsSelector = isSelector;
            SelectedParameters = selectedParameters ?? [];
            SelectingParameters = selectingParameters ?? [];

            Type = GcParameterType.Enumeration;

            Entries = CreateGcEnumEntryList(enumArray);
            StringValue = enumEntry.ToString();
        }

        /// <summary>
        /// Instantiates a non-implemented <see cref="enum"/> type parameter.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public GcEnumeration(string name)
        {
            if (name.Any(char.IsWhiteSpace))
                throw new ArgumentException("Parameter name cannot contain any whitespace characters!", nameof(name));

            Name = name;
            IsImplemented = false;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Implicit type conversion operator from GcEnumeration to string.
        /// </summary>
        /// <param name="gcEnumeration">GcEnumeration parameter.</param>
        public static implicit operator string(GcEnumeration gcEnumeration)
        {
            return gcEnumeration.StringValue;
        }

        /// <summary>
        /// Implicit type conversion operator from GcEnumeration to int.
        /// </summary>
        /// <param name="gcEnumeration">GcEnumeration parameter.</param>
        public static implicit operator long(GcEnumeration gcEnumeration)
        {
            return gcEnumeration.IntValue;
        }

        #endregion

        #region Public methods

        /// <inheritdoc/>
        public List<string> GetSymbolics()
        {
            var stringList = new List<string>(Entries.Count);
            foreach (GcEnumEntry enumEntry in Entries)
                stringList.Add(enumEntry.ValueString);
            return stringList;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public GcEnumEntry GetEntryByName(string stringValue)
        {
            return IsDefined(stringValue)
                ? Entries.Find(enumEntry => enumEntry.ValueString == stringValue)
                : throw new InvalidOperationException($"No entry with name {stringValue} was found in the enumeration!"); // return null?
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public GcEnumEntry GetEntry(long intValue)
        {
            return IsDefined(intValue)
                ? Entries.Find(enumEntry => enumEntry.ValueInt == intValue)
                : throw new InvalidOperationException($"No entry with value {intValue} was found in the enumeration!"); // return null?
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public GcEnumEntry GetEntry(double numericValue)
        {
            return IsDefined(numericValue)
                ? Entries.Find(enumEntry => enumEntry.NumericValue == numericValue)
                : throw new InvalidOperationException($"No entry with numeric value {numericValue} was found in the enumeration!"); // return null?
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return IsImplemented ? StringValue : $"{Name} is not implemented!";
        }

        /// <inheritdoc/>
        public override void FromString(string valueString)
        {
            StringValue = IsImplemented ? valueString : throw new InvalidOperationException($"{Name} is not implemented!");
        }

        /// <inheritdoc/>
        public override GcEnumeration Copy()
        {
            var entries = new List<GcEnumEntry>();
            foreach (var entry in Entries)
                entries.Add(entry.DeepCopy());

            return new GcEnumeration(Name, Category, entries.Find(e => e.ValueInt == CurrentEntry.ValueInt), entries, IsReadable, IsWritable, Visibility, Description, IsSelector, new List<string>(SelectingParameters), new List<string>(SelectedParameters));
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Checks if integer value is part of a valid enumeration entry.
        /// </summary>
        /// <param name="intValue">Integer value of enumeration entry.</param>
        /// <returns>True if a valid entry is found, false if not.</returns>
        private bool IsDefined(long intValue)
        {
            return Entries.Any(entry => entry.ValueInt == intValue);
        }

        private bool IsDefined(double numericValue)
        {
            return Entries.Any(entry => entry.NumericValue == numericValue);
        }

        /// <summary>
        /// Checks if string value is part of a valid enumeration entry.
        /// </summary>
        /// <param name="stringValue">String value of enumeration entry.</param>
        /// <returns>True if a valid entry is found, false if not.</returns>
        private bool IsDefined(string stringValue)
        {
            return Entries.Any(entry => entry.ValueString == stringValue);
        }

        /// <summary>
        /// Creates a list of enumeration entries corresponding to a specified System.Enum type.
        /// </summary>
        /// <param name="enumType">System.Type (must be of System.Enum type)</param>
        /// <returns>List of GcEnumEntry entries.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static List<GcEnumEntry> CreateGcEnumEntryList(Type enumType)
        {
            Array intValues = Enum.GetValues(enumType);
            string[] stringValues = Enum.GetNames(enumType);

            var entries = new List<GcEnumEntry>(intValues.Length);
            for (int i = 0; i < intValues.Length; i++)
                entries.Add(new GcEnumEntry(stringValues[i], (int)intValues.GetValue(i)));

            return entries;
        }

        /// <summary>
        /// Creates a list of enumeration entries corresponding to specified System.Enum array.
        /// </summary>
        /// <param name="e">Array of System.Enum values.</param>
        /// <returns>List of GcEnumEntry entries.</returns>
        private static List<GcEnumEntry> CreateGcEnumEntryList(Enum[] e)
        {
            var gcEnumsList = new List<GcEnumEntry>(e.Length);
            for (int i = 0; i < e.Length; i++)
                gcEnumsList.Add(new GcEnumEntry(e.GetValue(i).ToString(), (int)e.GetValue(i)));

            return gcEnumsList;
        }

        #endregion
    }
}