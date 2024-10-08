using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcBooleanTests
{
    private static GcBoolean GetBoolean(string name = "TestBoolean", bool value = true)
    {
        return new GcBoolean(name: name,
                             category: "Test",
                             value: value,
                             isReadable: true,
                             isWritable: true,
                             visibility: GcVisibility.Beginner,
                             description: "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcBoolean_ValidInputs_PropertiesAreValid()
    {
        // Act
        var gcBoolean = new GcBoolean(name: "TestBoolean",
                                      category: "Test",
                                      value: true,
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcBoolean);
        Assert.IsTrue(gcBoolean.IsImplemented);
        Assert.AreEqual(gcBoolean.Type, GcParameterType.Boolean);
        Assert.IsTrue(gcBoolean.Name == "TestBoolean");
        Assert.IsTrue(gcBoolean.Category == "Test");
        Assert.IsTrue(gcBoolean.Value == true);
        Assert.IsTrue(gcBoolean.IsReadable);
        Assert.IsTrue(gcBoolean.IsWritable);
        Assert.IsTrue(gcBoolean.Visibility == GcVisibility.Beginner);
        Assert.IsTrue(gcBoolean.Description == "This is a unit test parameter.");
    }

    [TestMethod]
    public void GcBoolean_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcBoolean(name: "Name containing white spaces", category: "Test", value: true));
    }

    [TestMethod]
    public void GcBoolean_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcBoolean = new GcBoolean("NonimplementedParameter");

        // Assert
        Assert.IsTrue(gcBoolean.Name == "NonimplementedParameter");
        Assert.IsFalse(gcBoolean.Value);
        Assert.IsFalse(gcBoolean.IsImplemented);
    }

    [TestMethod]
    public void ImplicitConversion_ExpectedValueIsEqual()
    {
        // Arrange
        var expectedValue = true;
        var gcBoolean = new GcBoolean(name: "TestBoolean",
                                      category: "Test",
                                      value: expectedValue,
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Act
        bool actualValue = gcBoolean;

        // Assert
        Assert.AreEqual(actualValue, expectedValue);
    }

    [TestMethod]
    public void ToString_Implemented_ReturnsValueAsString()
    {
        // Arrange
        var expectedValue = true;
        var gcBoolean = new GcBoolean(name: "TestBoolean", category: "Test", value: expectedValue);

        // Act
        var actualValue = gcBoolean.ToString();

        // Assert
        Assert.IsTrue(actualValue == expectedValue.ToString());
    }

    [TestMethod]
    public void ToString_NonImplemented_ReturnsNotImplementedString()
    {
        // Arrange
        var name = "NonimplementedParameter";
        var expectedValue = $"{name} is not implemented!";

        // Act
        var gcBoolean = new GcBoolean(name);
        var actualValue = gcBoolean.ToString();

        // Assert
        Assert.IsTrue(actualValue == expectedValue);
    }

    [TestMethod]
    [DataRow("true")]
    [DataRow("false")]
    [DataRow("True")]
    [DataRow("False")]
    public void FromString_StringIsBoolean_DoesNotThrow(string stringValue)
    {
        // Arrange
        var gcBoolean = GetBoolean("TestBoolean");

        // Act/Assert
        gcBoolean.FromString(stringValue);
    }

    [TestMethod]
    public void FromString_StringIsNotBoolean_ThrowsException()
    {
        // Arrange
        var gcBoolean = GetBoolean("TestBoolean");

        // Act/Assert
        Assert.ThrowsException<FormatException>(() => gcBoolean.FromString("NotBoolean"));
        Assert.ThrowsException<FormatException>(() => gcBoolean.FromString("42"));
        Assert.ThrowsException<FormatException>(() => gcBoolean.FromString("3.14"));
        Assert.ThrowsException<ArgumentNullException>(() => gcBoolean.FromString(null));
    }

    [TestMethod]
    public void FromString_StringIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var gcBoolean = GetBoolean("TestBoolean");

        // Act/Assert
        Assert.ThrowsException<ArgumentNullException>(() => gcBoolean.FromString(null));
    }

    [TestMethod]
    public void FromString_NonImplemented_ThrowsInvalidOperationException()
    {
        // Act
        var gcBoolean = new GcBoolean("NonimplementedParameter");

        // Assert
        Assert.ThrowsException<InvalidOperationException>(() => gcBoolean.FromString("GoodbyeUniverse"));
    }

    [TestMethod]
    public void DeepCopy_AreEqualButNotSame()
    {
        // Arrange
        var gcBoolean = GetBoolean("TestBoolean");

        // Act
        var copyBoolean = gcBoolean.Copy();

        // Assert
        Assert.AreNotSame(gcBoolean, copyBoolean);
        Assert.AreEqual(gcBoolean.Name, copyBoolean.Name);
        Assert.AreEqual(gcBoolean.DisplayName, copyBoolean.DisplayName);
        Assert.AreEqual(gcBoolean.Category, copyBoolean.Category);
        Assert.AreEqual(gcBoolean.Value, copyBoolean.Value);
        Assert.AreEqual(gcBoolean.Type, copyBoolean.Type);
        Assert.AreEqual(gcBoolean.IsReadable, copyBoolean.IsReadable);
        Assert.AreEqual(gcBoolean.IsWritable, copyBoolean.IsWritable);
        Assert.AreEqual(gcBoolean.IsImplemented, copyBoolean.IsImplemented);
        Assert.AreEqual(gcBoolean.Visibility, copyBoolean.Visibility);
        Assert.AreEqual(gcBoolean.Description, copyBoolean.Description);
        Assert.IsTrue(gcBoolean.SelectingParameters.SequenceEqual(copyBoolean.SelectingParameters));
        Assert.IsTrue(gcBoolean.SelectedParameters.SequenceEqual(copyBoolean.SelectedParameters));
    }

    [TestMethod]
    public void DeepCopy_ChangeValue_ValueIsNotEqual()
    {
        // Arrange
        var gcBoolean = GetBoolean("TestBoolean");
        var copyBoolean = gcBoolean.Copy();

        // Act
        gcBoolean.Value = !gcBoolean.Value;

        // Assert
        Assert.AreNotEqual(gcBoolean.Value, copyBoolean.Value);
    }
}