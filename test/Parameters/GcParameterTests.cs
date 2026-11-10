using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Parameters;

[TestClass]
public class GcParameterTests
{
    private static GcBoolean CreateBooleanParameter(
        string name = "ValidName",
        GcVisibility visibility = GcVisibility.Beginner,
        bool isReadable = true,
        bool isWritable = true)
    {
        return new GcBoolean(name: name,
                             category: "Test",
                             value: true,
                             isReadable: isReadable,
                             isWritable: isWritable,
                             visibility: visibility);
    }

    [TestMethod]
    public void GcParameter_ValidName_ValidateInitialization()
    {
        // Act
        var parameter = CreateBooleanParameter(name: "ValidName");

        // Assert
        Assert.AreEqual("ValidName", parameter.Name);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Invalid Name")]
    [DataRow("Invalid\tName")]
    public void GcParameter_InvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => CreateBooleanParameter(name: invalidName));
    }

    [TestMethod]
    public void DisplayName_NameContainsPathSeparators_ReturnsLastPathSegment()
    {
        // Act
        var parameter = CreateBooleanParameter(name: "Root/Node\\Leaf");

        // Assert
        Assert.AreEqual("Leaf", parameter.DisplayName);
    }

    [TestMethod]
    public void IsVisible_VisibilityLevelIsExpert_IsVisibleAsExpected()
    {
        // Arrange
        var parameter = CreateBooleanParameter(visibility: GcVisibility.Expert);

        // Act/Assert
        Assert.IsFalse(parameter.IsVisible(GcVisibility.Beginner));
        Assert.IsTrue(parameter.IsVisible(GcVisibility.Expert));
        Assert.IsTrue(parameter.IsVisible(GcVisibility.Guru));
    }

    [TestMethod]
    public void ImposeAccessMode_ValuesChanged_RaisesPropertyChangedForBothFlags()
    {
        // Arrange
        var parameter = CreateBooleanParameter(isReadable: true, isWritable: true);
        var changedProperties = new List<string>();
        parameter.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        // Act
        parameter.ImposeAccessMode(isReadable: false, isWritable: false);

        // Assert
        Assert.IsFalse(parameter.IsReadable);
        Assert.IsFalse(parameter.IsWritable);
        CollectionAssert.AreEqual(new List<string> { nameof(GcParameter.IsReadable), nameof(GcParameter.IsWritable) }, changedProperties);
    }

    [TestMethod]
    public void ImposeAccessMode_ValuesUnchanged_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var parameter = CreateBooleanParameter(isReadable: true, isWritable: false);
        var changedProperties = new List<string>();
        parameter.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        // Act
        parameter.ImposeAccessMode(isReadable: true, isWritable: false);

        // Assert
        Assert.IsEmpty(changedProperties);
    }
}
