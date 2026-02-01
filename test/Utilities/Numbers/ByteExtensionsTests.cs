using System;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class ByteExtensionsTests
{
    [TestMethod]
    public void SetBitRange_LengthZero_DoesNotModifyArray()
    {
        // Arrange
        var bytes = new byte[1] { 0b1010_1010 }; // binary representation of 170
        var originalBytes = (byte[])bytes.Clone();
        var value = new Span<byte>([0b1111_1111]); // binary representation of 255

        // Act
        ByteExtensions.SetBitRange(bytes: bytes, start: 0, length: 0, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        CollectionAssert.AreEqual(originalBytes, bytes);
    }

    [TestMethod]
    public void SetBitRange_SingleBitWithMsb0_TogglesFirstBit()
    {
        // Arrange
        var bytes = new byte[1] { 0b0000_0000 }; // binary representation of 0, where MSB is 0
        var value = new Span<byte>([0b1000_0000]); // binary representation of 128 = 1000 0000, where MSB is 1

        // Act
        ByteExtensions.SetBitRange(bytes: bytes, start: 0, length: 1, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Msb0);

        // Assert
        Assert.AreEqual(0b1000_0000, bytes[0]);
        Assert.AreNotEqual(0b0000_0000, bytes[0]);
    }

    [TestMethod]
    public void SetBitRange_SingleBitWithLsb0_DoesNotToggleFirstBit()
    {
        // Arrange
        var bytes = new byte[1] { 0b0000_0000 }; // binary representation of 0, where LSB is 0
        var value = new Span<byte>([0b1000_0000]); // binary representation of 128 = 1000 0000, where LSB is 0

        // Act
        ByteExtensions.SetBitRange(bytes: bytes, start: 0, length: 1, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.AreNotEqual(0b1000_0000, bytes[0]);
        Assert.AreEqual(0b0000_0000, bytes[0]);
    }

    [TestMethod]
    public void SetBitRange_LittleEndian_SetsBitsInReversedByteOrder()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0000, 0b0000_0000 }; // binary representations of 0
        var value = new Span<byte>([0b1111_1111, 0b0000_0001]); // binary representations of 255 and 1

        // Act
        ByteExtensions.SetBitRange(bytes: bytes, start: 0, length: 16, value: value, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.AreEqual(0b0000_0001, bytes[0]); // First byte should be original second byte
        Assert.AreEqual(0b1111_1111, bytes[1]); // Second byte should be original first byte
    }

    [TestMethod]
    public void SetBitRange_BigEndian_SetsBitsInOriginalByteOrder()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0000, 0b0000_0000 }; // binary representations of 0
        var value = new Span<byte>([0b1111_1111, 0b0000_0001]); // binary representations of 255 and 1

        // Act
        ByteExtensions.SetBitRange(bytes: bytes, start: 0, length: 16, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.AreEqual(0b1111_1111, bytes[0]); // First byte should be original first byte
        Assert.AreEqual(0b0000_0001, bytes[1]); // Second byte should be original second byte
    }

    [TestMethod]
    public void GetBitRange_LengthZero_ReturnsEmptyArray()
    {
        // Arrange
        var bytes = new byte[] { 0b1010_1010, 0b0101_0101 }; // binary representations of 170 and 85

        // Act
        var result = ByteExtensions.GetBitRange(bytes: bytes, start: 0, length: 0, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetBitRange_SingleBitWithMsb0_ExtractsMsbToResultMostSignificantPosition()
    {
        // Arrange
        var bytes = new byte[] { 0b1000_0000 }; // binary representation of 128, where MSB is 1

        // Act
        var result = ByteExtensions.GetBitRange(bytes: bytes, 0, 1, ByteExtensions.Endianness.BigEndian, ByteExtensions.BitNumbering.Msb0);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual(0b1000_0000, result[0]);
    }

    [TestMethod]
    public void GetBitRange_SingleBitWithLsb0_ExtractsLsbToResultLeastSignificantPosition()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0001 }; // binary representation of 1, where LSB is 1

        // Act
        var result = ByteExtensions.GetBitRange(bytes: bytes, 0, 1, ByteExtensions.Endianness.BigEndian, ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual(0b0000_0001, result[0]);
    }

    [TestMethod]
    public void GetBitRange_LittleEndian_ExtractsBitsInReversedByteOrder()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0001, 0b0000_0010 }; // binary representations of 1 and 2

        // Act
        var result = ByteExtensions.GetBitRange(bytes: bytes, start: 0, length: 16, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual(0b0000_0010, result[0]); // First byte in result should be original second byte
        Assert.AreEqual(0b0000_0001, result[1]); // Second byte in result should be original first byte
    }

    [TestMethod]
    public void GetBitRange_BigEndian_ExtractsBitsInOriginalByteOrder()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0001, 0b0000_0010 }; // binary representations of 1 and 2

        // Act
        var result = ByteExtensions.GetBitRange(bytes: bytes, start: 0, length: 16, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual(0b0000_0001, result[0]); // First byte in result should be original first byte
        Assert.AreEqual(0b0000_0010, result[1]); // Second byte in result should be original second byte
    }
}