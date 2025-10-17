using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcFloatTests
{
    private static GcFloat GetFloat(string name = "TestFloat", double value = 3.14, double min = 0.00, double max = 10.00, double increment = 0.0)
    {
        return new GcFloat(name: name,
                           category: "Test",
                           value: value,
                           min: min,
                           max: max,
                           increment: increment,
                           unit: "rad",
                           isReadable: true,
                           isWritable: true,
                           visibility: GcVisibility.Beginner,
                           description: "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcFloat_PropertiesAreValid()
    {
        // Act
        var gcFloat = new GcFloat(name: "TestFloat",
                                  category: "Test",
                                  value: 3.14,
                                  min: 0.0,
                                  max: 10.0,
                                  increment: 0.01,
                                  unit: "rad",
                                  displayPrecision: 2,
                                  isReadable: true,
                                  isWritable: true,
                                  visibility: GcVisibility.Beginner,
                                  description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcFloat);
        Assert.IsTrue(gcFloat.IsImplemented);
        Assert.AreEqual(gcFloat.Type, GcParameterType.Float);
        Assert.AreEqual(gcFloat.Name, "TestFloat");
        Assert.AreEqual(gcFloat.DisplayName, gcFloat.Name);
        Assert.AreEqual(gcFloat.Category, "Test");
        Assert.AreEqual(gcFloat.Value, 3.14);
        Assert.AreEqual(gcFloat.Min, 0.0);
        Assert.AreEqual(gcFloat.Max, 10.0);
        Assert.AreEqual(gcFloat.Increment, 0.01);
        Assert.AreEqual(gcFloat.Unit, "rad");
        Assert.AreEqual(gcFloat.DisplayPrecision, 2);
        Assert.IsTrue(gcFloat.IsReadable);
        Assert.IsTrue(gcFloat.IsWritable);
        Assert.AreEqual(gcFloat.Visibility, GcVisibility.Beginner);
        Assert.AreEqual(gcFloat.Description, "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcFloat_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcFloat(name: "Name containing white spaces", category: "Test", value: 3.14, min: 0.0, max: 10.0));
    }

    [TestMethod]
    public void GcFloat_InvalidMinimumMaximum_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcFloat(name: "TestFloat", category: "Test", value: 3.14, min: 4.0, max: 3.0));
    }

    [TestMethod]
    public void GcFloat_InvalidValue_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcFloat(name: "TestFloat", category: "Test", value: 4.01, min: 3.9, max: 4.0));
    }

    [TestMethod]
    public void GcFloat_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcFloat = new GcFloat("NonimplementedParameter");

        // Assert
        Assert.AreEqual("NonimplementedParameter", gcFloat.Name);
        Assert.IsFalse(gcFloat.IsImplemented);
    }

    [TestMethod]
    [DataRow(3.14, 0)]
    [DataRow(3.14, 0.01)]
    public void ImplicitConversion_ExpectedValueIsEqual(double expectedValue, double increment)
    {
        // Arrange
        var gcFloat= new GcFloat(name: "TestFloat", category: "Test", value: expectedValue, min: 0.0, max: 10.0, increment: increment);

        // Act
        double actualValue = gcFloat;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    [DataRow(-3.2)]
    [DataRow(1.4)]
    [DataRow(3.1)]
    [DataRow(78)]
    [DataRow(100)]
    public void Value_InputIsInRange_ExpectedValueIsEqual(double expectedValue)
    {
        // Arrange
        var gcFloat = GetFloat(min: -5.1, max: 145.2, increment: 0.1);

        // Act
        gcFloat.Value = expectedValue;

        // Assert
        Assert.AreEqual(gcFloat.Value, expectedValue);
    }

    [TestMethod]
    [DataRow(3.1, 3.1245, 0.1)]
    [DataRow(32.75, 32.74785, 0.01)]
    [DataRow(32.8, 32.7, 0.2)]
    [DataRow(32.5, 32.6, 0.25)]
    [DataRow(32.75, 32.65, 0.25)]
    [DataRow(3, 3.49, 1)]
    [DataRow(42, 41.5, 1)]
    public void Value_RoundToNearestIncrement(double expectedValue, double inputValue, double increment)
    {
        // Arrange
        var gcFloat = GetFloat(min: 0.0, max: 100.0, increment: increment);
        
        // Act
        gcFloat.Value = inputValue;

        // Assert
        Assert.AreEqual(gcFloat.Value, expectedValue);
    }

    [TestMethod]
    public void IsVisible_VisibleUpToSpecifiedLevel()
    {
        // Arrange
        var gcFloat = new GcFloat(name: "TestFloat",
                                  category: "Test",
                                  value: 3.14,
                                  min: 0.0,
                                  max: 10.0,
                                  visibility: GcVisibility.Expert);

        // Assert
        Assert.IsFalse(gcFloat.IsVisible(GcVisibility.Beginner));
        Assert.IsTrue(gcFloat.IsVisible(GcVisibility.Expert));
        Assert.IsTrue(gcFloat.IsVisible(GcVisibility.Guru));
        Assert.IsTrue(gcFloat.IsVisible(GcVisibility.Invisible));
    }

    [TestMethod]
    public void GetIntAlias_AreEquall()
    {
        // Arrange
        var gcFloat = GetFloat(value: Math.PI, min: 0, max: 100);

        // Act
        GcInteger gcInteger = (GcInteger)gcFloat.GetIntAlias();

        // Assert
        Assert.IsNotNull(gcInteger);
        Assert.AreEqual(gcFloat.Name, gcInteger.Name);
        Assert.AreEqual(gcFloat.DisplayName, gcInteger.DisplayName);
        Assert.AreEqual(gcFloat.Category, gcInteger.Category);
        Assert.AreEqual((long)gcFloat.Value, gcInteger.Value);
        Assert.AreEqual(gcFloat.Min, gcInteger.Min);
        Assert.AreEqual(gcFloat.Max, gcInteger.Max);
        Assert.AreEqual(gcFloat.Increment, gcInteger.Increment);
        Assert.AreEqual(gcFloat.Unit, gcInteger.Unit);
        Assert.AreEqual(gcFloat.IsImplemented, gcInteger.IsImplemented);
        Assert.AreEqual(gcFloat.IsReadable, gcInteger.IsReadable);
        Assert.AreEqual(gcFloat.IsWritable, gcInteger.IsWritable);
        Assert.AreEqual(gcFloat.IsSelector, gcInteger.IsSelector);
        Assert.IsTrue(Enumerable.SequenceEqual(gcFloat.SelectedParameters, gcInteger.SelectedParameters));
        Assert.IsTrue(Enumerable.SequenceEqual(gcFloat.SelectingParameters, gcInteger.SelectingParameters));
        Assert.AreEqual(gcFloat.Visibility, gcInteger.Visibility);
        Assert.AreEqual(gcFloat.Description, gcInteger.Description);
        Assert.AreNotEqual(gcFloat.Type, gcInteger.Type);
    }

    [TestMethod]
    public void ToString_IsImplemented_IsNotNull()
    {
        // Arrange
        var gcFloat = GetFloat();

        // Act
        var stringRepresentation = gcFloat.ToString();

        // Assert
        Assert.IsNotNull(stringRepresentation);
    }

    [TestMethod]
    public void ToString_IsNotImplemented_StringAsExpected()
    {
        // Arrange
        var gcFloat = new GcFloat("NonImplementedParameter");
        var expectedString = $"{gcFloat.Name} is not implemented!";

        // Act
        var actualString = gcFloat.ToString();

        // Assert
        Assert.AreEqual(actualString, expectedString);
    }

    [TestMethod]
    [DataRow(Math.PI, 0)]
    [DataRow(Math.PI, 1)]
    [DataRow(Math.PI, 2)]
    [DataRow(Math.PI, 3)]
    [DataRow(112432.4752, 0)]
    [DataRow(112432.4752, 1)]
    [DataRow(112432.4752, 2)]
    public void ToString_CorrectFormat(double value, long displayPrecision)
    {
        // Arrange
        var gcFloat = new GcFloat(name: "TestFloat",
                                  category: "Test",
                                  value: value,
                                  min: 0.0,
                                  max: 1000000.0,
                                  displayPrecision: displayPrecision,
                                  visibility: GcVisibility.Expert);        
        var expectedString = Convert.ToString(Math.Round(gcFloat.Value, (int)displayPrecision));

        // Act
        var actualString = gcFloat.ToString();

        // Assert
        Assert.AreEqual(actualString, expectedString);
    }

    [TestMethod]
    public void ToString_CultureInfo_CurrentCulture()
    {
        // Arrange
        var gcFloat = new GcFloat(name: "TestFloat",
                                  category: "Test",
                                  value: Math.PI,
                                  min: 0.0,
                                  max: 1000000.0,
                                  displayPrecision: 3,
                                  visibility: GcVisibility.Expert);

        // Assert
        Assert.AreEqual(Convert.ToString(Math.Round(gcFloat.Value, 3)), gcFloat.ToString(CultureInfo.CurrentCulture));
        Assert.AreEqual(Convert.ToString(Math.Round(gcFloat.Value, 3), CultureInfo.InvariantCulture), gcFloat.ToString(CultureInfo.InvariantCulture));
        Assert.AreEqual(Convert.ToString(Math.Round(gcFloat.Value, 3), CultureInfo.InstalledUICulture), gcFloat.ToString(CultureInfo.InstalledUICulture));
        Assert.AreEqual(Convert.ToString(Math.Round(gcFloat.Value, 3), CultureInfo.CurrentUICulture), gcFloat.ToString(CultureInfo.CurrentUICulture));
        Assert.AreEqual(Convert.ToString(Math.Round(gcFloat.Value, 3), CultureInfo.DefaultThreadCurrentCulture), gcFloat.ToString(CultureInfo.DefaultThreadCurrentCulture));
    }

    [TestMethod]
    [DataRow(",")]
    [DataRow(".")]
    [DataRow("_")]
    [DataRow(" ")]
    public void ToString_CultureInfo_CustomNumberDecimalSeparator(string decimalSeparator)
    {
        // Arrange
        CultureInfo cultureInfo = new("sv-SE");
        cultureInfo.NumberFormat.NumberDecimalSeparator = decimalSeparator;

        var gcFloat = new GcFloat(name: "TestFloat",
                                  category: "Test",
                                  value: Math.PI,
                                  min: 0.0,
                                  max: 1000000.0,
                                  displayPrecision: 3,
                                  visibility: GcVisibility.Expert);
        var expectedString = Convert.ToString(Math.Round(gcFloat.Value, 3), cultureInfo);

        // Act
        var actualString = gcFloat.ToString(cultureInfo);

        // Assert
        Assert.AreEqual(actualString, expectedString);
    }

    [TestMethod]
    [DataRow("33,74")]
    [DataRow("33")]
    [DataRow("-29,001")]
    public void FromString_IsImplemented_StringIsNumber_ValueAsExpected(string expectedValue)
    {
        // Arrange
        var gcFloat = GetFloat(value: Math.PI, min: -100, max: 100);

        // Act
        gcFloat.FromString(expectedValue);
        var actualValue = gcFloat.ToString();

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    [DataRow("33.74")]
    [DataRow("33")]
    [DataRow("-29.001")]
    public void FromString_IsImplemented_StringIsNumber_InvariantCulture_ValueAsExpected(string expectedValue)
    {
        // Arrange
        var gcFloat = GetFloat(value: Math.PI, min: -100, max: 100);

        // Act
        gcFloat.FromString(expectedValue, CultureInfo.InvariantCulture);
        var actualValue = gcFloat.ToString(CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    public void FromString_IsImplemented_StringIsNotNumber_ThrowsFormatException()
    {
        // Arrange
        var gcFloat = GetFloat(value: Math.PI, min: 0, max: 100);

        // Act
        Assert.Throws<FormatException>(() => gcFloat.FromString("HelloWorld"));
    }

    [TestMethod]
    public void FromString_IsImplemented_StringIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var gcFloat = GetFloat(value: Math.PI, min: 0, max: 100);

        // Act
        Assert.Throws<ArgumentNullException>(() => gcFloat.FromString(null));
    }

    [TestMethod]
    public void FromString_IsNotImplemented_ThrowsInvalidOperationException()
    {
        // Arrange
        var gcFloat = new GcInteger("NonImplementedParameter");

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => gcFloat.FromString(Math.PI.ToString()));
    }

    [TestMethod]
    public void DeepCopy_AreEqualButNotSame()
    {
        // Arrange
        var gcFloat = GetFloat("TestFloat");

        // Act
        var copyFloat = gcFloat.Copy();

        // Assert
        Assert.AreNotSame(gcFloat, copyFloat);
        Assert.AreEqual(gcFloat.Name, copyFloat.Name);
        Assert.AreEqual(gcFloat.DisplayName, copyFloat.DisplayName);
        Assert.AreEqual(gcFloat.Category, copyFloat.Category);
        Assert.AreEqual(gcFloat.Value, copyFloat.Value);
        Assert.AreEqual(gcFloat.Min, copyFloat.Min);
        Assert.AreEqual(gcFloat.Max, copyFloat.Max);
        Assert.AreEqual(gcFloat.Increment, copyFloat.Increment);
        Assert.AreEqual(gcFloat.Type, copyFloat.Type);
        Assert.AreEqual(gcFloat.IsReadable, copyFloat.IsReadable);
        Assert.AreEqual(gcFloat.IsWritable, copyFloat.IsWritable);
        Assert.AreEqual(gcFloat.IsImplemented, copyFloat.IsImplemented);
        Assert.AreEqual(gcFloat.Visibility, copyFloat.Visibility);
        Assert.AreEqual(gcFloat.Description, copyFloat.Description);
        Assert.IsTrue(gcFloat.SelectingParameters.SequenceEqual(copyFloat.SelectingParameters));
        Assert.IsTrue(gcFloat.SelectedParameters.SequenceEqual(copyFloat.SelectedParameters));
    }

    [TestMethod]
    public void DeepCopy_ChangeValue_ValueIsNotEqual()
    {
        // Arrange
        var gcFloat = GetFloat("TestFloat");
        var copyFloat = gcFloat.Copy();

        // Act
        gcFloat.Value = 1.11;

        // Assert
        Assert.AreNotEqual(gcFloat.Value, copyFloat.Value);
    }
}
