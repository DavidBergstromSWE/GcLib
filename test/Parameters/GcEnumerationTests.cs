using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcEnumerationTests
{
    private static List<GcEnumEntry> GetValidEnumEntries(int count = 3)
    {
        var entries = new List<GcEnumEntry>();
        for (int i = 0; i < count; i++)
        {
            entries.Add(new GcEnumEntry("Enum" + i.ToString(), i));
        }
        return entries;
    }

    enum ValidEnum
    {
        Zero,
        First,
        Second,
        Third
    }

    enum InvalidEnum
    {
        Zero = 0,
        First = 1,
#pragma warning disable CA1069 // Enums values should not be duplicated
        Second = 1,
#pragma warning restore CA1069 // Enums values should not be duplicated
        Third = 3
    }

    #region Constructors

    [TestMethod]
    public void GcEnumeration_GcEnumEntries_PropertiesAreValid()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var expectedEntry = new GcEnumEntry("Enum1", 1);

        // Act
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration",
                                              category: "Test",
                                              gcEnumEntry: expectedEntry,
                                              gcEnumEntries: entries,
                                              isReadable: true,
                                              isWritable: true,
                                              visibility: GcVisibility.Beginner,
                                              description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcEnumeration);
        Assert.IsTrue(gcEnumeration.IsImplemented);
        Assert.AreEqual(gcEnumeration.Type, GcParameterType.Enumeration);
        Assert.AreEqual(gcEnumeration.Name, "TestEnumeration");
        Assert.AreEqual(gcEnumeration.Category, "Test");
        Assert.IsTrue(Enumerable.SequenceEqual(gcEnumeration.Entries, entries));
        Assert.AreEqual(gcEnumeration.CurrentEntry, expectedEntry);
        Assert.AreEqual(gcEnumeration.StringValue, expectedEntry.ValueString);
        Assert.AreEqual(gcEnumeration.IntValue, expectedEntry.ValueInt);
        Assert.AreEqual(gcEnumeration.NumericValue, expectedEntry.NumericValue);       
        Assert.IsTrue(gcEnumeration.IsReadable);
        Assert.IsTrue(gcEnumeration.IsWritable);
        Assert.AreEqual(gcEnumeration.Visibility, GcVisibility.Beginner);
        Assert.AreEqual(gcEnumeration.Description, "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcEnumeration_GcEnumEntries_EntryNotMember_ThrowsArgumentException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var nonMember = new GcEnumEntry("FakeEnum", 21);

        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", nonMember, entries));
    }

    [TestMethod]
    public void GcEnumeration_GcEnumEntries_EntriesNotUnique_ThrowsArgumentException()
    {
        // Arrange
        var entries = new List<GcEnumEntry>() { new("Enum0", 0), new("Enum1", 1), new("Enum1", 1) };
        var entry = new GcEnumEntry("Enum0", 0);

        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", entry, entries));
    }

    [TestMethod]
    public void GcEnumeration_Type_PropertiesAreValid()
    {
        // Arrange
        var expectedEntry = ValidEnum.First;

        // Act
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration",
                                              category: "Test",
                                              enumEntry: ValidEnum.First,
                                              enumType: typeof(ValidEnum),
                                              isReadable: true,
                                              isWritable: true,
                                              visibility: GcVisibility.Beginner,
                                              description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcEnumeration);
        Assert.IsTrue(gcEnumeration.IsImplemented);
        Assert.AreEqual(gcEnumeration.Type, GcParameterType.Enumeration);
        Assert.AreEqual(gcEnumeration.Name, "TestEnumeration");
        Assert.AreEqual(gcEnumeration.Category, "Test");
        Assert.IsTrue(Enumerable.SequenceEqual(gcEnumeration.Entries.Select(e => e.ValueString), typeof(ValidEnum).GetEnumNames()));
        Assert.IsTrue(gcEnumeration.Entries.Contains(gcEnumeration.CurrentEntry));
        Assert.AreEqual(gcEnumeration.StringValue, expectedEntry.ToString());
        Assert.AreEqual(gcEnumeration.IntValue, (int)expectedEntry);
        Assert.AreEqual(gcEnumeration.NumericValue, gcEnumeration.IntValue);
        Assert.IsTrue(gcEnumeration.IsReadable);
        Assert.IsTrue(gcEnumeration.IsWritable);
        Assert.AreEqual(gcEnumeration.Visibility, GcVisibility.Beginner);
        Assert.AreEqual(gcEnumeration.Description, "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcEnumeration_Type_TypeNotEnum_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", DayOfWeek.Friday, typeof(int)));
    }

    [TestMethod]
    public void GcEnumeration_Type_EntryNotMember_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", DayOfWeek.Friday, typeof(ValidEnum)));
    }

    [TestMethod]
    public void GcEnumeration_Type_EntriesNotUnique_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", InvalidEnum.First, typeof(InvalidEnum)));
    }

    [TestMethod]
    public void GcEnumeration_EnumArray_PropertiesAreValid()
    {
        // Arrange
        var expectedEntry = ValidEnum.First;
        var array = new Enum[] { ValidEnum.First, ValidEnum.Third };

        // Act
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration",
                                              category: "Test",
                                              enumEntry: ValidEnum.First,
                                              enumArray: array,
                                              isReadable: true,
                                              isWritable: true,
                                              visibility: GcVisibility.Beginner,
                                              description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcEnumeration);
        Assert.IsTrue(gcEnumeration.IsImplemented);
        Assert.AreEqual(gcEnumeration.Type, GcParameterType.Enumeration);
        Assert.AreEqual(gcEnumeration.Name, "TestEnumeration");
        Assert.AreEqual(gcEnumeration.Category, "Test");
        Assert.IsTrue(Enumerable.SequenceEqual(gcEnumeration.GetSymbolics(), array.Select(s => s.ToString())));
        Assert.IsTrue(gcEnumeration.Entries.Contains(gcEnumeration.CurrentEntry));
        Assert.AreEqual(gcEnumeration.StringValue, expectedEntry.ToString());
        Assert.AreEqual(gcEnumeration.IntValue, (int)expectedEntry);
        Assert.AreEqual(gcEnumeration.NumericValue, gcEnumeration.IntValue);
        Assert.IsTrue(gcEnumeration.IsReadable);
        Assert.IsTrue(gcEnumeration.IsWritable);
        Assert.AreEqual(gcEnumeration.Visibility, GcVisibility.Beginner);
        Assert.AreEqual(gcEnumeration.Description, "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcEnumeration_EnumArray_EntryNotMember_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", DayOfWeek.Friday, [ValidEnum.First, ValidEnum.Third]));
    }

    [TestMethod]
    public void GcEnumeration_EnumArray_EntriesNotUnique_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration("TestEnumeration", "Test", ValidEnum.Zero, [ValidEnum.Zero, ValidEnum.Zero, ValidEnum.Third]));
    }

    [TestMethod]
    public void GcEnumeration_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcEnumeration(name: "Name containing white spaces", category: "Test", enumEntry: DayOfWeek.Monday, enumType: typeof(DayOfWeek)));
    }

    [TestMethod]
    public void GcEnumeration_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcEnumeration = new GcEnumeration("NonimplementedParameter");

        // Assert
        Assert.IsTrue(gcEnumeration.Name == "NonimplementedParameter");
        Assert.IsFalse(gcEnumeration.IsImplemented);
    }

    #endregion

    #region Properties

    [TestMethod]
    public void StringValue_InputIsValid_ExpectedValueIsEqual()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);
        var expectedStringValue = entries[1].ValueString;

        // Act
        gcEnumeration.StringValue = expectedStringValue;
        var actualStringValue = gcEnumeration.StringValue;

        // Assert
        Assert.AreEqual(actualStringValue, expectedStringValue);
    }

    [TestMethod]
    public void StringValue_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.StringValue = "FakeEnum");
    }

    [TestMethod]
    public void StringValue_InputIsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.StringValue = "FakeEnum");
    }

    [TestMethod]
    public void IntValue_InputIsValid_ExpectedValueIsEqual()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);
        var expectedIntValue = entries[1].ValueInt;

        // Act
        gcEnumeration.IntValue = expectedIntValue;
        var actualIntValue = gcEnumeration.IntValue;

        // Assert
        Assert.AreEqual(actualIntValue, expectedIntValue);
    }

    [TestMethod]
    public void IntValue_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.IntValue = 1);
    }

    [TestMethod]
    public void IntValue_InputIsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.IntValue = 42);
    }

    [TestMethod]
    public void NumericValue_InputIsValid_ExpectedValueIsEqual()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);
        var expectedNumericValue = entries[1].NumericValue;

        // Act
        gcEnumeration.NumericValue = expectedNumericValue;
        var actualNumericValue = gcEnumeration.NumericValue;

        // Assert
        Assert.AreEqual(actualNumericValue, expectedNumericValue);
    }

    [TestMethod]
    public void NumericValue_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.NumericValue = 1);
    }

    [TestMethod]
    public void NumericValue_InputIsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.NumericValue = 3.14);
    }

    [TestMethod]
    public void CurrentEntry_IsImplemented_ReturnsExpectedEntry()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var expectedEntry = entries[1];
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: expectedEntry, gcEnumEntries: entries);

        // Act
        var actualEntry = gcEnumeration.CurrentEntry;

        // Assert
        Assert.AreEqual(actualEntry, expectedEntry);
    }

    [TestMethod]
    public void CurrentEntry_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.CurrentEntry);
    }

    [TestMethod]
    public void Entries_IsImplemented_ReturnsEntries()
    {
        // Arrange
        var expectedEntries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: expectedEntries[0], gcEnumEntries: expectedEntries);

        // Act
        var actualEntries = gcEnumeration.Entries;

        // Assert
        Assert.AreEqual(actualEntries, expectedEntries);
        Assert.IsTrue(Enumerable.SequenceEqual(actualEntries, expectedEntries));
    }

    [TestMethod]
    public void Entries_IsNotImplemented_ReturnsNull()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");

        // Act
        var actualEntries = gcEnumeration.Entries;

        // Assert
        Assert.IsNull(actualEntries);
    }

    #endregion

    #region Operators

    [TestMethod]
    public void ImplicitConversion_String_StringAsExpected()
    {
        // Arrange
        var expectedString = "Enum1";
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration",
                                              category: "Test",
                                              gcEnumEntry: new GcEnumEntry(expectedString, 1),
                                              gcEnumEntries: GetValidEnumEntries());

        // Act
        string actualString = gcEnumeration;

        // Assert
        Assert.AreEqual(actualString, expectedString);
    }

    [TestMethod]
    public void ImplicitConversion_Long_LongAsExpected()
    {
        // Arrange
        var expectedLong = 1;
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration",
                                              category: "Test",
                                              gcEnumEntry: new GcEnumEntry("Enum1", expectedLong),
                                              gcEnumEntries: GetValidEnumEntries());

        // Act
        long actualLong = gcEnumeration;

        // Assert
        Assert.AreEqual(actualLong, expectedLong);
    }

    #endregion

    #region Methods

    [TestMethod]
    public void GetSymbolics_ReturnsValueStrings()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act
        var enumerationNames = gcEnumeration.GetSymbolics();

        // Assert
        Assert.IsNotNull(enumerationNames);
        Assert.IsTrue(Enumerable.SequenceEqual(enumerationNames, entries.Select(e => e.ValueString)));
    }

    [TestMethod]
    public void GetEntryByName_EntryExists_ReturnsEntry()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act
        var entry = gcEnumeration.GetEntryByName(entries[1].ValueString);

        // Assert
        Assert.IsNotNull(entry);
        Assert.IsTrue(entries.Contains(entry));
        Assert.AreEqual(entry, entries[1]);
    }

    [TestMethod]
    public void GetEntryByName_EntryDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.GetEntryByName("FakeEnum"));
    }

    [TestMethod]
    public void GetEntry_LongInput_EntryExists_ReturnsEntry()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act
        var entry = gcEnumeration.GetEntry(1);

        // Assert
        Assert.IsNotNull(entry);
        Assert.IsTrue(entries.Contains(entry));
        Assert.AreEqual(entry, entries[1]);
    }

    [TestMethod]
    public void GetEntry_LongInput_EntryDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.GetEntry(5));
    }

    [TestMethod]
    public void GetEntry_NumericInput_EntryExists_ReturnsEntry()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act
        var entry = gcEnumeration.GetEntry(1.0);

        // Assert
        Assert.IsNotNull(entry);
        Assert.IsTrue(entries.Contains(entry));
        Assert.AreEqual(entry, entries[1]);
    }

    [TestMethod]
    public void GetEntry_NumericInput_EntryDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.GetEntry(3.14));
    }

    [TestMethod]
    public void ToString_IsImplemented_ReturnsStringValue()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);
        var expectedString = entries[0].ValueString;

        // Act
        var actualString = gcEnumeration.ToString();

        // Assert
        Assert.AreEqual(expectedString, actualString);
    }

    [TestMethod]
    public void ToString_IsNotImplemented_ReturnsNotImplementedString()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");
        var expectedString = $"{gcEnumeration.Name} is not implemented!";

        // Act
        var actualString = gcEnumeration.ToString();

        // Assert
        Assert.AreEqual(expectedString, actualString);
    }

    [TestMethod]
    public void FromString_IsImplemented_StringValueAsExpected()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);
        var expectedString = entries[1].ValueString;

        // Act
        gcEnumeration.FromString(expectedString);
        var actualString = gcEnumeration.StringValue;

        // Assert
        Assert.AreEqual(expectedString, actualString);
    }

    [TestMethod]
    public void FromString_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration");

        // Act/Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcEnumeration.FromString("Enum0"));
    }

    [TestMethod]
    public void DeepCopy_AreEqualButNotSame()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);

        // Act
        var copyEnumeration = gcEnumeration.Copy();

        // Assert
        Assert.AreNotSame(gcEnumeration, copyEnumeration);
        Assert.AreEqual(gcEnumeration.Name, copyEnumeration.Name);
        Assert.AreEqual(gcEnumeration.DisplayName, copyEnumeration.DisplayName);
        Assert.AreEqual(gcEnumeration.Category, copyEnumeration.Category);
        Assert.AreEqual(gcEnumeration.StringValue, copyEnumeration.StringValue);
        Assert.AreEqual(gcEnumeration.IntValue, copyEnumeration.IntValue);
        Assert.AreEqual(gcEnumeration.NumericValue, copyEnumeration.NumericValue);
        Assert.AreEqual(gcEnumeration.CurrentEntry, copyEnumeration.CurrentEntry);
        Assert.IsTrue(gcEnumeration.Entries.SequenceEqual(copyEnumeration.Entries));
        Assert.AreEqual(gcEnumeration.Type, copyEnumeration.Type);
        Assert.AreEqual(gcEnumeration.IsReadable, copyEnumeration.IsReadable);
        Assert.AreEqual(gcEnumeration.IsWritable, copyEnumeration.IsWritable);
        Assert.AreEqual(gcEnumeration.IsImplemented, copyEnumeration.IsImplemented);
        Assert.AreEqual(gcEnumeration.Visibility, copyEnumeration.Visibility);
        Assert.AreEqual(gcEnumeration.Description, copyEnumeration.Description);
        Assert.IsTrue(gcEnumeration.SelectingParameters.SequenceEqual(copyEnumeration.SelectingParameters));
        Assert.IsTrue(gcEnumeration.SelectedParameters.SequenceEqual(copyEnumeration.SelectedParameters));
    }

    [TestMethod]
    public void DeepCopy_ChangeValue_ValueIsNotEqual()
    {
        // Arrange
        var entries = GetValidEnumEntries();
        var gcEnumeration = new GcEnumeration(name: "TestEnumeration", category: "Test", gcEnumEntry: entries[0], gcEnumEntries: entries);
        var copyEnumeration = gcEnumeration.Copy();

        // Act
        gcEnumeration.IntValue = 2;

        // Assert
        Assert.AreNotEqual(gcEnumeration.IntValue, copyEnumeration.IntValue);
        Assert.AreNotEqual(gcEnumeration.StringValue, copyEnumeration.StringValue);
        Assert.AreNotEqual(gcEnumeration.NumericValue, copyEnumeration.NumericValue);
    }

    #endregion
}