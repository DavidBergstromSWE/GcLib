using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcIntegerTests
{
    private static GcInteger GetInteger(string name = "TestInteger", long value = 42, long min = 0, long max = 100, long increment = 1, EIncMode incrementMode = EIncMode.fixedIncrement, List<long> listOfValidValue = null)
    {
        return new GcInteger(name: name,
                             category: "Test",
                             value: value,
                             min: min,
                             max: max,
                             increment: increment,
                             incrementMode: incrementMode,
                             listOfValidValue: listOfValidValue,
                             unit: "kronor",
                             isReadable: true,
                             isWritable: true,
                             visibility: GcVisibility.Beginner,
                             description: "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcInteger_FixedIncrement_PropertiesAreValid()
    {
        // Act
        var gcInteger = new GcInteger(name: "TestInteger",
                                      category: "Test",
                                      value: 42,
                                      min: 0,
                                      max: 100,
                                      increment: 1,
                                      incrementMode: EIncMode.fixedIncrement,
                                      unit: "kronor",
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcInteger);
        Assert.IsTrue(gcInteger.IsImplemented);
        Assert.AreEqual(gcInteger.Type, GcParameterType.Integer);
        Assert.IsTrue(gcInteger.Name == "TestInteger");
        Assert.IsTrue(gcInteger.Category == "Test");
        Assert.IsTrue(gcInteger.Value == 42);
        Assert.IsTrue(gcInteger.Min == 0);
        Assert.IsTrue(gcInteger.Max == 100);
        Assert.IsTrue(gcInteger.Increment == 1);
        Assert.IsTrue(gcInteger.IncrementMode == EIncMode.fixedIncrement);
        Assert.IsNull(gcInteger.ListOfValidValue);
        Assert.IsTrue(gcInteger.Unit == "kronor");
        Assert.IsTrue(gcInteger.IsReadable);
        Assert.IsTrue(gcInteger.IsWritable);
        Assert.IsTrue(gcInteger.Visibility == GcVisibility.Beginner);
        Assert.IsTrue(gcInteger.Description == "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcInteger_ListIncrement_PropertiesAreValid()
    {
        // Act
        var gcInteger = new GcInteger(name: "TestInteger",
                                      category: "Test",
                                      value: 42,
                                      min: 0,
                                      max: 100,
                                      increment: 1,
                                      incrementMode: EIncMode.listIncrement,
                                      listOfValidValue: [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100],
                                      unit: "kronor",
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcInteger);
        Assert.IsTrue(gcInteger.Name == "TestInteger");
        Assert.IsTrue(gcInteger.Category == "Test");
        Assert.IsTrue(gcInteger.Value == 40);
        Assert.IsTrue(gcInteger.Min == 0);
        Assert.IsTrue(gcInteger.Max == 100);
        Assert.IsTrue(gcInteger.IncrementMode == EIncMode.listIncrement);
        Assert.IsNotNull(gcInteger.ListOfValidValue);
        Assert.IsTrue(gcInteger.Unit == "kronor");
        Assert.IsTrue(gcInteger.IsReadable);
        Assert.IsTrue(gcInteger.IsWritable);
        Assert.IsTrue(gcInteger.Visibility == GcVisibility.Beginner);
        Assert.IsTrue(gcInteger.Description == "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcInteger_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcInteger(name: "Name containing white spaces", category: "Test", value: 42, min: 0, max: 100));
    }

    [TestMethod]
    public void GcInteger_InvalidMinimumMaximum_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcInteger(name: "TestInteger", category: "Test", value: 42, min: 43, max: 41));
    }

    [TestMethod]
    public void GcInteger_InvalidValue_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcInteger(name: "TestInteger", category: "Test", value: 101, min: 0, max: 100));
    }

    [TestMethod]
    [DataRow(20, 50)]
    [DataRow(10, 40)]
    public void GcInteger_InvalidListOfValidValues_ThrowsArgumentException(long min, long max)
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcInteger(name: "TestInteger", category: "Test", value: 30, min: min, max: max, incrementMode: EIncMode.listIncrement, listOfValidValue: [10, 20, 30, 40, 50]));
    }

    [TestMethod]
    public void GcInteger_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcInteger = new GcInteger("NonimplementedParameter");

        // Assert
        Assert.IsTrue(gcInteger.Name == "NonimplementedParameter");
        Assert.IsFalse(gcInteger.IsImplemented);
    }

    [TestMethod]
    public void ImplicitConversion_ExpectedValueIsEqual()
    {
        // Arrange
        var expectedValue = 42;
        var gcInteger = new GcInteger(name: "TestInteger", category: "Test", value: expectedValue, min: 0, max: 100);

        // Act
        long actualValue = gcInteger;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(11)]
    [DataRow(52)]
    [DataRow(78)]
    [DataRow(100)]
    public void Value_InputIsInRange_ExpectedValueIsEqual(long expectedValue)
    {
        // Arrange
        var gcInteger = GetInteger(value: expectedValue, min: 0, max: 100);

        // Act
        gcInteger.Value = expectedValue;

        // Assert
        Assert.AreEqual(gcInteger.Value, expectedValue);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(-1014)]
    public void Value_InputIsLessThanMinimum_ExpectedValueIsMinimum(long value)
    {
        // Arrange
        var gcInteger = GetInteger(min: 0, max: 100);
        var expectedValue = gcInteger.Min;

        // Act
        gcInteger.Value = value;
        var actualValue = gcInteger.Value;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    [DataRow(101)]
    [DataRow(1014)]
    public void Value_InputIsLargerThanMaximum_ExpectedValueIsMaximum(long value)
    {
        // Arrange
        var gcInteger = GetInteger(min: 0, max: 100);
        var expectedValue = gcInteger.Max;

        // Act
        gcInteger.Value = value;
        var actualValue = gcInteger.Value;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    public void Value_InputIsBetweenIncrement_ExpectedValueIsRoundedUp()
    {
        // Arrange
        var gcInteger = GetInteger(min: 0, max: 100, increment: 10);

        // Act
        gcInteger.Value = 95;

        // Assert
        Assert.AreEqual(gcInteger.Value, 100);
    }

    [TestMethod]
    public void Value_InputIsBetweenIncrement_ExpectedValueIsRoundedDown()
    {
        // Arrange
        var gcInteger = GetInteger(min: 0, max: 100, increment: 10);

        // Act
        gcInteger.Value = 4;

        // Assert
        Assert.AreEqual(gcInteger.Value, 0);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(6)]
    [DataRow(9)]
    [DataRow(13)]
    [DataRow(14)]
    [DataRow(100)]
    public void ValueFromValidList_InputIsBetweenIncrement_ExpectedValueIsValid(long inputValue)
    {
        // Arrange
        var validValues = new List<long>() { 4, 8, 14, 42, 75, 99 };
        var gcInteger = GetInteger(min: 0, max: 100, incrementMode: EIncMode.listIncrement, listOfValidValue: validValues);

        // Act
        gcInteger.Value = inputValue;

        // Assert
        Assert.IsTrue(validValues.Contains(gcInteger.Value));
    }

    [TestMethod]
    public void ImposeMin_SmallerThanValue_MinAsExpected()
    {
        // Arrange
        var gcInteger = GetInteger(min: 10, max: 100, value: 15);
        var expectedValue = -10;

        // Act
        gcInteger.ImposeMin(expectedValue);
        var actualValue = gcInteger.Min;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
        Assert.AreEqual(gcInteger.Value, 15);
    }

    [TestMethod]
    public void ImposeMin_LargerThanValue_ValueAsMin()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);

        // Act
        gcInteger.ImposeMin(45);

        // Assert
        Assert.AreEqual(gcInteger.Value, gcInteger.Min);
    }

    [TestMethod]
    public void ImposeMin_LargerThanMax_MaxAsMin()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);
        var expectedValue = 120;

        // Act
        gcInteger.ImposeMin(expectedValue);

        // Assert
        Assert.AreEqual(gcInteger.Max, gcInteger.Min);
        Assert.AreEqual(gcInteger.Value, expectedValue);
    }

    [TestMethod]
    public void ImposeMax_LargerThanValue_MaxAsExpected()
    {
        // Arrange
        var gcInteger = GetInteger(min: 10, max: 100, value: 15);
        var expectedValue = 75;

        // Act
        gcInteger.ImposeMax(expectedValue);
        var actualValue = gcInteger.Max;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
        Assert.AreEqual(gcInteger.Value, 15);
    }

    [TestMethod]
    public void ImposeMax_SmallerThanValue_ValueAsMax()
    {
        // Arrange
        var gcInteger = GetInteger(min: 10, max: 100, value: 40);

        // Act
        gcInteger.ImposeMax(25);

        // Assert
        Assert.AreEqual(gcInteger.Value, gcInteger.Max);
    }

    [TestMethod]
    public void ImposeMax_SmallerThanMin_MinAsMax()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);
        var expectedValue = -10;

        // Act
        gcInteger.ImposeMax(expectedValue);

        // Assert
        Assert.AreEqual(gcInteger.Min, gcInteger.Max);
        Assert.AreEqual(gcInteger.Value, expectedValue);
    }

    [TestMethod]
    public void GetFloatAlias_AreEquall()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);

        // Act
        GcFloat gcFloat = (GcFloat)gcInteger.GetFloatAlias();

        // Assert
        Assert.IsNotNull(gcFloat);
        Assert.AreEqual(gcInteger.Name, gcFloat.Name);
        Assert.AreEqual(gcInteger.DisplayName, gcFloat.DisplayName);
        Assert.AreEqual(gcInteger.Category, gcFloat.Category);
        Assert.AreEqual(gcInteger.Value, gcFloat.Value);
        Assert.AreEqual(gcInteger.Min, gcFloat.Min);
        Assert.AreEqual(gcInteger.Max, gcFloat.Max);
        Assert.AreEqual(gcInteger.Increment, gcFloat.Increment);
        Assert.AreEqual(gcInteger.Unit, gcFloat.Unit);
        Assert.AreEqual(gcInteger.IsImplemented, gcFloat.IsImplemented);
        Assert.AreEqual(gcInteger.IsReadable, gcFloat.IsReadable);
        Assert.AreEqual(gcInteger.IsWritable, gcFloat.IsWritable);
        Assert.AreEqual(gcInteger.IsSelector, gcFloat.IsSelector);
        Assert.IsTrue(Enumerable.SequenceEqual(gcInteger.SelectedParameters, gcFloat.SelectedParameters));
        Assert.IsTrue(Enumerable.SequenceEqual(gcInteger.SelectingParameters, gcFloat.SelectingParameters));
        Assert.AreEqual(gcInteger.Visibility, gcFloat.Visibility);
        Assert.AreEqual(gcInteger.Description, gcFloat.Description);
        Assert.AreNotEqual(gcInteger.Type, gcFloat.Type);
    }

    // Add tests for ToString and FromString.

    [TestMethod]
    public void ToString_IsImplemented_IsNotNull()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);

        // Act
        var stringRepresentation = gcInteger.ToString();

        // Assert
        Assert.IsNotNull(stringRepresentation);
        Assert.AreEqual(gcInteger.Value, Convert.ToInt64(stringRepresentation));
    }

    [TestMethod]
    public void ToString_IsNotImplemented_StringAsExpected()
    {
        // Arrange
        var gcInteger = new GcInteger("NonImplementedParameter");
        var expectedString = $"{gcInteger.Name} is not implemented!";

        // Act
        var actualString = gcInteger.ToString();

        // Assert
        Assert.AreEqual(actualString, expectedString);
    }

    [TestMethod]
    public void FromString_IsImplemented_StringIsInteger_ValueAsExpected()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min:0, max: 100);
        var expectedValue = "33";

        // Act
        gcInteger.FromString(expectedValue);
        var actualValue = gcInteger.ToString();

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    public void FromString_IsImplemented_StringIsNotInteger_ThrowsFormatException()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);

        // Act
        Assert.ThrowsException<FormatException>(() => gcInteger.FromString("HelloWorld"));
        Assert.ThrowsException<FormatException>(() => gcInteger.FromString("11.12"));
    }

    [TestMethod]
    public void FromString_IsImplemented_StringIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var gcInteger = GetInteger(value: 42, min: 0, max: 100);

        // Act
        Assert.ThrowsException<ArgumentNullException>(() => gcInteger.FromString(null));
    }

    [TestMethod]
    public void FromString_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcInteger = new GcInteger("NonImplementedParameter");

        // Act/Assert
        Assert.ThrowsException< InvalidOperationException>(() => gcInteger.FromString("33"));
    }

    [TestMethod]
    public void DeepCopy_AreEqualButNotSame()
    {
        // Arrange
        var gcInteger = GetInteger("TestInteger");

        // Act
        var copyInteger = gcInteger.Copy();

        // Assert
        Assert.AreNotSame(gcInteger, copyInteger);
        Assert.AreEqual(gcInteger.Name, copyInteger.Name);
        Assert.AreEqual(gcInteger.DisplayName, copyInteger.DisplayName);
        Assert.AreEqual(gcInteger.Category, copyInteger.Category);
        Assert.AreEqual(gcInteger.Value, copyInteger.Value);
        Assert.AreEqual(gcInteger.Min, copyInteger.Min);
        Assert.AreEqual(gcInteger.Max, copyInteger.Max);
        Assert.AreEqual(gcInteger.Increment, copyInteger.Increment);
        Assert.AreEqual(gcInteger.IncrementMode, copyInteger.IncrementMode);
        Assert.AreEqual(gcInteger.Type, copyInteger.Type);
        Assert.AreEqual(gcInteger.IsReadable, copyInteger.IsReadable);
        Assert.AreEqual(gcInteger.IsWritable, copyInteger.IsWritable);
        Assert.AreEqual(gcInteger.IsImplemented, copyInteger.IsImplemented);
        Assert.AreEqual(gcInteger.Visibility, copyInteger.Visibility);
        Assert.AreEqual(gcInteger.Description, copyInteger.Description);
        Assert.IsTrue(gcInteger.SelectingParameters.SequenceEqual(copyInteger.SelectingParameters));
        Assert.IsTrue(gcInteger.SelectedParameters.SequenceEqual(copyInteger.SelectedParameters));
    }

    [TestMethod]
    public void DeepCopy_ChangeValue_ValueIsNotEqual()
    {
        // Arrange
        var gcInteger = GetInteger("TestInteger");
        var copyInteger = gcInteger.Copy();

        // Act
        gcInteger.Value = 33;

        // Assert
        Assert.AreNotEqual(gcInteger.Value, copyInteger.Value);
    }
}
