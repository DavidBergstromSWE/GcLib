using System;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class GcBufferExtensionsTests
{
    #region Unpack Tests

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
    public void Pack_UnpackedBuffer_ReturnsPropertiesAsExpected()
    {
        // Arrange
        var unpackedBuffer = new GcBuffer([136, 3, 136, 3, 136, 3, 136, 3], 4, 1, PixelFormat.Mono10, 42, 123);

        // Act
        var packedBuffer = unpackedBuffer.Pack();

        // Assert
        Assert.AreEqual(PixelFormat.Mono10p, packedBuffer.PixelFormat);
        Assert.AreEqual(4, (int)packedBuffer.Width);
        Assert.AreEqual(1, (int)packedBuffer.Height);
        Assert.HasCount(5, packedBuffer.ImageData, "Packed image data length should match expected calculation for Mono10p format.");
        Assert.AreSequenceEqual(new byte[] { 136, 35, 142, 56, 226 }, packedBuffer.ImageData);
        Assert.AreEqual(unpackedBuffer.GetPixel(0, 0, 0), packedBuffer.GetPixel(0, 0, 0));
        Assert.AreEqual(unpackedBuffer.GetPixel(0, 1, 0), packedBuffer.GetPixel(0, 1, 0));
        Assert.AreEqual(unpackedBuffer.GetPixel(0, 2, 0), packedBuffer.GetPixel(0, 2, 0));
        Assert.AreEqual(unpackedBuffer.GetPixel(0, 3, 0), packedBuffer.GetPixel(0, 3, 0));
    }

    [TestMethod]
    public void Unpack_PackedBuffer_ReturnsPropertiesAsExpected()
    {
        // Arrange
        var packedBuffer = new GcBuffer([136, 35, 142, 56, 226], 4, 1, PixelFormat.Mono10p, 42, 123);

        // Act
        var unpackedBuffer = packedBuffer.Unpack();

        // Assert
        Assert.AreSequenceEqual(new byte[] { 136, 3, 136, 3, 136, 3, 136, 3 }, unpackedBuffer.ImageData);
        Assert.AreEqual(PixelFormat.Mono10, unpackedBuffer.PixelFormat);
        Assert.AreEqual(4, (int)unpackedBuffer.Width);
        Assert.AreEqual(1, (int)unpackedBuffer.Height);
        Assert.HasCount(8, unpackedBuffer.ImageData, "Unpacked image data length should match expected calculation for Mono10 format.");
        Assert.AreEqual(packedBuffer.GetPixel(0, 0, 0), unpackedBuffer.GetPixel(0, 0, 0));
        Assert.AreEqual(packedBuffer.GetPixel(0, 1, 0), unpackedBuffer.GetPixel(0, 1, 0));
        Assert.AreEqual(packedBuffer.GetPixel(0, 2, 0), unpackedBuffer.GetPixel(0, 2, 0));
        Assert.AreEqual(packedBuffer.GetPixel(0, 3, 0), unpackedBuffer.GetPixel(0, 3, 0));
    }

    [TestMethod]
    public void Pack_Mono10p_PfncByteLayout_IsExpected()
    {
        // Arrange (PFNC Mono10p sample layout)
        var unpackedBuffer = new GcBuffer([136, 3, 136, 3, 136, 3, 136, 3], 4, 1, PixelFormat.Mono10, 42, 123);

        // Act
        var packedBuffer = unpackedBuffer.Pack();

        // Assert (B0=P0[7:0], B1=P1[5:0]|P0[9:8], B2=P2[3:0]|P1[9:6], B3=P3[1:0]|P2[9:4], B4=P3[9:2])
        Assert.AreSequenceEqual(new byte[] { 136, 35, 142, 56, 226 }, packedBuffer.ImageData);
    }

    [TestMethod]
    public void Unpack_Mono12p_PfncByteLayout_IsExpected()
    {
        // Arrange (PFNC Mono12p sample layout)
        // B0=P0[7:0], B1=P1[3:0]<<4 | P0[11:8], B2=P1[11:4]
        var packedBuffer = new GcBuffer([0xBC, 0x3A, 0x12], 2, 1, PixelFormat.Mono12p, 42, 123);

        // Act
        var unpackedBuffer = packedBuffer.Unpack();

        // Assert: P0=0xABC and P1=0x123 in little-endian 16-bit storage
        Assert.AreSequenceEqual(new byte[] { 0xBC, 0x0A, 0x23, 0x01 }, unpackedBuffer.ImageData);
        Assert.AreEqual(0xABC, unpackedBuffer.GetPixel(0, 0, 0));
        Assert.AreEqual(0x123, unpackedBuffer.GetPixel(0, 1, 0));
    }

    [TestMethod]
    public void UnpackBuffer_UnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var packedFormat = PixelFormat.Mono8;
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var packedImageData = new byte[width * height];
        var packedBuffer = new GcBuffer(packedImageData, width, height, packedFormat, frameId, timeStamp);

        // Act
        Action action = () => packedBuffer.Unpack();

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    public void UnpackBuffer_InvalidEndianness_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var packedBuffer = new GcBuffer([136, 35, 142, 56, 226], 4, 1, PixelFormat.Mono10p, 42, 123);
        var invalidEndianness = (ByteExtensions.Endianness)123;

        // Act
        Action action = () => packedBuffer.Unpack(invalidEndianness);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    #endregion

    #region Pack Tests

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
        // Arrange
        byte[] imageData = [0x01, 0x00, 0x55, 0x01, 0xFF, 0x03, 0xAA, 0xAA];
        var original = new GcBuffer(imageData, 3, 1, PixelFormat.Mono10, 42, 123);
        byte[] expected = endianness == ByteExtensions.Endianness.BigEndian
            ? [0x3F, 0xF5, 0x54, 0x01]
            : [0x01, 0x54, 0xF5, 0x3F];

        // Act
        var packed = original.Pack(endianness);

        // Assert
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
    public void PackAndUnpack_DefaultEndianness_MatchesLittleEndian()
    {
        // Arrange
        var unpacked = new GcBuffer([0x01, 0x00, 0x55, 0x01, 0xFF, 0x03], 3, 1, PixelFormat.Mono10, 42, 123);
        var packed = unpacked.Pack(ByteExtensions.Endianness.LittleEndian);

        // Act
        var packedWithDefault = unpacked.Pack();
        var unpackedWithDefault = packed.Unpack();
        var unpackedWithLittleEndian = packed.Unpack(ByteExtensions.Endianness.LittleEndian);

        // Assert
        Assert.AreSequenceEqual(packed.ImageData, packedWithDefault.ImageData);
        Assert.AreSequenceEqual(unpackedWithLittleEndian.ImageData, unpackedWithDefault.ImageData);
    }

    [TestMethod]
    public void PackBuffer_UnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var width = (uint)10;
        var height = (uint)10;
        var unpackedFormat = PixelFormat.Mono8;
        var frameId = (long)42;
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var unpackedImageData = new byte[width * height];
        var unpackedBuffer = new GcBuffer(unpackedImageData, width, height, unpackedFormat, frameId, timeStamp);

        // Act
        Action action = () => unpackedBuffer.Pack();

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    public void PackBuffer_InvalidEndianness_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var unpackedBuffer = new GcBuffer([136, 3, 136, 3], 2, 1, PixelFormat.Mono10, 42, 123);
        var invalidEndianness = (ByteExtensions.Endianness)123;

        // Act
        Action action = () => unpackedBuffer.Pack(invalidEndianness);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
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

    #endregion

    #region ToMatTests

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void ToMat_IsNotNull(PixelFormat pixelFormat)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);
        var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act
        var mat = buffer.ToMat();

        // Assert
        Assert.IsNotNull(mat);
        Assert.IsFalse(mat.IsEmpty);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8, 2, 3)]
    [DataRow(PixelFormat.Mono10, 2, 2)]
    [DataRow(PixelFormat.Mono12, 3, 2)]
    [DataRow(PixelFormat.Mono14, 2, 3)]
    [DataRow(PixelFormat.Mono16, 3, 2)]
    [DataRow(PixelFormat.RGB8, 3, 3)]
    [DataRow(PixelFormat.BGR8, 3, 3)]
    public void ToMat_PropertiesAreEqual(PixelFormat pixelFormat, int width, int height)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage((uint)width, (uint)height, pixelFormat, TestPattern.GrayHorizontalRamp);
        var buffer = new GcBuffer(imageData, (uint)width, (uint)height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act
        var mat = buffer.ToMat();

        // Assert
        Assert.AreEqual(width, mat.Width);
        Assert.AreEqual(height, mat.Height);
        Assert.AreEqual(buffer.NumChannels, (uint)mat.NumberOfChannels);
        Assert.AreEqual(EmguHelper.GetDepthType(pixelFormat), mat.Depth);
    }

    [TestMethod]
    public void ToMat_AreNotSame()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(2, 2, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
        var buffer = new GcBuffer(imageData, 2, 2, PixelFormat.Mono8, 42, 0);

        // Act
        var mat = buffer.ToMat();
        var matData = new byte[mat.Width * mat.Height * mat.ElementSize];
        mat.CopyTo(matData);
        buffer.SetPixel(0, 0, 0, 42);

        // Assert
        Assert.AreNotSame(buffer.ImageData, matData); // does not share memory
        Assert.AreNotEqual(buffer.GetPixel(0, 0)[0], mat.GetPixel(0, 0, 0)); // can mutate one without affecting the other
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void ToMat_Red_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 3; uint height = 3;
        var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.Red);
        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);
        var mat = buffer.ToMat();

        // Act/Assert
        for (int col = 0; col < mat.Width; col++)
            for (int row = 0; row < mat.Height; row++)
            {
                if (pixelFormat == PixelFormat.RGB8)
                {
                    Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // B
                    Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                    Assert.AreEqual(255, mat.GetPixel(row, col, 2)); // R
                }
                else if (pixelFormat == PixelFormat.BGR8)
                {
                    Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // B
                    Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                    Assert.AreEqual(255, mat.GetPixel(row, col, 2)); // R
                }
            }
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void ToMat_Green_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 3; uint height = 3;
        var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.Green);
        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);
        var mat = buffer.ToMat();

        // Act/Assert
        for (int col = 0; col < mat.Width; col++)
            for (int row = 0; row < mat.Height; row++)
            {
                Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // R
                Assert.AreEqual(255, mat.GetPixel(row, col, 1)); // G
                Assert.AreEqual(0, mat.GetPixel(row, col, 2)); // B
            }
    }

    #endregion

    #region DeBayer Tests

    [TestMethod]
    [DataRow(PixelFormat.BayerBG8)]
    [DataRow(PixelFormat.BayerBG10)]
    [DataRow(PixelFormat.BayerBG12)]
    [DataRow(PixelFormat.BayerBG14)]
    [DataRow(PixelFormat.BayerBG16)]
    [DataRow(PixelFormat.BayerGB8)]
    [DataRow(PixelFormat.BayerGB10)]
    [DataRow(PixelFormat.BayerGB12)]
    [DataRow(PixelFormat.BayerGB14)]
    [DataRow(PixelFormat.BayerGB16)]
    [DataRow(PixelFormat.BayerRG8)]
    [DataRow(PixelFormat.BayerRG10)]
    [DataRow(PixelFormat.BayerRG12)]
    [DataRow(PixelFormat.BayerRG14)]
    [DataRow(PixelFormat.BayerRG16)]
    [DataRow(PixelFormat.BayerGR8)]
    [DataRow(PixelFormat.BayerGR10)]
    [DataRow(PixelFormat.BayerGR12)]
    [DataRow(PixelFormat.BayerGR14)]
    [DataRow(PixelFormat.BayerGR16)]
    public void DeBayer_Bgr_ReturnsColorBufferWithExpectedMetadata(PixelFormat pixelFormat)
    {
        // Arrange
        var bayerBuffer = CreateBayerBuffer(pixelFormat);

        // Act
        var actual = bayerBuffer.DeBayer(ColorOrder.BGR);
        var defaultOrder = bayerBuffer.DeBayer();

        // Assert
        Assert.AreEqual(GetExpectedDeBayerFormat(pixelFormat, ColorOrder.BGR), actual.PixelFormat);
        Assert.AreEqual(bayerBuffer.Width, actual.Width);
        Assert.AreEqual(bayerBuffer.Height, actual.Height);
        Assert.AreEqual(bayerBuffer.FrameID, actual.FrameID);
        Assert.AreEqual(bayerBuffer.TimeStamp, actual.TimeStamp);
        Assert.AreEqual((uint)3, GenICamHelper.GetNumChannels(actual.PixelFormat));
        Assert.HasCount((int)(actual.Width * actual.Height * GenICamHelper.GetBitsPerPixel(actual.PixelFormat) / 8), actual.ImageData);
        Assert.AreSequenceEqual(actual.ImageData, defaultOrder.ImageData);
    }

    [TestMethod]
    [DataRow(PixelFormat.BayerBG8)]
    [DataRow(PixelFormat.BayerBG10)]
    [DataRow(PixelFormat.BayerBG12)]
    [DataRow(PixelFormat.BayerBG14)]
    [DataRow(PixelFormat.BayerBG16)]
    [DataRow(PixelFormat.BayerGB8)]
    [DataRow(PixelFormat.BayerGB10)]
    [DataRow(PixelFormat.BayerGB12)]
    [DataRow(PixelFormat.BayerGB14)]
    [DataRow(PixelFormat.BayerGB16)]
    [DataRow(PixelFormat.BayerRG8)]
    [DataRow(PixelFormat.BayerRG10)]
    [DataRow(PixelFormat.BayerRG12)]
    [DataRow(PixelFormat.BayerRG14)]
    [DataRow(PixelFormat.BayerRG16)]
    [DataRow(PixelFormat.BayerGR8)]
    [DataRow(PixelFormat.BayerGR10)]
    [DataRow(PixelFormat.BayerGR12)]
    [DataRow(PixelFormat.BayerGR14)]
    [DataRow(PixelFormat.BayerGR16)]
    public void DeBayer_Rgb_ReturnsColorBufferWithExpectedMetadata(PixelFormat pixelFormat)
    {
        // Arrange
        var bayerBuffer = CreateBayerBuffer(pixelFormat);

        // Act
        var actual = bayerBuffer.DeBayer(ColorOrder.RGB);

        // Assert
        Assert.AreEqual(GetExpectedDeBayerFormat(pixelFormat, ColorOrder.RGB), actual.PixelFormat);
        Assert.AreEqual(bayerBuffer.Width, actual.Width);
        Assert.AreEqual(bayerBuffer.Height, actual.Height);
        Assert.AreEqual(bayerBuffer.FrameID, actual.FrameID);
        Assert.AreEqual(bayerBuffer.TimeStamp, actual.TimeStamp);
        Assert.AreEqual((uint)3, GenICamHelper.GetNumChannels(actual.PixelFormat));
        Assert.HasCount((int)(actual.Width * actual.Height * GenICamHelper.GetBitsPerPixel(actual.PixelFormat) / 8), actual.ImageData);
    }

    [TestMethod]
    public void DeBayer_NonBayer_ThrowsArgumentException()
    {
        // Arrange
        var buffer = new GcBuffer(new byte[16], 4, 4, PixelFormat.Mono8, 42, 123);

        // Act
        Action action = () => buffer.DeBayer();

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [TestMethod]
    public void DeBayer_UnsupportedColorOrder_ThrowsNotSupportedException()
    {
        // Arrange
        var bayerBuffer = CreateBayerBuffer(PixelFormat.BayerBG8);
        var unsupportedColorOrder = (ColorOrder)123;

        // Act
        Action action = () => bayerBuffer.DeBayer(unsupportedColorOrder);

        // Assert
        Assert.Throws<NotSupportedException>(action);
    }

    private static GcBuffer CreateBayerBuffer(PixelFormat pixelFormat)
    {
        uint width = 4;
        uint height = 4;
        int byteLength = checked((int)(width * height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8));
        var imageData = new byte[byteLength];

        for (int i = 0; i < imageData.Length; i++)
            imageData[i] = (byte)(i % 251);

        return new GcBuffer(imageData, width, height, pixelFormat, 42, 123);
    }

    private static PixelFormat GetExpectedDeBayerFormat(PixelFormat bayerFormat, ColorOrder colorOrder)
    {
        uint bitDepth = GenICamHelper.GetBitsPerPixelPerChannel(bayerFormat);

        return colorOrder switch
        {
            ColorOrder.BGR => bitDepth switch
            {
                <= 8 => PixelFormat.BGR8,
                <= 10 => PixelFormat.BGR10,
                <= 12 => PixelFormat.BGR12,
                <= 14 => PixelFormat.BGR14,
                _ => PixelFormat.BGR16
            },
            ColorOrder.RGB => bitDepth switch
            {
                <= 8 => PixelFormat.RGB8,
                <= 10 => PixelFormat.RGB10,
                <= 12 => PixelFormat.RGB12,
                <= 14 => PixelFormat.RGB14,
                _ => PixelFormat.RGB16
            },
            _ => throw new NotSupportedException($"Color order '{colorOrder}' is not supported.")
        };
    }

    #endregion
}