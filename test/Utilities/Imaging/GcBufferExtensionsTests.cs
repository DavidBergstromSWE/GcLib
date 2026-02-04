using System;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;


/// <summary>
/// Tests for BufferConverter.UnpackBuffer.
/// Focuses on supported/unsupported pixel format handling and basic unpacking behavior for packed buffers.
/// </summary>
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
    public void UnpackBuffer_SupportedFormat_ReturnsUnpackedBuffer(PixelFormat packedFormat)
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var numChannels = GenICamConverter.GetNumChannels(packedFormat);
        var bitDepth = GenICamConverter.GetBitsPerPixelPerChannel(packedFormat);
        var pixelDynamicRangeMax = GenICamConverter.GetDynamicRangeMax(packedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;

        // Provide a few bytes of packed data (zeros). Size intentionally larger than the minima to avoid constructor checks.
        var packedImageData = new byte[(width * height * numChannels * bitDepth + 7) / 8]; // all zeros

        var packedBuffer = new GcBuffer(packedImageData, width, height, packedFormat, pixelDynamicRangeMax, frameId, timeStamp);

        // Pre-compute expected values based on the input packed buffer
        var expectedUnpackedPixelFormat = Enum.Parse<PixelFormat>(packedBuffer.PixelFormat.ToString().Replace("p", string.Empty));
        var expectedUnpackedLength = (int)(width * height * numChannels * ((bitDepth + 7) / 8));

        // Act
        var unpackedBuffer = GcBufferExtensions.Unpack(packedBuffer);

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
        var pixelDynamicRangeMax = GenICamConverter.GetDynamicRangeMax(packedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var packedImageData = new byte[width * height]; // arbitrary data
        var packedBuffer = new GcBuffer(packedImageData, width, height, packedFormat, pixelDynamicRangeMax, frameId, timeStamp);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => GcBufferExtensions.Unpack(packedBuffer));
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
    public void PackBuffer_SupportedFormat_ReturnsPackedBuffer(PixelFormat unpackedFormat)
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var numChannels = GenICamConverter.GetNumChannels(unpackedFormat);
        var pixelDynamicRangeMax = GenICamConverter.GetDynamicRangeMax(unpackedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;

        // Provide a few bytes of unpacked data (zeros). Length is intentionally larger than minima to avoid constructor size checks.
        var unpackedImageData = new byte[width * height * GenICamConverter.GetBitsPerPixel(unpackedFormat) / 8]; // all zeros
        var unpackedBuffer = new GcBuffer(unpackedImageData, width, height, unpackedFormat, pixelDynamicRangeMax, frameId, timeStamp);

        // Pre-compute expected values based on the input unpacked buffer.
        var expectedPackedPixelFormat = Enum.Parse<PixelFormat>(unpackedBuffer.PixelFormat.ToString() + "p");
        var expectedPackedLength = (int)(width * height * numChannels * (int)GenICamConverter.GetPixelSize(expectedPackedPixelFormat) + 7) / 8;

        // Act
        var packedBuffer = GcBufferExtensions.Pack(unpackedBuffer);

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
    public void PackBuffer_UnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var unpackedFormat = PixelFormat.Mono8; // Unsupported unpacked format
        var pixelDynamicRangeMax = GenICamConverter.GetDynamicRangeMax(unpackedFormat);
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var unpackedImageData = new byte[width * height]; // arbitrary data
        var unpackedBuffer = new GcBuffer(unpackedImageData, width, height, unpackedFormat, pixelDynamicRangeMax, frameId, timeStamp);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => GcBufferExtensions.Pack(unpackedBuffer));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    public void UnpackBuffer_PackedBuffer_ReturnsOriginalBuffer(PixelFormat unpackedFormat)
    {
        // Arrange
        var originalBuffer = new GcBuffer(TestPatternGenerator.CreateImage(10, 10, unpackedFormat, TestPattern.FrameCounter), 10, 10, unpackedFormat, GenICamConverter.GetDynamicRangeMax(unpackedFormat), 42, (ulong)DateTime.Now.Ticks);
        var packedBuffer = GcBufferExtensions.Pack(originalBuffer);

        // Act
        var unpackedBuffer = GcBufferExtensions.Unpack(packedBuffer);

        // Assert
        Assert.AreEqual(originalBuffer.PixelFormat, unpackedBuffer.PixelFormat);
        Assert.AreEqual(originalBuffer.Width, unpackedBuffer.Width);
        Assert.AreEqual(originalBuffer.Height, unpackedBuffer.Height);
        Assert.AreEqual(originalBuffer.BitDepth, unpackedBuffer.BitDepth);
        CollectionAssert.AreEqual(originalBuffer.ImageData, unpackedBuffer.ImageData);
    }
}