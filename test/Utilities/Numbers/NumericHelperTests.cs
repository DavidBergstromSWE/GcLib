using System;
using System.Linq;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class NumericHelperTests
{
    [TestMethod]
    public void ToBytes_UnsignedIntegers_ReturnsNativeByteRepresentation()
    {
        ushort[] values = [0, 0x1234, ushort.MaxValue];
        byte[] expected = [.. values.SelectMany(BitConverter.GetBytes)];

        byte[] actual = NumericHelper.ToBytes(values);

        Assert.AreSequenceEqual(expected, actual);
    }

    [TestMethod]
    public void ToBytes_SignedIntegers_ReturnsNativeByteRepresentation()
    {
        int[] values = [int.MinValue, -1, 0, 0x12345678, int.MaxValue];
        byte[] expected = [.. values.SelectMany(BitConverter.GetBytes)];

        byte[] actual = NumericHelper.ToBytes(values);

        Assert.AreSequenceEqual(expected, actual);
    }

    [TestMethod]
    public void ToBytes_FloatingPointValues_PreservesBitPatterns()
    {
        double[] values = [-1.5, -0.0, 0.0, double.NaN, double.PositiveInfinity];
        byte[] expected = [.. values.SelectMany(BitConverter.GetBytes)];

        byte[] actual = NumericHelper.ToBytes(values);

        Assert.AreSequenceEqual(expected, actual);
    }

    [TestMethod]
    public void ToBytes_HalfValues_ReturnsNativeByteRepresentation()
    {
        Half[] values = [Half.MinValue, (Half)1.5, Half.MaxValue];
        byte[] expected = [.. values.SelectMany(BitConverter.GetBytes)];

        byte[] actual = NumericHelper.ToBytes(values);

        Assert.AreSequenceEqual(expected, actual);
    }

    [TestMethod]
    public void ToBytes_ByteArray_ReturnsIndependentCopy()
    {
        byte[] values = [0, 1, byte.MaxValue];

        byte[] actual = NumericHelper.ToBytes(values);

        Assert.AreSequenceEqual(values, actual);
        Assert.AreNotSame(values, actual);
        actual[0] = 42;
        Assert.AreEqual((byte)0, values[0]);
        values[1] = 17;
        Assert.AreEqual((byte)1, actual[1]);
    }

    [TestMethod]
    public void ToBytes_EmptyArray_ReturnsEmptyArray()
    {
        byte[] actual = NumericHelper.ToBytes(Array.Empty<int>());

        Assert.AreEqual(0, actual.Length);
    }

    [TestMethod]
    public void ToBytes_NullArray_ThrowsArgumentNullException()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => NumericHelper.ToBytes<int>(null));

        Assert.AreEqual("numericArray", exception.ParamName);
    }
}
