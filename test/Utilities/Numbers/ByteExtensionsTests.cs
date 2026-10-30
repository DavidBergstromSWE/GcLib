using System;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

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
        ByteExtensions.SetBitRange(target: bytes, start: 0, length: 0, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.AreSequenceEqual(originalBytes, bytes);
    }

    [TestMethod]
    public void SetBitRange_SingleBitWithMsb0_TogglesFirstBit()
    {
        // Arrange
        var bytes = new byte[1] { 0b0000_0000 }; // binary representation of 0, where MSB is 0
        var value = new Span<byte>([0b1000_0000]); // binary representation of 128 = 1000 0000, where MSB is 1

        // Act
        ByteExtensions.SetBitRange(target: bytes, start: 0, length: 1, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Msb0);

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
        ByteExtensions.SetBitRange(target: bytes, start: 0, length: 1, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

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
        ByteExtensions.SetBitRange(target: bytes, start: 0, length: 16, value: value, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

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
        ByteExtensions.SetBitRange(target: bytes, start: 0, length: 16, value: value, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.AreEqual(0b1111_1111, bytes[0]); // First byte should be original first byte
        Assert.AreEqual(0b0000_0001, bytes[1]); // Second byte should be original second byte
    }

    [TestMethod]
    [DataRow(ByteExtensions.Endianness.BigEndian, ByteExtensions.BitNumbering.Lsb0, (byte)0x07, (byte)0xE0)]
    [DataRow(ByteExtensions.Endianness.LittleEndian, ByteExtensions.BitNumbering.Lsb0, (byte)0xE0, (byte)0x07)]
    [DataRow(ByteExtensions.Endianness.BigEndian, ByteExtensions.BitNumbering.Msb0, (byte)0xE0, (byte)0x07)]
    [DataRow(ByteExtensions.Endianness.LittleEndian, ByteExtensions.BitNumbering.Msb0, (byte)0x07, (byte)0xE0)]
    public void SetBitRange_TargetSpan_PreservesBitsOutsideRange(ByteExtensions.Endianness endianness, ByteExtensions.BitNumbering bitNumbering, byte first, byte second)
    {
        byte[] target = [0xAA, 0xFF, 0xFF, 0x55];
        ReadOnlySpan<byte> value = [0, 0];

        ByteExtensions.SetBitRange(target.AsSpan(1, 2), 3, 10, value, endianness, bitNumbering);

        Assert.AreSequenceEqual(new byte[] { 0xAA, first, second, 0x55 }, target);
    }

    [TestMethod]
    public void SetBitRange_TargetSpan_ShortValuePreservesRemainingBits()
    {
        byte[] target = [0xFF, 0xFF];
        ReadOnlySpan<byte> value = [0];

        ByteExtensions.SetBitRange(target.AsSpan(), 0, 16, value, ByteExtensions.Endianness.BigEndian);

        Assert.AreSequenceEqual(new byte[] { 0, 0xFF }, target);
    }

    [TestMethod]
    public void GetBitRange_LengthZero_DoesNotModifyDestination()
    {
        ReadOnlySpan<byte> source = [0b1010_1010, 0b0101_0101];
        byte[] destination = [0xAA, 0x55];

        source.GetBitRange(start: 0, length: 0, destination: destination);
        source.GetBitRange(start: 0, length: 0, destination: Span<byte>.Empty);

        Assert.AreSequenceEqual(new byte[] { 0xAA, 0x55 }, destination);
    }

    [TestMethod]
    [DataRow(ByteExtensions.Endianness.BigEndian, ByteExtensions.BitNumbering.Lsb0, (byte)0x75, (byte)0x04)]
    [DataRow(ByteExtensions.Endianness.LittleEndian, ByteExtensions.BitNumbering.Lsb0, (byte)0xAC, (byte)0x05)]
    [DataRow(ByteExtensions.Endianness.BigEndian, ByteExtensions.BitNumbering.Msb0, (byte)0x6B, (byte)0x00)]
    [DataRow(ByteExtensions.Endianness.LittleEndian, ByteExtensions.BitNumbering.Msb0, (byte)0x1D, (byte)0x60)]
    public void GetBitRange_DestinationSpan_ExtractsAcrossByteBoundaryAndClearsUnusedBits(ByteExtensions.Endianness endianness, ByteExtensions.BitNumbering bitNumbering, byte first, byte second)
    {
        ReadOnlySpan<byte> source = [0b1010_1101, 0b0110_0011];
        byte[] destination = [0xFF, 0xFF, 0x55];

        source.GetBitRange(start: 3, length: 11, destination: destination, endianness: endianness, bitNumbering: bitNumbering);

        Assert.AreSequenceEqual(new byte[] { first, second, 0x55 }, destination);
    }

    [TestMethod]
    public void GetBitRange_DestinationSpan_ThrowsWhenTooSmall()
    {
        // Arrange
        byte[] source = [0b1010_1101, 0b0110_0011];
        byte[] destination = new byte[1];

        // Act and Assert
        Assert.ThrowsExactly<ArgumentException>(() => ByteExtensions.GetBitRange(source, start: 0, length: 16, destination));
    }

    [TestMethod]
    public void GetBitRange_SingleBitWithMsb0_ExtractsMsbToResultMostSignificantPosition()
    {
        // Arrange
        var bytes = new byte[] { 0b1000_0000 }; // binary representation of 128, where MSB is 1

        byte[] result = new byte[1];

        // Act
        ByteExtensions.GetBitRange(source: bytes, start: 0, length: 1, destination: result, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Msb0);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual(0b1000_0000, result[0]);
    }

    [TestMethod]
    public void GetBitRange_SingleBitWithLsb0_ExtractsLsbToResultLeastSignificantPosition()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0001 }; // binary representation of 1, where LSB is 1

        byte[] result = new byte[1];

        // Act
        ByteExtensions.GetBitRange(source: bytes, start: 0, length: 1, destination: result, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual(0b0000_0001, result[0]);
    }

    [TestMethod]
    public void GetBitRange_LittleEndian_ExtractsBitsInReversedByteOrder()
    {
        // Arrange
        var bytes = new byte[] { 0b0000_0001, 0b0000_0010 }; // binary representations of 1 and 2

        byte[] result = new byte[2];

        // Act
        ByteExtensions.GetBitRange(source: bytes, start: 0, length: 16, destination: result, endianness: ByteExtensions.Endianness.LittleEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

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

        byte[] result = new byte[2];

        // Act
        ByteExtensions.GetBitRange(source: bytes, start: 0, length: 16, destination: result, endianness: ByteExtensions.Endianness.BigEndian, bitNumbering: ByteExtensions.BitNumbering.Lsb0);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual(0b0000_0001, result[0]); // First byte in result should be original first byte
        Assert.AreEqual(0b0000_0010, result[1]); // Second byte in result should be original second byte
    }
}