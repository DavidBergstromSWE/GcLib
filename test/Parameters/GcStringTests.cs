using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcStringTests
{
    private static GcString GetString(string name = "TestString", string value = "HelloWorld", long maxLength = 10)
    {
        return new GcString(name: name,
                            category: "Test",
                            value: value,
                            maxLength: maxLength,
                            isReadable: true,
                            isWritable: true,
                            visibility: GcVisibility.Beginner,
                            description: "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcString_ValidInputs_PropertiesAreValid()
    {
        // Act
        var gcString = new GcString(name: "TestString",
                                    category: "Test",
                                    value: "HelloWorld",
                                    maxLength: 10,
                                    isReadable: true,
                                    isWritable: true,
                                    visibility: GcVisibility.Beginner,
                                    description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcString);
        Assert.IsTrue(gcString.IsImplemented);
        Assert.AreEqual(gcString.Type, GcParameterType.String);
        Assert.AreEqual("TestString", gcString.Name);
        Assert.AreEqual("Test", gcString.Category);
        Assert.AreEqual("HelloWorld", gcString.Value);
        Assert.AreEqual(10, gcString.MaxLength);
        Assert.IsTrue(gcString.IsReadable);
        Assert.IsTrue(gcString.IsWritable);
        Assert.AreEqual(GcVisibility.Beginner, gcString.Visibility);
        Assert.AreEqual("This is a unit test parameter.", gcString.Description);
    }

    [TestMethod]
    public void GcString_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcString(name: "Name containing white spaces", category: "Test", value: "HelloWorld", maxLength: 10));
    }

    [TestMethod]
    public void GcString_InvalidMaxLength_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcString(name: "Name containing white spaces", category: "Test", value: "HelloWorld", maxLength: -1));
    }

    [TestMethod]
    public void GcString_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcString = new GcString("NonimplementedParameter");

        // Assert
        Assert.AreEqual("NonimplementedParameter", gcString.Name);
        Assert.IsNull(gcString.Value);
        Assert.AreEqual(0, gcString.MaxLength);
        Assert.IsFalse(gcString.IsImplemented);
    }

    [TestMethod]
    public void FromString_StringIsValid_ValueIsEqual()
    {
        // Arrange
        var gcString = GetString();
        var expectedValue = "Goodbye";

        // Act      
        gcString.FromString(expectedValue);

        // Assert
        var actualValue = gcString.Value;
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void FromString_NonImplemented_ThrowsInvalidOperationException()
    {
        // Act
        var gcString = new GcString("NonimplementedParameter");

        // Assert
        Assert.Throws<InvalidOperationException>(() => gcString.FromString("GoodbyeUniverse"));
    }

    [TestMethod]
    public void ToString_NonImplemented_ReturnsNotImplementedString()
    {
        // Arrange
        var name = "NonimplementedParameter";
        var expectedValue = $"{name} is not implemented!";

        // Act
        var gcString = new GcString(name);
        var actualValue = gcString.ToString();

        // Assert
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void ImplicitConversion_ExpectedValueIsEqual()
    {
        // Arrange
        var expectedValue = "HelloWorld";
        var gcString = new GcString(name: "TestString",
                                    category: "Test",
                                    value: expectedValue,
                                    maxLength: 10,
                                    isReadable: true,
                                    isWritable: true,
                                    visibility: GcVisibility.Beginner,
                                    description: "This is a unit test parameter.");

        // Act
        string actualValue = gcString;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    public void ValueTooLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var gcString = GetString("TestString");
        var maxLength = gcString.MaxLength;

        // Act/Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => gcString.Value = new string('a', (int)maxLength + 1));
    }

    [TestMethod]
    public void DeepCopy_AreEqualButNotSame()
    {
        // Arrange
        var gcString = GetString("TestString");

        // Act
        var copyString = gcString.Copy();

        // Assert
        Assert.AreNotSame(gcString, copyString);
        Assert.AreEqual(gcString.Name, copyString.Name);
        Assert.AreEqual(gcString.DisplayName, copyString.DisplayName);
        Assert.AreEqual(gcString.Category, copyString.Category);
        Assert.AreEqual(gcString.Value, copyString.Value);
        Assert.AreEqual(gcString.Type, copyString.Type);
        Assert.AreEqual(gcString.IsReadable, copyString.IsReadable);
        Assert.AreEqual(gcString.IsWritable, copyString.IsWritable);
        Assert.AreEqual(gcString.IsImplemented, copyString.IsImplemented);
        Assert.AreEqual(gcString.Visibility, copyString.Visibility);
        Assert.AreEqual(gcString.Description, copyString.Description);
        Assert.IsTrue(gcString.SelectingParameters.SequenceEqual(copyString.SelectingParameters));
        Assert.IsTrue(gcString.SelectedParameters.SequenceEqual(copyString.SelectedParameters));
    }

    [TestMethod]
    public void DeepCopy_ChangeValue_ValueIsNotEqual()
    {
        // Arrange
        var gcString = GetString("TestString");
        var copyString = gcString.Copy();

        // Act
        gcString.Value = "Goodbye";

        // Assert
        Assert.AreNotEqual(gcString.Value, copyString.Value);
    }
}
