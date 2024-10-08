using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests
{
    [TestClass]
    public class GcEnumEntryTests
    {
        [TestMethod]
        public void GcEnumEntry_WithoutNumeric_PropertiesAreValid()
        {
            // Arrange
            var expectedString = "TestEnum0";
            var expectedInt = 0;

            // Act
            var gcEnumEntry = new GcEnumEntry(expectedString, expectedInt);

            // Assert
            Assert.IsNotNull(gcEnumEntry);
            Assert.AreEqual(gcEnumEntry.ValueString, expectedString);
            Assert.AreEqual(gcEnumEntry.ValueInt, expectedInt);
            Assert.AreEqual(gcEnumEntry.NumericValue, expectedInt);
        }

        [TestMethod]
        public void GcEnumEntry_WithNumeric_PropertiesAreValid()
        {
            // Arrange
            var expectedString = "TestEnum0";
            var expectedInt = 0;
            var expectedNumeric = 1.3;

            var gcEnumEntry = new GcEnumEntry(expectedString, expectedInt, expectedNumeric);

            Assert.IsNotNull(gcEnumEntry);
            Assert.AreEqual(gcEnumEntry.ValueString, expectedString);
            Assert.AreEqual(gcEnumEntry.ValueInt, expectedInt);
            Assert.AreEqual(gcEnumEntry.NumericValue, expectedNumeric);
        }

        [TestMethod]
        [DataRow("TestEnum0", 0)]
        [DataRow("#23", 23)]
        [DataRow("123", 123)]
        public void Operator_Equals_ReturnsTrue(string enumName, int enumValue)
        {
            // Arrange
            var gcEnumEntry1 = new GcEnumEntry(enumName, enumValue);
            var gcEnumEntry2 = new GcEnumEntry(enumName, enumValue);
            bool expectedEqual = true;

            // Act
            bool actualEqual = gcEnumEntry1 == gcEnumEntry2;

            // Assert
            Assert.IsTrue(actualEqual);
            Assert.AreEqual(actualEqual, expectedEqual);
        }

        [TestMethod]
        [DataRow("TestEnum1", 0)]
        [DataRow("TestEnum0", 1)]
        public void Operator_Equals_ReturnsFalse(string enumName, int enumValue)
        {
            // Arrange
            var gcEnumEntry1 = new GcEnumEntry("TestEnum0", 0);
            var gcEnumEntry2 = new GcEnumEntry(enumName, enumValue);
            bool expectedEqual = false;

            // Act
            bool actualEqual = gcEnumEntry1 == gcEnumEntry2;

            // Assert
            Assert.IsFalse(actualEqual);
            Assert.AreEqual(actualEqual, expectedEqual);
        }

        [TestMethod]
        [DataRow("TestEnum0", 0)]
        [DataRow("#23", 23)]
        [DataRow("123", 123)]
        public void Operator_NotEquals_ReturnsFalse(string enumName, int enumValue)
        {
            // Arrange
            var gcEnumEntry1 = new GcEnumEntry(enumName, enumValue);
            var gcEnumEntry2 = new GcEnumEntry(enumName, enumValue);
            bool expectedNotEqual = false;

            // Act
            bool actualNotEqual = gcEnumEntry1 != gcEnumEntry2;

            // Assert
            Assert.IsFalse(actualNotEqual);
            Assert.AreEqual(actualNotEqual, expectedNotEqual);
        }

        [TestMethod]
        [DataRow("TestEnum1", 0)]
        [DataRow("TestEnum0", 1)]
        public void Operator_NotEquals_ReturnsTrue(string enumName, int enumValue)
        {
            // Arrange
            var gcEnumEntry1 = new GcEnumEntry("TestEnum0", 0);
            var gcEnumEntry2 = new GcEnumEntry(enumName, enumValue);
            bool expectedNotEqual = true;

            // Act
            bool actualNotEqual = gcEnumEntry1 != gcEnumEntry2;

            // Assert
            Assert.IsTrue(actualNotEqual);
            Assert.AreEqual(actualNotEqual, expectedNotEqual);
        }

        [TestMethod]
        public void Equals_OnlyReturnsTrueIfStringAndValueAreSame()
        {
            Assert.IsTrue(Equals(new GcEnumEntry("TestEnum0", 0), new GcEnumEntry("TestEnum0", 0)));
            Assert.IsFalse(Equals(new GcEnumEntry("TestEnum0", 0), new GcEnumEntry("TestEnum0", 1)));
            Assert.IsFalse(Equals(new GcEnumEntry("TestEnum0", 0), new GcEnumEntry("TestEnum1", 0)));
            Assert.IsFalse(Equals(new GcEnumEntry("TestEnum0", 0), new GcEnumEntry("TestEnum1", 1)));
        }

        [TestMethod]
        public void ToString_IsValueString()
        {
            // Arrange
            var gcEnumEntry = new GcEnumEntry("TestEnum0", 0);
            var expectedString = "TestEnum0";
            
            // Act
            var actualString = gcEnumEntry.ToString();

            // Assert
            Assert.AreEqual(expectedString, actualString);
        }

        [TestMethod]
        public void DeepCopy_AreEqualButNotSame()
        {
            // Arrange
            var gcEnumEntry = new GcEnumEntry("TestEnum0", 0);

            // Act
            var copyEnumEntry = gcEnumEntry.DeepCopy();

            // Assert
            Assert.AreNotSame(gcEnumEntry, copyEnumEntry);
            Assert.AreEqual(gcEnumEntry.ValueInt, copyEnumEntry.ValueInt);
            Assert.AreEqual(gcEnumEntry.ValueString, copyEnumEntry.ValueString);
            Assert.AreEqual(gcEnumEntry.NumericValue, copyEnumEntry.NumericValue);
        }
    }
}
