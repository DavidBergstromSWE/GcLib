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

        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public void ToBytes_NullArray_ThrowsArgumentNullException()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => NumericHelper.ToBytes<int>(null));

        Assert.AreEqual("numericArray", exception.ParamName);
    }

    [TestMethod]
    public void IsNumericType_Half_ReturnsTrue()
    {
        Assert.IsTrue(NumericHelper.IsNumericType(typeof(Half)));
    }

    [TestMethod]
    public void GetMinAndMaxValue_Half_ReturnsNumericBounds()
    {
        Assert.AreEqual(Half.MinValue, NumericHelper.GetMinValue<Half>());
        Assert.AreEqual(Half.MaxValue, NumericHelper.GetMaxValue<Half>());
        Assert.AreEqual(Half.MinValue, NumericHelper.GetMinValue<Half>());
        Assert.AreEqual(Half.MaxValue, NumericHelper.GetMaxValue<Half>());
    }

    [TestMethod]
    public void DoubleToSpan_Scalar_WritesAllSupportedWidthsIntoProvidedBuffer()
    {
        Assert.AreSequenceEqual([byte.MaxValue], ConvertToBytes(byte.MaxValue, sizeof(byte)));
        Assert.AreSequenceEqual(BitConverter.GetBytes((ushort)0x1234), ConvertToBytes(0x1234, sizeof(ushort)));
        Assert.AreSequenceEqual(BitConverter.GetBytes((uint)0x12345678), ConvertToBytes(0x12345678, sizeof(uint)));
        Assert.AreSequenceEqual(BitConverter.GetBytes(0x123456789ABCD0UL), ConvertToBytes(0x123456789ABCD0UL, sizeof(ulong)));
    }

    [TestMethod]
    public void DoubleToSpan_ScalarUnsupportedWidth_ThrowsInvalidCastException()
    {
        Assert.ThrowsExactly<InvalidCastException>(DoubleToSpanWithUnsupportedWidth);
    }

    [TestMethod]
    public void DoubleToSpan_Array_WritesAllValuesIntoProvidedBuffer()
    {
        double[] values = [0x1234, 0x5678];
        byte[] buffer = new byte[values.Length * sizeof(ushort)];
        Span<byte> bytes = buffer;

        NumericHelper.DoubleToSpan(ref bytes, values);

        byte[] expected = [.. BitConverter.GetBytes((ushort)0x1234), .. BitConverter.GetBytes((ushort)0x5678)];
        Assert.AreSequenceEqual(expected, buffer);
    }

    [TestMethod]
    public void DoubleToSpan_ArrayNull_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(DoubleToSpanWithNullValues);
    }

    [TestMethod]
    public void DoubleToSpan_ArrayWrongBufferLength_ThrowsInvalidCastException()
    {
        Assert.ThrowsExactly<InvalidCastException>(DoubleToSpanWithWrongBufferLength);
    }

    [TestMethod]
    public void SpanToDouble_ByteWidths_ReturnsUnsignedValue()
    {
        Assert.AreEqual((double)byte.MaxValue, NumericHelper.SpanToDouble([byte.MaxValue]));
        Assert.AreEqual((double)ushort.MaxValue, NumericHelper.SpanToDouble(BitConverter.GetBytes(ushort.MaxValue)));
        Assert.AreEqual((double)uint.MaxValue, NumericHelper.SpanToDouble(BitConverter.GetBytes(uint.MaxValue)));
        Assert.AreEqual((double)ulong.MaxValue, NumericHelper.SpanToDouble(BitConverter.GetBytes(ulong.MaxValue)));
    }

    [TestMethod]
    public void SpanToDouble_UnsupportedWidth_ThrowsInvalidCastException()
    {
        Assert.ThrowsExactly<InvalidCastException>(() => NumericHelper.SpanToDouble(new byte[3]));
    }

    private static byte[] ConvertToBytes(double value, int width)
    {
        byte[] buffer = new byte[width];
        Span<byte> bytes = buffer;
        NumericHelper.DoubleToSpan(ref bytes, value);
        return buffer;
    }

    private static void DoubleToSpanWithUnsupportedWidth()
    {
        Span<byte> bytes = stackalloc byte[3];
        NumericHelper.DoubleToSpan(ref bytes, 1);
    }

    private static void DoubleToSpanWithNullValues()
    {
        Span<byte> bytes = stackalloc byte[1];
        NumericHelper.DoubleToSpan(ref bytes, null);
    }

    private static void DoubleToSpanWithWrongBufferLength()
    {
        Span<byte> bytes = stackalloc byte[3];
        NumericHelper.DoubleToSpan(ref bytes, [1, 2]);
    }
}
