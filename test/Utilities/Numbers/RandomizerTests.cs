using System;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class RandomizerTests
{
    [TestMethod]
    public void NextBoolean_ReturnsBoolean()
    {
        for (int i = 0; i < 100; i++)
        {
            // Arrange
            var randomizer = new Randomizer(i);

            // Act
            var value = randomizer.NextBoolean();

            // Assert
            Assert.IsInstanceOfType<bool>(value);
        }
    }

    [TestMethod]
    public void NextItem_ReturnsItem()
    {
        var collection = Enum.GetValues<DayOfWeek>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            // Arrange
            var randomizer = new Randomizer(i);

            var item = randomizer.NextItem(collection);

            // Assert
            Assert.IsInstanceOfType<DayOfWeek>(item);
        }
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 10)]
    [DataRow(-34, 65)]
    public void Next_Int32_WithinLimits_ReturnsInteger(int min, int max)
    {
        for (int i = 0; i < 100; i++)
        {
            // Arrange
            var randomizer = new Randomizer(i);

            // Act
            var integer = randomizer.Next(min, max);

            // Assert
            Assert.IsInstanceOfType<int>(integer);
            Assert.IsTrue(integer <= max && integer >= min);
        }
    }

    [TestMethod]
    [DataRow(0L, 1L)]
    [DataRow(1L, 10L)]
    [DataRow(-34L, 65L)]
    public void Next_Int64_WithinLimits_ReturnsInteger(long min, long max)
    {
        for (int i = 0; i < 100; i++)
        {
            // Arrange
            var randomizer = new Randomizer(i);

            // Act
            var integer = randomizer.Next(min, max);

            // Assert
            Assert.IsInstanceOfType<long>(integer);
            Assert.IsTrue(integer <= max && integer >= min);
        }
    }

    [TestMethod]
    [DataRow(0UL, 1UL)]
    [DataRow(1UL, 10UL)]
    [DataRow(34UL, 65UL)]
    public void Next_UInt64_WithinLimits_ReturnsInteger(ulong min, ulong max)
    {
        for (int i = 0; i < 100; i++)
        {
            // Arrange
            var randomizer = new Randomizer(i);

            // Act
            var integer = randomizer.Next(min, max);

            // Assert
            Assert.IsInstanceOfType<ulong>(integer);
            Assert.IsTrue(integer <= max && integer >= min);
        }
    }

    [TestMethod]
    [DataRow(0.0, 0.999)]
    [DataRow(1.1, 9.9)]
    [DataRow(34.43, 65.2)]
    public void Next_Double_WithinLimits_ReturnsDouble(double min, double max)
    {
        for (int i = 0; i < 100; i++)
        {
            // Arrange
            var randomizer = new Randomizer(i);

            // Act
            var value = randomizer.Next(min, max);

            // Assert
            Assert.IsInstanceOfType<double>(value);
            Assert.IsTrue(value <= max && value >= min);
        }
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 0)]
    public void Next_MaxLessThanOrEqualToMin_ThrowsArgumentOutOfRangeException(int min, int max)
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act/Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(min, max));
    }
}
