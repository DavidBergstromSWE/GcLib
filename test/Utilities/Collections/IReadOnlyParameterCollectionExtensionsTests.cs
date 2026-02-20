using System;
using System.Collections.Generic;
using System.Text;
using GcLib.Utilities.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests
{
    [TestClass]
    public class IReadOnlyParameterCollectionExtensionsTests
    {
        IEnumerable<GcParameter> _mockedParameters;

        #region Private methods

        /// <summary>
        /// Provides a collection of parameters of all <see cref="GcParameter"/>-derived types.
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

        #endregion

        [TestInitialize]
        public void TestInitialize()
        {
            // Retrieve parameters.
            _mockedParameters = GetParameters();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockedParameters = null;
        }

        #region GcBoolean

        [TestMethod]
        public void GetBoolean_IsBoolean_ReturnsParameterAsGcBoolean()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcBoolean = collection.GetBoolean("BooleanParameter");

            // Assert
            Assert.IsNotNull(gcBoolean);
            Assert.IsInstanceOfType<GcBoolean>(gcBoolean);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("StringParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("FakeParameter")]
        public void GetBoolean_IsNotBoolean_ReturnsNull(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcBoolean = collection.GetBoolean(parameterName);

            // Assert
            Assert.IsNull(gcBoolean);
        }

        [TestMethod]
        public void GetBooleanValue_IsBoolean_ReturnsValue()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var value = collection.GetBooleanValue("BooleanParameter");

            // Assert
            Assert.IsInstanceOfType<bool>(value);
            Assert.IsTrue(value);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void GetBooleanValue_IsNotBoolean_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.GetBooleanValue(parameterName));
        }

        [TestMethod]
        public void SetBooleanValue_IsBoolean_ValueAsExpected()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
            var expectedValue = false;

            // Act
            collection.SetBooleanValue("BooleanParameter", expectedValue);

            // Assert
            Assert.AreEqual(expected: expectedValue, actual: collection.GetBooleanValue("BooleanParameter"));
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void SetBooleanValue_IsNotBoolean_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.SetBooleanValue(parameterName, false));
        }

        #endregion

        #region GcString

        [TestMethod]
        public void GetString_IsString_ReturnsParameterAsGcString()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcString = collection.GetString("StringParameter");

            // Assert
            Assert.IsNotNull(gcString);
            Assert.IsInstanceOfType<GcString>(gcString);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("FakeParameter")]
        public void GetString_IsNotString_ReturnsNull(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcString = collection.GetString(parameterName);

            // Assert
            Assert.IsNull(gcString);
        }

        [TestMethod]
        public void GetStringValue_IsString_ReturnsValue()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var value = collection.GetStringValue("StringParameter");

            // Assert
            Assert.IsInstanceOfType<string>(value);
            Assert.AreEqual("HelloWorld", value);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("FakeParameter")]
        public void GetStringValue_IsNotString_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.GetStringValue(parameterName));
        }

        [TestMethod]
        public void SetStringValue_IsString_ValueAsExpected()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
            var expectedValue = "HejSvejs";

            // Act
            collection.SetStringValue("StringParameter", expectedValue);

            // Assert
            Assert.AreEqual(expected: expectedValue, actual: collection.GetStringValue("StringParameter"));
        }

        #endregion

        #region GcEnumeration

        [TestMethod]
        public void GetEnumeration_IsEnumeration_ReturnsParameterAsGcEnumeration()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcEnumeration = collection.GetEnumeration("EnumerationParameter");

            // Assert
            Assert.IsNotNull(gcEnumeration);
            Assert.IsInstanceOfType<GcEnumeration>(gcEnumeration);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("CommandParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void GetEnumeration_IsNotEnumeration_ReturnsNull(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcEnumeration = collection.GetEnumeration(parameterName);

            // Assert
            Assert.IsNull(gcEnumeration);
        }

        [TestMethod]
        public void GetEnumValueAsInt_IsEnumeration_ReturnsValue()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var value = collection.GetEnumValueAsInt("EnumerationParameter");

            // Assert
            Assert.IsInstanceOfType<long>(value);
            Assert.AreEqual((int)DayOfWeek.Monday, value);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("StringParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("FakeParameter")]
        public void GetEnumValueAsInt_IsNotEnumeration_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.GetEnumValueAsInt(parameterName));
        }

        [TestMethod]
        public void GetEnumValueAsString_IsEnumeration_ReturnsValue()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var value = collection.GetEnumValueAsString("EnumerationParameter");

            // Assert
            Assert.IsInstanceOfType<string>(value);
            Assert.AreEqual("Monday", value);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("StringParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("FakeParameter")]
        public void GetEnumValueAsString_IsNotEnumeration_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.GetEnumValueAsString(parameterName));
        }

        [TestMethod]
        public void SetEnumValue_AsInt_IsEnumeration_ValueAsExpected()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
            var expectedValue = (int)DayOfWeek.Friday;

            // Act
            collection.SetEnumValue("EnumerationParameter", expectedValue);

            // Assert
            Assert.AreEqual(expected: expectedValue, actual: collection.GetEnumValueAsInt("EnumerationParameter"));
        }

        [TestMethod]
        public void SetEnumValue_AsString_IsEnumeration_ValueAsExpected()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
            var expectedValue = DayOfWeek.Friday.ToString();

            // Act
            collection.SetEnumValue("EnumerationParameter", expectedValue);

            // Assert
            Assert.AreEqual(expected: expectedValue, actual: collection.GetEnumValueAsString("EnumerationParameter"));
        }

        #endregion

        #region GcInteger

        [TestMethod]
        public void GetInteger_IsInteger_ReturnsParameterAsGcInteger()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcInteger = collection.GetInteger("IntegerParameter");

            // Assert
            Assert.IsNotNull(gcInteger);
            Assert.IsInstanceOfType<GcInteger>(gcInteger);
        }

        [TestMethod]
        [DataRow("BooleanParameter")]
        [DataRow("FloatParameter")]
        [DataRow("StringParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("FakeParameter")]
        public void GetInteger_IsNotInteger_ReturnsNull(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcInteger = collection.GetInteger(parameterName);

            // Assert
            Assert.IsNull(gcInteger);
        }

        [TestMethod]
        public void GetIntegerValue_IsInteger_ReturnsValue()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var value = collection.GetIntegerValue("IntegerParameter");

            // Assert
            Assert.IsInstanceOfType<long>(value);
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        [DataRow("BooleanParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void GetIntegerValue_IsNotInteger_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.GetIntegerValue(parameterName));
        }

        [TestMethod]
        public void SetIntegerValue_IsInteger_ValueAsExpected()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
            var expectedValue = 7;

            // Act
            collection.SetIntegerValue("IntegerParameter", expectedValue);

            // Assert
            Assert.AreEqual(expected: expectedValue, actual: collection.GetIntegerValue("IntegerParameter"));
        }

        [TestMethod]
        [DataRow("BooleanParameter")]
        [DataRow("FloatParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void SetIntegerValue_IsNotInteger_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.SetIntegerValue(parameterName, 7));
        }

        #endregion

        #region GcFloat

        [TestMethod]
        public void GetFloat_IsFloat_ReturnsParameterAsGcFloat()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcFloat = collection.GetFloat("FloatParameter");

            // Assert
            Assert.IsNotNull(gcFloat);
            Assert.IsInstanceOfType<GcFloat>(gcFloat);
        }

        [TestMethod]
        [DataRow("BooleanParameter")]
        [DataRow("IntegerParameter")]
        [DataRow("StringParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("FakeParameter")]
        public void GetFloat_IsNotFloat_ReturnsNull(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcFloat = collection.GetFloat(parameterName);

            // Assert
            Assert.IsNull(gcFloat);
        }

        [TestMethod]
        public void GetFloatValue_IsFloat_ReturnsValue()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var value = collection.GetFloatValue("FloatParameter");

            // Assert
            Assert.IsInstanceOfType<double>(value);
            Assert.AreEqual(3.1, value);
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void GetFloatValue_IsNotFloat_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.GetFloatValue(parameterName));
        }

        [TestMethod]
        public void SetFloatValue_IsFloat_ValueAsExpected()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);
            var expectedValue = 2.7;

            // Act
            collection.SetFloatValue("FloatParameter", expectedValue);

            // Assert
            Assert.AreEqual(expected: expectedValue, actual: collection.GetFloatValue("FloatParameter"));
        }

        [TestMethod]
        [DataRow("IntegerParameter")]
        [DataRow("BooleanParameter")]
        [DataRow("CommandParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("StringParameter")]
        [DataRow("FakeParameter")]
        public void SetFloatValue_IsNotFloat_ThrowsInvalidOperationException(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => collection.SetFloatValue(parameterName, 2.7));
        }

        #endregion

        #region GcCommand

        [TestMethod]
        public void GetCommand_IsCommand_ReturnsParameterAsGcCommand()
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcCommand = collection.GetCommand("CommandParameter");

            // Assert
            Assert.IsNotNull(gcCommand);
            Assert.IsInstanceOfType<GcCommand>(gcCommand);
        }

        [TestMethod]
        [DataRow("BooleanParameter")]
        [DataRow("IntegerParameter")]
        [DataRow("StringParameter")]
        [DataRow("FloatParameter")]
        [DataRow("EnumerationParameter")]
        [DataRow("FakeParameter")]
        public void GetCommand_IsNotCommand_ReturnsNull(string parameterName)
        {
            // Arrange
            var collection = new ReadOnlyParameterCollection("UnitTestCollection", _mockedParameters);

            // Act
            var gcCommand = collection.GetCommand(parameterName);

            // Assert
            Assert.IsNull(gcCommand);
        }

        #endregion
    }
}