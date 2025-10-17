using System;
using System.Collections.Generic;
using System.Linq;
using GcLib.Utilities.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class ReadOnlyParameterCollectionTests
{
    IEnumerable<GcParameter> _mockedParameters;

    /// <summary>
    /// Provides a mocked collection of parameters.
    /// </summary>
    /// <returns>Collection of parameters.</returns>
    private static List<GcParameter> GetParameters()
    {
        return
        [
            new GcBoolean(name: "BooleanParameter", category: "UnitTest", value: true),
            new GcInteger(name: "IntegerParameter", category: "UnitTest", value: 42, min: 0, max: 100, increment: 1, incrementMode: EIncMode.fixedIncrement, visibility: GcVisibility.Guru),
            new GcFloat(name: "FloatParameter", category: "UnitTest2", value: 3.1, min: 0.0, max: 9.9, increment: 0.1),
            new GcString(name: "StringParameter", category: "UnitTest", value: "HelloWorld", maxLength: 20),
            new GcCommand(name: "CommandParameter", category: "UnitTest2", method: () => { throw new InvalidOperationException(); }, visibility: GcVisibility.Expert),
            new GcEnumeration(name: "EnumerationParameter", category: "UnitTest", DayOfWeek.Monday, typeof(DayOfWeek))
        ];
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _mockedParameters = GetParameters();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockedParameters = null;
    }

    [TestMethod]
    public void ReadOnlyParameterCollection_ValidateInitialization()
    {
        // Arrange
        var name = "UnitTestCollection";

        // Act
        var collection = new ReadOnlyParameterCollection(name, _mockedParameters);

        // Assert
        Assert.AreEqual(name, collection.Name);
        Assert.AreEqual(collection.Count, _mockedParameters.Count());
    }

    [TestMethod]
    public void ToList_ReturnsListAsExpected()
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
        var expectedList = _mockedParameters.ToList();

        // Act
        var actualList = collection.ToList();

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(actualList, expectedList));
    }

    [TestMethod]
    [DataRow(GcVisibility.Beginner)]
    [DataRow(GcVisibility.Expert)]
    [DataRow(GcVisibility.Guru)]
    public void ToList_WithVisibility_ReturnsListAsExpected(GcVisibility visibility)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
        var expectedList = _mockedParameters.Where(p => p.Visibility <= visibility).ToList();

        // Act
        var actualList = collection.ToList(visibility);

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(actualList, expectedList));
    }

    [TestMethod]
    [DataRow(GcVisibility.Beginner)]
    [DataRow(GcVisibility.Expert)]
    [DataRow(GcVisibility.Guru)]
    public void GetPropertyList_ListAsExpected(GcVisibility visibility)
    {
        // Arrange
        ReadOnlyParameterCollection collection = new("UnitTestCollection", _mockedParameters);
        var expectedNames = _mockedParameters.Where(p => p.Visibility <= visibility && p.Type != GcParameterType.Command).Select(p => p.Name);
        var expectedValues = _mockedParameters.Where(p => p.Visibility <= visibility && p.Type != GcParameterType.Command).Select(p => p.ToString());

        // Act
        var actualNames = collection.GetPropertyList(visibility).Select(p => p.Key);
        var actualValues = collection.GetPropertyList(visibility).Select(p => p.Value);

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(actualNames, expectedNames));
        Assert.IsTrue(Enumerable.SequenceEqual(actualValues, expectedValues));
    }

    [TestMethod]
    [DataRow(GcVisibility.Beginner)]
    [DataRow(GcVisibility.Expert)]
    [DataRow(GcVisibility.Guru)]
    public void GetCategories_ReturnsCategoriesAsExpected(GcVisibility visibility)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
        var expectedList = _mockedParameters.Where(p => p.Visibility <= visibility).Select(p => p.Category).Distinct().ToList();

        // Act
        var actualList = collection.GetCategories(visibility);

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(actualList, expectedList));
    }

    [TestMethod]
    [DataRow("BooleanParameter")]
    [DataRow("IntegerParameter")]
    [DataRow("FloatParameter")]
    [DataRow("StringParameter")]
    [DataRow("CommandParameter")]
    [DataRow("EnumerationParameter")]
    public void IsImplemented_ParameterExists_ReturnsTrue(string parameterName)
    {
        // Arrange
        ReadOnlyParameterCollection collection = new("UnitTestCollection", _mockedParameters);

        // Act/Assert
        Assert.IsTrue(collection.IsImplemented(parameterName));
    }

    [TestMethod]
    [DataRow("FakeParameter")]
    public void IsImplemented_ParameterDoesNotExist_ReturnsFalse(string parameterName)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act/Assert
        Assert.IsFalse(collection.IsImplemented(parameterName));
    }

    [TestMethod]
    public void GetParameterValue_ParameterExists_ReturnValueAsExpected()
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        foreach (var parameter in _mockedParameters)
        {
            // Arrange
            var expectedValue = parameter.ToString();

            // Act
            var actualValue = collection.GetParameterValue(parameter.Name);

            // Assert
            Assert.AreEqual(expectedValue, actualValue);
        } 
    }

    [TestMethod]
    [DataRow("FakeParameter")]
    public void GetParameterValue_ParameterDoesNotExist_ReturnsNull(string parameterName)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act/Assert
        Assert.IsNull(collection.GetParameterValue(parameterName));
    }

    [TestMethod]
    [DataRow("BooleanParameter", "False")]
    [DataRow("IntegerParameter", "11")]
    [DataRow("FloatParameter", "1,3")]
    [DataRow("StringParameter", "Goodbye")]
    [DataRow("EnumerationParameter", "Tuesday")]
    public void SetParameterValue_ParameterExistsAndIsNotCommand_ValueIsUpdated(string parameterName, string expectedValue)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act
        collection.SetParameterValue(parameterName, expectedValue);

        // Arramge
        var actualValue = collection.GetParameterValue(parameterName);
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    [DataRow("CommandParameter", "Huh")]
    public void SetParameterValue_ParameterExistsAndIsCommand_ReturnsName(string parameterName, string expectedValue)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act
        collection.SetParameterValue(parameterName, expectedValue);

        // Assert
        var actualValue = collection.GetParameterValue(parameterName);
        Assert.AreEqual(parameterName, actualValue);
    }

    [TestMethod]
    public void SetParameterValue_ParameterDoesNotExists_NoExceptionsThrown()
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act/Assert
        collection.SetParameterValue("FakeParameter", "FakeValue");
    }

    [TestMethod]
    [DataRow("CommandParameter")]
    public void ExecuteParameterCommand_ParameterExistsAndIsCommand_CommandIsExecuted(string parameterName)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => collection.ExecuteParameterCommand(parameterName));
    }

    [TestMethod]
    [DataRow("BooleanParameter")]
    [DataRow("IntegerParameter")]
    [DataRow("FloatParameter")]
    [DataRow("StringParameter")]
    [DataRow("EnumerationParameter")]
    public void ExecuteParameterCommand_ParameterExistsAndIsNotCommand_NoExceptionsThrown(string parameterName)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act/Assert
        collection.ExecuteParameterCommand(parameterName);
    }

    [TestMethod]
    public void ExecuteParameterCommand_ParameterDoesNotExist_NoExceptionsThrown()
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

        // Act/Assert
        collection.ExecuteParameterCommand("FakeParameter");
    }

    [TestMethod]
    [DataRow("BooleanParameter")]
    [DataRow("IntegerParameter")]
    [DataRow("FloatParameter")]
    [DataRow("StringParameter")]
    [DataRow("CommandParameter")]
    [DataRow("EnumerationParameter")]
    public void GetParameter_ParameterExists_ReturnsExpectedParameter(string parameterName)
    {
        // Arrange
        var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
        var expectedParameter = _mockedParameters.ToList().Find(p => p.Name == parameterName);

        // Act
        var actualParameter = collection.GetParameter(parameterName);

        // Assert
        Assert.IsNotNull(actualParameter);
        Assert.AreEqual(expectedParameter.Name, actualParameter.Name);
        Assert.AreEqual(expectedParameter.Category, actualParameter.Category);
        Assert.AreEqual(expectedParameter.Type, actualParameter.Type);
        Assert.AreEqual(expectedParameter.Visibility, actualParameter.Visibility);
        Assert.AreEqual(expectedParameter.ToString(), actualParameter.ToString());
    }

    [TestMethod]
    public void GetParameter_ParameterDoesNotExist_ReturnsNull()
    {
        // Arrange
        ReadOnlyParameterCollection collection = new("UnitTestCollection", _mockedParameters);

        // Act
        var actualParameter = collection.GetParameter("FakeParameter");

        // Assert
        Assert.IsNull(actualParameter);
    }
}