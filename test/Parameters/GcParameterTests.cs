using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcParameterTests
{
    [TestMethod]
    public void GcParameter_ValidName_ValidateInitialization()
    {
        // Act
        var parameter = new GcBoolean(name: "ValidName", category: "Test", value: true);

        // Assert
        Assert.AreEqual("ValidName", parameter.Name);
    }

    [TestMethod]
    public void GcParameter_NameIsNull_ThrowsArgumentNullException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBoolean(name: null, category: "Test", value: true));
    }

    [TestMethod]
    public void GcParameter_NameIsEmpty_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBoolean(name: "", category: "Test", value: true));
    }

    [TestMethod]
    public void GcParameter_NameContainsWhiteCharacters_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBoolean(name: "Invalid Name", category: "Test", value: true));
    }
}