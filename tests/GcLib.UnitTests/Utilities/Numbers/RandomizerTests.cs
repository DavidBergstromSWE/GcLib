using System;
using System.Numerics;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

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
        var randomizer = new Randomizer();

        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(min, max));
    }

    [TestMethod]
    public void Next_Int32RangeEndingAtMaxValue_ReturnsWithinRange()
    {
        var randomizer = new Randomizer(42);

        for (int i = 0; i < 100; i++)
        {
            int value = randomizer.Next(int.MaxValue - 1, int.MaxValue);
            Assert.IsTrue(value >= int.MaxValue - 1 && value <= int.MaxValue);
        }
    }

    [TestMethod]
    public void Next_Int64FullRange_ReturnsWithinRange()
    {
        var randomizer = new Randomizer(42);

        for (int i = 0; i < 100; i++)
        {
            long value = randomizer.Next(long.MinValue, long.MaxValue);
            Assert.IsTrue(value >= long.MinValue && value <= long.MaxValue);
        }
    }

    [TestMethod]
    public void Next_UInt64RangeEndingAtMaxValue_ReturnsWithinRange()
    {
        var randomizer = new Randomizer(42);

        for (int i = 0; i < 100; i++)
        {
            ulong value = randomizer.Next(ulong.MaxValue - 1, ulong.MaxValue);
            Assert.IsTrue(value >= ulong.MaxValue - 1 && value <= ulong.MaxValue);
        }
    }

    [TestMethod]
    public void Next_DoubleExtremeRange_ReturnsFiniteValueWithinRange()
    {
        var randomizer = new Randomizer(42);

        double value = randomizer.Next(-double.MaxValue, double.MaxValue);

        Assert.IsTrue(double.IsFinite(value));
        Assert.IsTrue(value >= -double.MaxValue && value <= double.MaxValue);
    }

    [TestMethod]
    public void Next_DecimalFullRange_ReturnsValueWithinRange()
    {
        var randomizer = new Randomizer(42);

        decimal value = randomizer.Next(decimal.MinValue, decimal.MaxValue);

        Assert.IsTrue(value >= decimal.MinValue && value <= decimal.MaxValue);
    }

    [TestMethod]
    public void Next_DoubleNonFiniteBounds_ThrowsArgumentOutOfRangeException()
    {
        var randomizer = new Randomizer(42);

        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(0.0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(double.NaN, 1.0));
    }

    [TestMethod]
    public void NextItem_NullCollection_ThrowsArgumentNullException()
    {
        var randomizer = new Randomizer();

        Assert.Throws<ArgumentNullException>(() => randomizer.NextItem<int>(null));
    }

    [TestMethod]
    public void NextItem_EmptyCollection_ThrowsArgumentException()
    {
        var randomizer = new Randomizer();

        Assert.Throws<ArgumentException>(() => randomizer.NextItem(Array.Empty<int>()));
    }

    [TestMethod]
    public void Next_IntegralTypes_IncludeBothBounds()
    {
        AssertBothBoundsOccur(sbyte.MinValue, (sbyte)(sbyte.MinValue + 1));
        AssertBothBoundsOccur((byte)(byte.MaxValue - 1), byte.MaxValue);
        AssertBothBoundsOccur(short.MinValue, (short)(short.MinValue + 1));
        AssertBothBoundsOccur((ushort)(ushort.MaxValue - 1), ushort.MaxValue);
        AssertBothBoundsOccur(int.MaxValue - 1, int.MaxValue);
        AssertBothBoundsOccur(uint.MaxValue - 1, uint.MaxValue);
        AssertBothBoundsOccur(long.MinValue, long.MinValue + 1);
        AssertBothBoundsOccur(ulong.MaxValue - 1, ulong.MaxValue);
    }

    [TestMethod]
    public void Next_FullWidthIntegralRanges_ProduceValuesOnBothSides()
    {
        var signed = new Randomizer(42);
        var unsigned = new Randomizer(42);
        bool negativeSeen = false;
        bool nonNegativeSeen = false;
        bool lowSeen = false;
        bool highSeen = false;

        for (int i = 0; i < 100; i++)
        {
            long signedValue = signed.Next(long.MinValue, long.MaxValue);
            ulong unsignedValue = unsigned.Next(0UL, ulong.MaxValue);
            negativeSeen |= signedValue < 0;
            nonNegativeSeen |= signedValue >= 0;
            lowSeen |= unsignedValue < (1UL << 63);
            highSeen |= unsignedValue >= (1UL << 63);
        }

        Assert.IsTrue(negativeSeen && nonNegativeSeen);
        Assert.IsTrue(lowSeen && highSeen);
    }

    [TestMethod]
    public void Next_LargeNonFullWidthRanges_ReturnValuesWithinBounds()
    {
        var randomizer = new Randomizer(42);
        const long signedMin = long.MinValue;
        const long signedMax = 0;
        const ulong unsignedMax = (1UL << 63);

        for (int i = 0; i < 100; i++)
        {
            long signedValue = randomizer.Next(signedMin, signedMax);
            ulong unsignedValue = randomizer.Next(0UL, unsignedMax);
            Assert.IsTrue(signedValue >= signedMin && signedValue <= signedMax);
            Assert.IsTrue(unsignedValue <= unsignedMax);
        }
    }

    [TestMethod]
    public void Next_SingleMaximum_MatchesZeroBasedRange()
    {
        var singleBound = new Randomizer(42);
        var twoBounds = new Randomizer(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(twoBounds.Next(0, 10), singleBound.Next(10));
            Assert.AreEqual(twoBounds.Next(0.0, 10.0), singleBound.Next(10.0));
            Assert.AreEqual(twoBounds.Next(0m, 10m), singleBound.Next(10m));
        }
    }

    [TestMethod]
    public void Next_SingleMaximumNotGreaterThanZero_Throws()
    {
        var randomizer = new Randomizer(42);

        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(0m));
    }

    [TestMethod]
    public void Next_SameSeed_ReproducesMixedSequence()
    {
        var first = new Randomizer(42);
        var second = new Randomizer(42);
        string[] items = ["first", "second", "third"];

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(first.Next(-100, 100), second.Next(-100, 100));
            Assert.AreEqual(first.Next(0UL, ulong.MaxValue), second.Next(0UL, ulong.MaxValue));
            Assert.AreEqual(first.Next(-1f, 1f), second.Next(-1f, 1f));
            Assert.AreEqual(first.Next(-1.0, 1.0), second.Next(-1.0, 1.0));
            Assert.AreEqual(first.Next(-1m, 1m), second.Next(-1m, 1m));
            Assert.AreEqual(first.NextBoolean(), second.NextBoolean());
            Assert.AreEqual(first.NextItem(items), second.NextItem(items));
        }
    }

    [TestMethod]
    public void Next_FloatExtremeRange_ReturnsFiniteValuesOnBothSides()
    {
        var randomizer = new Randomizer(42);
        bool negativeSeen = false;
        bool positiveSeen = false;

        for (int i = 0; i < 100; i++)
        {
            float value = randomizer.Next(-float.MaxValue, float.MaxValue);
            Assert.IsTrue(float.IsFinite(value));
            Assert.IsTrue(value >= -float.MaxValue && value <= float.MaxValue);
            negativeSeen |= value < 0;
            positiveSeen |= value > 0;
        }

        Assert.IsTrue(negativeSeen && positiveSeen);
    }

    [TestMethod]
    public void Next_DecimalSameSignRanges_ReturnValuesWithinBounds()
    {
        var randomizer = new Randomizer(42);

        for (int i = 0; i < 100; i++)
        {
            decimal positive = randomizer.Next(decimal.MaxValue / 2, decimal.MaxValue);
            decimal negative = randomizer.Next(decimal.MinValue, decimal.MinValue / 2);
            Assert.IsTrue(positive >= decimal.MaxValue / 2 && positive <= decimal.MaxValue);
            Assert.IsTrue(negative >= decimal.MinValue && negative <= decimal.MinValue / 2);
        }
    }

    [TestMethod]
    public void Next_FloatingPointNonFiniteBounds_Throws()
    {
        var randomizer = new Randomizer(42);

        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(float.NaN, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(0f, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(float.NegativeInfinity, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(0f, float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(0.0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Next(double.NegativeInfinity, 1.0));
    }

    [TestMethod]
    public void Next_UnsupportedNumericType_Throws()
    {
        var randomizer = new Randomizer(42);

        Assert.Throws<InvalidOperationException>(() => randomizer.Next(BigInteger.Zero, BigInteger.One));
    }

    [TestMethod]
    public void NextBoolean_ProducesBothValues()
    {
        var randomizer = new Randomizer(42);
        bool trueSeen = false;
        bool falseSeen = false;

        for (int i = 0; i < 100; i++)
        {
            bool value = randomizer.NextBoolean();
            trueSeen |= value;
            falseSeen |= !value;
        }

        Assert.IsTrue(trueSeen && falseSeen);
    }

    [TestMethod]
    public void NextItem_ReturnsCollectionMembersIncludingEachPosition()
    {
        var randomizer = new Randomizer(42);
        string[] items = ["first", "second", "third"];
        bool[] seen = new bool[items.Length];

        Assert.AreEqual("only", randomizer.NextItem(new[] { "only" }));
        for (int i = 0; i < 100; i++)
        {
            int index = Array.IndexOf(items, randomizer.NextItem(items));
            Assert.IsTrue(index >= 0);
            seen[index] = true;
        }

        CollectionAssert.AreEqual(new[] { true, true, true }, seen);
    }

    private static void AssertBothBoundsOccur<T>(T min, T max) where T : INumber<T>
    {
        var randomizer = new Randomizer(42);
        bool minSeen = false;
        bool maxSeen = false;

        for (int i = 0; i < 100; i++)
        {
            T value = randomizer.Next(min, max);
            Assert.IsTrue(value >= min && value <= max);
            minSeen |= value == min;
            maxSeen |= value == max;
        }

        Assert.IsTrue(minSeen && maxSeen, $"Both bounds should occur for {typeof(T).Name}.");
    }
}
