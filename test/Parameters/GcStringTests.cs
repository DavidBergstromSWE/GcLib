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
        Assert.IsTrue(gcString.Name == "TestString");
        Assert.IsTrue(gcString.Category == "Test");
        Assert.IsTrue(gcString.Value == "HelloWorld");
        Assert.IsTrue(gcString.MaxLength == 10);
        Assert.IsTrue(gcString.IsReadable);
        Assert.IsTrue(gcString.IsWritable);
        Assert.IsTrue(gcString.Visibility == GcVisibility.Beginner);
        Assert.IsTrue(gcString.Description == "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcString_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcString(name: "Name containing white spaces", category: "Test", value: "HelloWorld", maxLength: 10));
    }

    [TestMethod]
    public void GcString_InvalidMaxLength_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcString(name: "Name containing white spaces", category: "Test", value: "HelloWorld", maxLength: -1));
    }

    [TestMethod]
    public void GcString_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcString = new GcString("NonimplementedParameter");

        // Assert
        Assert.IsTrue(gcString.Name == "NonimplementedParameter");
        Assert.IsNull(gcString.Value);
        Assert.IsTrue(gcString.MaxLength == 0);
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
        Assert.IsTrue(actualValue == expectedValue);
    }

    [TestMethod]
    public void FromString_NonImplemented_ThrowsInvalidOperationException()
    {
        // Act
        var gcString = new GcString("NonimplementedParameter");

        // Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcString.FromString("GoodbyeUniverse"));
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
        Assert.IsTrue(actualValue == expectedValue);
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
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => gcString.Value = new string('a', (int)maxLength + 1));
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
