using System;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class GcBufferExtensionsTests
{
    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    [DataRow(PixelFormat.Mono14p)]
    [DataRow(PixelFormat.BGR10p)]
    [DataRow(PixelFormat.BGR12p)]
    [DataRow(PixelFormat.RGB10p)]
    [DataRow(PixelFormat.RGB12p)]
    [DataRow(PixelFormat.BGRa10p)]
    [DataRow(PixelFormat.BGRa12p)]
    [DataRow(PixelFormat.RGBa10p)]
    [DataRow(PixelFormat.RGBa12p)]
    [DataRow(PixelFormat.BayerBG10p)]
    [DataRow(PixelFormat.BayerBG12p)]
    [DataRow(PixelFormat.BayerGB10p)]
    [DataRow(PixelFormat.BayerGB12p)]
    [DataRow(PixelFormat.BayerRG10p)]
    [DataRow(PixelFormat.BayerRG12p)]
    [DataRow(PixelFormat.BayerGR10p)]
    [DataRow(PixelFormat.BayerGR12p)]
    public void UnpackBuffer_SupportedFormat_ReturnsUnpackedBuffer(PixelFormat packedFormat)
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var numChannels = GenICamHelper.GetNumChannels(packedFormat);
        var bitDepth = GenICamHelper.GetBitsPerPixelPerChannel(packedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;

        // Provide a few bytes of packed data (zeros). Size intentionally larger than the minima to avoid constructor checks.
        var packedImageData = new byte[(width * height * numChannels * bitDepth + 7) / 8]; // all zeros

        var packedBuffer = new GcBuffer(packedImageData, width, height, packedFormat, frameId, timeStamp);

        // Pre-compute expected values based on the input packed buffer
        var expectedUnpackedPixelFormat = Enum.Parse<PixelFormat>(packedBuffer.PixelFormat.ToString().Replace("p", string.Empty));
        var expectedUnpackedLength = (int)(width * height * numChannels * ((bitDepth + 7) / 8));

        // Act
        var unpackedBuffer = packedBuffer.Unpack();

        // Assert
        Assert.IsNotNull(unpackedBuffer);
        Assert.AreEqual(expectedUnpackedPixelFormat, unpackedBuffer.PixelFormat, "PixelFormat should be converted by removing trailing 'p'.");
        Assert.AreEqual(width, unpackedBuffer.Width, "Width should be preserved.");
        Assert.AreEqual(height, unpackedBuffer.Height, "Height should be preserved.");
        Assert.AreEqual(packedBuffer.PixelDynamicRangeMax, unpackedBuffer.PixelDynamicRangeMax, "Pixel dynamic range max should be preserved.");
        Assert.AreEqual(packedBuffer.FrameID, unpackedBuffer.FrameID, "FrameID should be preserved.");
        Assert.AreEqual(packedBuffer.TimeStamp, unpackedBuffer.TimeStamp, "TimeStamp should be preserved.");
        Assert.IsNotNull(unpackedBuffer.ImageData, "ImageData should not be null.");
        Assert.HasCount(expectedUnpackedLength, unpackedBuffer.ImageData, "Unpacked image data length should match expected calculation.");

        // For all-zero packed input, the unpacked data bytes must be zero.
        for (int i = 0; i < unpackedBuffer.ImageData.Length; i++)
        {
            Assert.AreEqual(0, unpackedBuffer.ImageData[i], $"Unpacked byte at index {i} should be 0 when packed data is all zeros.");
        }
    }

    [TestMethod]
    public void UnpackBuffer_UnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var packedFormat = PixelFormat.Mono8; // Unsupported packed format
        var pixelDynamicRangeMax = GenICamHelper.GetPixelDynamicRangeMax(packedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var packedImageData = new byte[width * height]; // arbitrary data
        var packedBuffer = new GcBuffer(packedImageData, width, height, packedFormat, frameId, timeStamp);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => packedBuffer.Unpack());
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.BGRa10)]
    [DataRow(PixelFormat.BGRa12)]
    [DataRow(PixelFormat.RGBa10)]
    [DataRow(PixelFormat.RGBa12)]
    [DataRow(PixelFormat.BayerBG10)]
    [DataRow(PixelFormat.BayerBG12)]
    [DataRow(PixelFormat.BayerGB10)]
    [DataRow(PixelFormat.BayerGB12)]
    [DataRow(PixelFormat.BayerRG10)]
    [DataRow(PixelFormat.BayerRG12)]
    [DataRow(PixelFormat.BayerGR10)]
    [DataRow(PixelFormat.BayerGR12)]
    public void PackBuffer_SupportedFormat_ReturnsPackedBuffer(PixelFormat unpackedFormat)
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var numChannels = GenICamHelper.GetNumChannels(unpackedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;

        // Provide a few bytes of unpacked data (zeros). Length is intentionally larger than minima to avoid constructor size checks.
        var unpackedImageData = new byte[width * height * GenICamHelper.GetBitsPerPixel(unpackedFormat) / 8]; // all zeros
        var unpackedBuffer = new GcBuffer(unpackedImageData, width, height, unpackedFormat, frameId, timeStamp);

        // Pre-compute expected values based on the input unpacked buffer.
        var expectedPackedPixelFormat = Enum.Parse<PixelFormat>(unpackedBuffer.PixelFormat.ToString() + "p");
        var expectedPackedLength = (int)(width * height * numChannels * (int)GenICamHelper.GetPixelSize(expectedPackedPixelFormat) + 7) / 8;

        // Act
        var packedBuffer = unpackedBuffer.Pack();

        // Assert
        Assert.IsNotNull(packedBuffer, "Returned GcBuffer should not be null.");
        Assert.AreEqual(expectedPackedPixelFormat, packedBuffer.PixelFormat, "PixelFormat should be converted by appending trailing 'p'.");
        Assert.AreEqual(width, packedBuffer.Width, "Width should be preserved.");
        Assert.AreEqual(height, packedBuffer.Height, "Height should be preserved.");
        Assert.AreEqual(unpackedBuffer.PixelDynamicRangeMax, packedBuffer.PixelDynamicRangeMax, "Pixel dynamic range max should be preserved.");
        Assert.AreEqual(unpackedBuffer.FrameID, packedBuffer.FrameID, "FrameID should be preserved.");
        Assert.AreEqual(unpackedBuffer.TimeStamp, packedBuffer.TimeStamp, "TimeStamp should be preserved.");
        Assert.IsNotNull(packedBuffer.ImageData, "ImageData should not be null.");
        Assert.HasCount(expectedPackedLength, packedBuffer.ImageData, "Packed image data length should match expected calculation for packed format.");

        // For all-zero unpacked input, the packed data bytes must be zero.
        for (int i = 0; i < packedBuffer.ImageData.Length; i++)
        {
            Assert.AreEqual(0, packedBuffer.ImageData[i], $"Packed byte at index {i} should be 0 when unpacked data is all zeros.");
        }
    }

    [TestMethod]
    [DataRow(ByteExtensions.Endianness.BigEndian)]
    [DataRow(ByteExtensions.Endianness.LittleEndian)]
    public void PackBuffer_NonzeroSamples_PreservesBitOrderAndMetadata(ByteExtensions.Endianness endianness)
    {
        byte[] imageData = [0x01, 0x00, 0x55, 0x01, 0xFF, 0x03, 0xAA, 0xAA];
        var original = new GcBuffer(imageData, 3, 1, PixelFormat.Mono10, 42, 123);
        byte[] expected = endianness == ByteExtensions.Endianness.BigEndian
            ? [0x01, 0x54, 0xF5, 0x3F]
            : [0x3F, 0xF5, 0x54, 0x01];

        var packed = original.Pack(endianness);

        Assert.AreSequenceEqual(expected, packed.ImageData);
        Assert.AreEqual(PixelFormat.Mono10p, packed.PixelFormat);
        Assert.AreEqual(original.Width, packed.Width);
        Assert.AreEqual(original.Height, packed.Height);
        Assert.AreEqual(original.FrameID, packed.FrameID);
        Assert.AreEqual(original.TimeStamp, packed.TimeStamp);
        Assert.AreSequenceEqual(new byte[] { 0x01, 0x00, 0x55, 0x01, 0xFF, 0x03 }, packed.Unpack(endianness).ImageData);
        Assert.AreSequenceEqual(new byte[] { 0x01, 0x00, 0x55, 0x01, 0xFF, 0x03, 0xAA, 0xAA }, imageData);
    }

    [TestMethod]
    public void PackBuffer_UnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var unpackedFormat = PixelFormat.Mono8; // Unsupported unpacked format
        var pixelDynamicRangeMax = GenICamHelper.GetPixelDynamicRangeMax(unpackedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var unpackedImageData = new byte[width * height]; // arbitrary data
        var unpackedBuffer = new GcBuffer(unpackedImageData, width, height, unpackedFormat, frameId, timeStamp);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => unpackedBuffer.Pack());
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    public void UnpackBuffer_PackedBuffer_ReturnsOriginalBuffer(PixelFormat unpackedFormat)
    {
        // Arrange
        var originalBuffer = new GcBuffer(TestPatternGenerator.CreateImage(10, 10, unpackedFormat, TestPattern.GrayVerticalRamp), 10, 10, unpackedFormat, 42, (ulong)DateTime.Now.Ticks);
        var packedBuffer = originalBuffer.Pack();

        // Act
        var unpackedBuffer = packedBuffer.Unpack();

        // Assert
        Assert.AreEqual(originalBuffer.PixelFormat, unpackedBuffer.PixelFormat);
        Assert.AreEqual(originalBuffer.Width, unpackedBuffer.Width);
        Assert.AreEqual(originalBuffer.Height, unpackedBuffer.Height);
        Assert.AreEqual(GenICamHelper.GetBitsPerPixelPerChannel(originalBuffer.PixelFormat), GenICamHelper.GetBitsPerPixelPerChannel(unpackedBuffer.PixelFormat));
        Assert.AreSequenceEqual(originalBuffer.ImageData, unpackedBuffer.ImageData);
    }
}