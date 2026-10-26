using System;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcBufferTests
{
    #region ConstructorTests

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void GcBuffer_ByteArrayWithValidInputs_IsNotNull(PixelFormat pixelFormat)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);

        // Act
        var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Assert
        Assert.IsNotNull(buffer);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8, 2, 3)]
    [DataRow(PixelFormat.Mono10, 2, 2)]
    [DataRow(PixelFormat.Mono12, 3, 2)]
    [DataRow(PixelFormat.Mono14, 2, 3)]
    [DataRow(PixelFormat.Mono16, 3, 2)]
    [DataRow(PixelFormat.RGB8, 3, 3)]
    public void GcBuffer_ByteArrayWithValidInputs_PropertiesAreEqual(PixelFormat pixelFormat, int width, int height)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage((uint)width, (uint)height, pixelFormat, TestPattern.GrayHorizontalRamp);
        var frameID = Random.Shared.Next();
        var timeStamp = (ulong)DateTime.Now.Ticks;

        // Act
        var buffer = new GcBuffer(imageData, (uint)width, (uint)height, pixelFormat, frameID, timeStamp);

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(imageData, buffer.ImageData)); // checks element equality
        Assert.AreEqual((uint)height, buffer.Height);
        Assert.AreEqual((uint)width, buffer.Width);
        Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), GenICamHelper.GetPixelDynamicRangeMax(buffer.PixelFormat));
        Assert.AreEqual(frameID, buffer.FrameID);
        Assert.AreEqual(timeStamp, buffer.TimeStamp);
    }

    [TestMethod]
    public void GcBuffer_ByteArrayWithValidInputs_AreSame()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);

        // Act
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Assert
        Assert.AreSame(expected: imageData, actual: buffer.ImageData); // checks reference equality (share same memory)
        Assert.IsTrue(Enumerable.SequenceEqual(imageData, buffer.ImageData)); // checks pixel data equality
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p, 3, 3, 12)] // 90 bits -> 12 bytes (rounded up)
    [DataRow(PixelFormat.Mono12p, 3, 3, 14)] // 108 bits -> 14 bytes (rounded up)
    [DataRow(PixelFormat.Mono10p, 4, 4, 20)] // 160 bits -> 20 bytes (exact)
    [DataRow(PixelFormat.RGB10p, 2, 2, 15)]  // 120 bits -> 15 bytes (exact)
    public void GcBuffer_ByteArrayWithPackedFormatAndSufficientSize_IsNotNull(PixelFormat pixelFormat, int width, int height, int requiredBytes)
    {
        // Arrange
        var imageData = new byte[requiredBytes];

        // Act
        var buffer = new GcBuffer(imageData, (uint)width, (uint)height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Assert
        Assert.IsNotNull(buffer);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p, 3, 3, 11)]
    [DataRow(PixelFormat.Mono12p, 3, 3, 13)]
    [DataRow(PixelFormat.Mono10p, 4, 4, 19)]
    [DataRow(PixelFormat.RGB10p, 2, 2, 14)]
    public void GcBuffer_ByteArrayWithPackedFormatAndInsufficientSize_ThrowsArgumentException(PixelFormat pixelFormat, int width, int height, int allocatedBytes)
    {
        // Arrange
        var imageData = new byte[allocatedBytes];

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBuffer(imageData, (uint)width, (uint)height, pixelFormat, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    public void GcBuffer_ByteArrayWithInvalidSize_ThrowsArgumentException()
    {
        // Arrange
        var imageData = new byte[] { 1 };

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    [DataRow(DepthType.Cv8U, 1)]
    [DataRow(DepthType.Cv8U, 3)]
    [DataRow(DepthType.Cv16U, 1)]
    public void GcBuffer_MatWithValidInputs_IsNotNull(DepthType depthType, int channels)
    {
        // Arrange
        var mat = Mat.Eye(3, 3, depthType, channels);

        // Act
        var buffer = new GcBuffer(mat, 0, 0);

        // Assert
        Assert.IsNotNull(buffer);
    }

    [TestMethod]
    [DataRow(2, 2, DepthType.Cv8U, 1)]
    [DataRow(3, 2, DepthType.Cv8U, 3)]
    [DataRow(2, 3, DepthType.Cv16U, 1)]
    public void GcBuffer_MatWithValidInputs_PropertiesAreEqual(int width, int height, DepthType depthType, int channels)
    {
        // Arrange
        var mat = Mat.Eye(height, width, depthType, channels);
        var frameID = Random.Shared.Next();
        var timeStamp = (ulong)DateTime.Now.Ticks;

        // Act
        var buffer = new GcBuffer(mat, frameID, timeStamp);

        // Assert
        Assert.AreEqual(buffer.Width, (uint)mat.Width);
        Assert.AreEqual(buffer.Height, (uint)mat.Height);
        Assert.AreEqual(GenICamHelper.GetNumChannels(buffer.PixelFormat), (uint)mat.NumberOfChannels);
        Assert.AreEqual(GenICamHelper.GetBitsPerPixelPerChannel(buffer.PixelFormat), (uint)EmguHelper.GetBitDepth(mat.Depth));
        Assert.AreEqual(buffer.FrameID, frameID);
        Assert.AreEqual(buffer.TimeStamp, timeStamp);
    }

    [TestMethod]
    public void GcBuffer_MatWithValidInputs_AreNotSame()
    {
        // Arrange
        var mat = Mat.Eye(1, 1, DepthType.Cv8U, 1);
        var matData = new byte[mat.Width * mat.Height * mat.ElementSize];
        mat.CopyTo(matData);

        // Act
        var buffer = new GcBuffer(mat, 0, 0);
        mat.SetTo(new MCvScalar(42));

        // Assert
        Assert.AreNotSame(buffer.ImageData, matData); // does not share memory
        Assert.AreNotEqual(buffer.GetPixel(0, 0)[0], mat.GetPixel(0, 0, 0)); // can mutate one without affecting the other
    }

    [TestMethod]
    public void GcBuffer_MatWithEmptyMat_ThrowsArgumentException()
    {
        // Arrange
        var imageData = new Mat();

        // Act
        Assert.Throws<ArgumentException>(() => new GcBuffer(imageData, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    [DataRow(DepthType.Cv8S)]
    [DataRow(DepthType.Cv16S)]
    [DataRow(DepthType.Cv32F)]
    [DataRow(DepthType.Cv32S)]
    [DataRow(DepthType.Cv64F)]
    public void GcBuffer_MatWithInvalidDepthType_ThrowsNotSupportedException(DepthType depthType)
    {
        // Arrange
        var mat = Mat.Eye(3, 3, depthType, 1);

        // Act
        Assert.Throws<NotSupportedException>(() => new GcBuffer(mat, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8, DepthType.Cv8U, 1)]
    [DataRow(PixelFormat.Mono10, DepthType.Cv16U, 1)]
    [DataRow(PixelFormat.Mono12, DepthType.Cv16U, 1)]
    [DataRow(PixelFormat.Mono16, DepthType.Cv16U, 1)]
    [DataRow(PixelFormat.RGB8, DepthType.Cv8U, 3)]
    [DataRow(PixelFormat.BGR8, DepthType.Cv8U, 3)]
    public void GcBuffer_MatAndPixelFormatWithValidInputs_PropertiesAreEqual(PixelFormat pixelFormat, DepthType depthType, int channels)
    {
        // Arrange
        var mat = Mat.Eye(2, 3, depthType, channels);
        var matData = new byte[mat.Total.ToInt32() * mat.ElementSize];
        mat.CopyTo(matData);
        var frameID = Random.Shared.Next();
        var timeStamp = (ulong)DateTime.Now.Ticks;
        var pixelDynamicRangeMax = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        // Act
        var buffer = new GcBuffer(mat, pixelFormat, frameID, timeStamp);

        // Assert
        Assert.AreEqual((uint)mat.Width, buffer.Width);
        Assert.AreEqual((uint)mat.Height, buffer.Height);
        Assert.AreEqual(GenICamHelper.GetNumChannels(pixelFormat), GenICamHelper.GetNumChannels(buffer.PixelFormat));
        Assert.AreEqual(GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat), GenICamHelper.GetBitsPerPixelPerChannel(buffer.PixelFormat));
        Assert.AreEqual(pixelFormat, buffer.PixelFormat);
        Assert.AreEqual(pixelDynamicRangeMax, GenICamHelper.GetPixelDynamicRangeMax(buffer.PixelFormat));
        Assert.AreEqual(frameID, buffer.FrameID);
        Assert.AreEqual(timeStamp, buffer.TimeStamp);
        CollectionAssert.AreEqual(matData, buffer.ImageData);
    }

    [TestMethod]
    public void GcBuffer_MatAndPixelFormatWithValidInputs_AreNotSame()
    {
        // Arrange
        var mat = Mat.Eye(1, 1, DepthType.Cv8U, 1);

        // Act
        var buffer = new GcBuffer(mat, PixelFormat.Mono8, 0, 0);
        mat.SetTo(new MCvScalar(42));

        // Assert
        Assert.AreEqual(1, buffer.GetPixel(0, 0)[0]); // buffer is unaffected by later changes to the Mat
    }

    [TestMethod]
    public void GcBuffer_MatAndPixelFormatWithEmptyMat_ThrowsArgumentException()
    {
        // Arrange
        var mat = new Mat();

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBuffer(mat, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p, DepthType.Cv16U, 1)]
    [DataRow(PixelFormat.Mono12p, DepthType.Cv16U, 1)]
    [DataRow(PixelFormat.RGB10p, DepthType.Cv16U, 3)]
    public void GcBuffer_MatAndPixelFormatWithPackedPixelFormat_ThrowsArgumentException(PixelFormat pixelFormat, DepthType depthType, int channels)
    {
        // Arrange
        var mat = Mat.Eye(3, 3, depthType, channels);

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBuffer(mat, pixelFormat, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8, DepthType.Cv8U, 3)]
    [DataRow(PixelFormat.RGB8, DepthType.Cv8U, 1)]
    [DataRow(PixelFormat.Mono16, DepthType.Cv16U, 3)]
    public void GcBuffer_MatAndPixelFormatWithMismatchingChannels_ThrowsArgumentException(PixelFormat pixelFormat, DepthType depthType, int channels)
    {
        // Arrange
        var mat = Mat.Eye(3, 3, depthType, channels);

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBuffer(mat, pixelFormat, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono16, DepthType.Cv8U, 1)]  // lower depth than pixel format
    [DataRow(PixelFormat.Mono10, DepthType.Cv8U, 1)]
    [DataRow(PixelFormat.Mono8, DepthType.Cv16U, 1)]  // higher depth than pixel format
    [DataRow(PixelFormat.RGB8, DepthType.Cv16U, 3)]
    public void GcBuffer_MatAndPixelFormatWithMismatchingBitDepth_ThrowsArgumentException(PixelFormat pixelFormat, DepthType depthType, int channels)
    {
        // Arrange
        var mat = Mat.Eye(3, 3, depthType, channels);

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new GcBuffer(mat, pixelFormat, 42, (ulong)DateTime.Now.Ticks));
    }

    [TestMethod]
    public void CopyConstructor_PropertiesAreEqualButAreNotSameObjects()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
        var originalBuffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act
        var copiedBuffer = new GcBuffer(originalBuffer);

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(copiedBuffer.ImageData, originalBuffer.ImageData)); // element are equal
        Assert.AreEqual(originalBuffer.Width, copiedBuffer.Width);
        Assert.AreEqual(originalBuffer.Height, copiedBuffer.Height);
        Assert.AreEqual(originalBuffer.PixelFormat, copiedBuffer.PixelFormat);
        Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(originalBuffer.PixelFormat), GenICamHelper.GetPixelDynamicRangeMax(copiedBuffer.PixelFormat));
        Assert.AreEqual(originalBuffer.FrameID, copiedBuffer.FrameID);
        Assert.AreEqual(originalBuffer.TimeStamp, copiedBuffer.TimeStamp);

        Assert.AreNotSame(notExpected: originalBuffer.ImageData, actual: copiedBuffer.ImageData); // does not share memory
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
        Assert.AreEqual(GenICamHelper.GetNumChannels(buffer.PixelFormat), (uint)mat.NumberOfChannels);
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
                    Assert.AreEqual(255, mat.GetPixel(row, col, 0)); // R
                    Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                    Assert.AreEqual(0, mat.GetPixel(row, col, 2)); // B
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

    #region GetPixelTests

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void GetPixel_Blue_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 3; uint height = 3;
        var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.Blue);
        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);
        var mat = buffer.ToMat();

        // Act/Assert
        for (int col = 0; col < mat.Width; col++)
            for (int row = 0; row < mat.Height; row++)
            {
                if (pixelFormat == PixelFormat.RGB8)
                {
                    Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // R
                    Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                    Assert.AreEqual(255, mat.GetPixel(row, col, 2)); // B
                }
                else if (pixelFormat == PixelFormat.BGR8)
                {
                    Assert.AreEqual(255, mat.GetPixel(row, col, 0)); // B
                    Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                    Assert.AreEqual(0, mat.GetPixel(row, col, 2)); // R
                }
            }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void GetPixel_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 3; uint height = 3;
        var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.White);
        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        for (int col = 0; col < buffer.Width; col++)
            for (int row = 0; row < buffer.Height; row++)
                for (int ch = 0; ch < GenICamHelper.GetNumChannels(buffer.PixelFormat); ch++)
                    Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), buffer.GetPixel((uint)row, (uint)col, (uint)ch));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void GetPixel_ForAllChannels_ValidatePixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 3; uint height = 3;
        var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.White);
        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        for (int col = 0; col < buffer.Width; col++)
            for (int row = 0; row < buffer.Height; row++)
                for (int ch = 0; ch < GenICamHelper.GetNumChannels(buffer.PixelFormat); ch++)
                {
                    var pixelValues = buffer.GetPixel((uint)row, (uint)col);
                    foreach (var pixelValue in pixelValues)
                        Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), pixelValue);
                }
    }

    [TestMethod]
    public void GetPixel_RowOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(3, 2));
        Assert.AreEqual("row", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(3, 2, 0));
        Assert.AreEqual("row", ex.ParamName);
    }

    [TestMethod]
    public void GetPixel_ColOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(2, 3));
        Assert.AreEqual("col", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(2, 3, 0));
        Assert.AreEqual("col", ex.ParamName);
    }

    [TestMethod]
    public void GetPixel_ChannelOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(2, 2, 1));
        Assert.AreEqual("channel", ex.ParamName);
    }

    #endregion

    #region SetPixelTests

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void SetPixel_ForSpecificChannel_ValidateChangedPixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);
        var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act
        int counter = 0;
        for (int col = 0; col < buffer.Width; col++)
            for (int row = 0; row < buffer.Height; row++)
                for (int ch = 0; ch < GenICamHelper.GetNumChannels(buffer.PixelFormat); ch++)
                {
                    buffer.SetPixel((uint)row, (uint)col, (uint)ch, counter++);
                }

        // Assert
        counter = 0;
        for (int col = 0; col < buffer.Width; col++)
            for (int row = 0; row < buffer.Height; row++)
                for (int ch = 0; ch < GenICamHelper.GetNumChannels(buffer.PixelFormat); ch++)
                {
                    Assert.AreEqual(buffer.GetPixel((uint)row, (uint)col, (uint)ch), counter++);
                }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void SetPixel_ForAllChannels_ValidateChangedPixelValues(PixelFormat pixelFormat)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);
        var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act
        for (int col = 0; col < buffer.Width; col++)
            for (int row = 0; row < buffer.Height; row++)
                for (int ch = 0; ch < GenICamHelper.GetNumChannels(buffer.PixelFormat); ch++)
                {
                    var pixelValues = new double[GenICamHelper.GetNumChannels(buffer.PixelFormat)];
                    for (int i = 0; i < GenICamHelper.GetNumChannels(buffer.PixelFormat); i++)
                        pixelValues[i] = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

                    buffer.SetPixel((uint)row, (uint)col, pixelValues);
                }

        // Assert
        for (int col = 0; col < buffer.Width; col++)
            for (int row = 0; row < buffer.Height; row++)
                for (int ch = 0; ch < GenICamHelper.GetNumChannels(buffer.PixelFormat); ch++)
                {
                    Assert.AreEqual(buffer.GetPixel((uint)row, (uint)col, (uint)ch), GenICamHelper.GetPixelDynamicRangeMax(pixelFormat));
                }
    }

    [TestMethod]
    public void SetPixel_RowOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(3, 2, 0, 33));
        Assert.AreEqual("row", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(3, 2, [33]));
        Assert.AreEqual("row", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_ColOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 3, 0, 33));
        Assert.AreEqual("col", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 3, [33]));
        Assert.AreEqual("col", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_ChannelOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, 1, 33));
        Assert.AreEqual("channel", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, [33, 33, 33]));
        Assert.AreEqual("pixelValues", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_PixelValueOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, 0, 256));
        Assert.AreEqual("pixelValue", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, [33, 2, -5]));
        Assert.AreEqual("pixelValues", ex.ParamName);
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8, 0)]
    [DataRow(PixelFormat.RGB8, 1)]
    [DataRow(PixelFormat.RGB8, 2)]
    [DataRow(PixelFormat.RGB8, 4)]
    [DataRow(PixelFormat.Mono16, 0)]
    [DataRow(PixelFormat.Mono16, 2)]
    public void SetPixel_WrongNumberOfValues_ThrowsArgumentOutOfRangeException(PixelFormat pixelFormat, int length)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(1, 1, new double[length]));
        Assert.AreEqual("pixelValues", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_SingleChannelNaNValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(1, 1, 0, double.NaN));
        Assert.AreEqual("pixelValue", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_NullValues_ThrowsArgumentNullException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        Assert.Throws<ArgumentNullException>(() => buffer.SetPixel(1, 1, null));
    }

    [TestMethod]
    public void SetPixel_NaNValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
        var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(1, 1, [double.NaN]));
        Assert.AreEqual("pixelValues", ex.ParamName);
    }

    #endregion

    #region PackedFormatPixelAccessTests

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    [DataRow(PixelFormat.Mono14p)]
    public void GetPixel_PackedMonochromeFormat_ReturnsCorrectValue(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 10;
        uint height = 10;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        // Calculate required bytes for packed format
        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Set some known integer values in the buffer (avoiding fractional values that would be truncated)
        double value1 = (double)maxValue / 2;
        double value2 = (double)maxValue / 4;
        double value3 = maxValue;

        buffer.SetPixel(0, 0, 0, value1);
        buffer.SetPixel(5, 5, 0, value2);
        buffer.SetPixel(9, 9, 0, value3);

        // Act & Assert - Use tolerance for floating-point comparison due to integer rounding
        Assert.AreEqual(value1, buffer.GetPixel(0, 0, 0), 1.0, "First pixel mismatch");
        Assert.AreEqual(value2, buffer.GetPixel(5, 5, 0), 1.0, "Second pixel mismatch");
        Assert.AreEqual(value3, buffer.GetPixel(9, 9, 0), 1.0, "Third pixel mismatch");
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    [DataRow(PixelFormat.Mono14p)]
    public void GetPixel_PackedMonochromeFormatAllChannels_ReturnsCorrectArray(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 5;
        uint height = 5;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Set known integer value
        double testValue = maxValue - 10;
        buffer.SetPixel(2, 3, [testValue]);

        // Act
        var pixelValues = buffer.GetPixel(2, 3);

        // Assert
        Assert.AreEqual(1, pixelValues.Length);
        Assert.AreEqual(testValue, pixelValues[0], 1.0);
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB10p)]
    [DataRow(PixelFormat.RGB12p)]
    public void GetPixel_PackedRGBFormat_ReturnCorrectMultiChannelValues(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 8;
        uint height = 8;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Set known RGB values (use integer values to avoid fractional rounding)
        double r = 100;
        double g = 200;
        double b = maxValue - 50;
        buffer.SetPixel(3, 4, [r, g, b]);

        // Act
        var pixelValues = buffer.GetPixel(3, 4);

        // Assert
        Assert.AreEqual(3, pixelValues.Length);
        Assert.AreEqual(r, pixelValues[0], 1.0); // Red
        Assert.AreEqual(g, pixelValues[1], 1.0); // Green
        Assert.AreEqual(b, pixelValues[2], 1.0); // Blue
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB10p)]
    [DataRow(PixelFormat.RGB12p)]
    public void GetPixel_PackedRGBFormatSingleChannel_ReturnCorrectValue(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 8;
        uint height = 8;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Set known values (use integer values to avoid fractional rounding)
        double redValue = 100;
        double greenValue = 200;
        double blueValue = 300;

        buffer.SetPixel(2, 2, 0, redValue);   // Red
        buffer.SetPixel(2, 2, 1, greenValue); // Green
        buffer.SetPixel(2, 2, 2, blueValue);  // Blue

        // Act & Assert - verify each channel
        Assert.AreEqual(redValue, buffer.GetPixel(2, 2, 0), 1.0);
        Assert.AreEqual(greenValue, buffer.GetPixel(2, 2, 1), 1.0);
        Assert.AreEqual(blueValue, buffer.GetPixel(2, 2, 2), 1.0);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    public void SetPixel_PackedMonochromeFormatSingleValue_UpdatesCorrectly(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 10;
        uint height = 10;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        double testValue = 256.0;

        // Act
        buffer.SetPixel(4, 7, 0, testValue);
        var result = buffer.GetPixel(4, 7, 0);

        // Assert
        Assert.AreEqual(testValue, result, 1.0);
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB10p)]
    [DataRow(PixelFormat.RGB12p)]
    public void SetPixel_PackedRGBFormatMultipleValues_UpdatesCorrectly(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 8;
        uint height = 8;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        double[] colors = [50.0, 150.0, 250.0];

        // Act
        buffer.SetPixel(5, 6, colors);
        var result = buffer.GetPixel(5, 6);

        // Assert
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual(colors[0], result[0], 1.0);
        Assert.AreEqual(colors[1], result[1], 1.0);
        Assert.AreEqual(colors[2], result[2], 1.0);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    [DataRow(PixelFormat.RGB10p)]
    [DataRow(PixelFormat.RGB12p)]
    public void GetSetPixel_PackedFormatIndexer_WorksCorrectly(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 6;
        uint height = 6;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);
        uint numChannels = GenICamHelper.GetNumChannels(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act - Test single channel indexer
        if (numChannels == 1)
        {
            double testValue = 128.0;
            buffer[2, 3, 0] = testValue;
            var result = buffer[2, 3, 0];
            Assert.AreEqual(testValue, result, 1.0);
        }
        else
        {
            // Test multi-channel indexer
            double[] testValues = [50.0, 150.0, 250.0];
            buffer[3, 3] = testValues;
            var result = buffer[3, 3];

            for (int i = 0; i < numChannels; i++)
                Assert.AreEqual(testValues[i], result[i], 1.0);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    public void GetSetPixel_PackedFormatMultiplePixels_AllIndependent(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 10;
        uint height = 10;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Set multiple different pixel values (using integer values)
        var testData = new (uint row, uint col, double value)[]
        {
            (0, 0, 100.0),
            (1, 1, 200.0),
            (2, 2, 300.0),
            (5, 5, 500.0),
            (9, 9, 900.0),
        };

        // Act
        foreach (var (row, col, value) in testData)
            buffer.SetPixel(row, col, 0, value);

        // Assert - verify all pixels maintained their values
        foreach (var (row, col, expectedValue) in testData)
            Assert.AreEqual(expectedValue, buffer.GetPixel(row, col, 0), 1.0,
                $"Pixel at [{row},{col}] does not match expected value");
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    public void GetSetPixel_PackedFormatBoundaryPixels_WorkCorrectly(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 5;
        uint height = 5;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Set boundary pixels
        double cornerValue = 256.0;
        buffer.SetPixel(0, 0, 0, cornerValue);              // Top-left
        buffer.SetPixel(0, width - 1, 0, 512.0);             // Top-right
        buffer.SetPixel(height - 1, 0, 0, 768.0);            // Bottom-left
        buffer.SetPixel(height - 1, width - 1, 0, maxValue); // Bottom-right

        // Act & Assert
        Assert.AreEqual(cornerValue, buffer.GetPixel(0, 0, 0), 1.0);
        Assert.AreEqual(512.0, buffer.GetPixel(0, width - 1, 0), 1.0);
        Assert.AreEqual(768.0, buffer.GetPixel(height - 1, 0, 0), 1.0);
        Assert.AreEqual(maxValue, buffer.GetPixel(height - 1, width - 1, 0), 1.0);
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB10p)]
    [DataRow(PixelFormat.RGB12p)]
    public void GetSetPixel_PackedRGBFormatAllChannels_EachChannelIndependent(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 6;
        uint height = 6;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        uint row = 3, col = 3;

        // Act - Set and modify individual channels
        buffer.SetPixel(row, col, 0, 100.0);  // Red
        buffer.SetPixel(row, col, 1, 200.0);  // Green
        buffer.SetPixel(row, col, 2, 300.0);  // Blue

        // Verify initial values
        Assert.AreEqual(100.0, buffer.GetPixel(row, col, 0), 1.0);
        Assert.AreEqual(200.0, buffer.GetPixel(row, col, 1), 1.0);
        Assert.AreEqual(300.0, buffer.GetPixel(row, col, 2), 1.0);

        // Modify red channel
        buffer.SetPixel(row, col, 0, 400.0);

        // Assert - verify red changed but others didn't
        Assert.AreEqual(400.0, buffer.GetPixel(row, col, 0), 1.0);
        Assert.AreEqual(200.0, buffer.GetPixel(row, col, 1), 1.0); // Green unchanged
        Assert.AreEqual(300.0, buffer.GetPixel(row, col, 2), 1.0); // Blue unchanged
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10p)]
    [DataRow(PixelFormat.Mono12p)]
    public void SetPixel_PackedFormatInvalidValue_ThrowsArgumentOutOfRangeException(PixelFormat pixelFormat)
    {
        // Arrange
        uint width = 5;
        uint height = 5;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

        uint bitsPerPixel = GenICamHelper.GetBitsPerPixel(pixelFormat);
        uint requiredBytes = (width * height * bitsPerPixel + 7) / 8;
        byte[] imageData = new byte[requiredBytes];

        var buffer = new GcBuffer(imageData, width, height, pixelFormat, 42, (ulong)DateTime.Now.Ticks);

        // Act & Assert - Value exceeds max
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, 0, maxValue + 1));
        Assert.AreEqual("pixelValue", ex.ParamName);

        // Act & Assert - Negative value
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, 0, -1.0));
        Assert.AreEqual("pixelValue", ex.ParamName);
    }

    [TestMethod]
    public void GetSetPixel_PackedFormat_ConsistentWithUnpackedFormat()
    {
        // Arrange - Create packed and unpacked buffers with same data
        uint width = 4;
        uint height = 4;
        uint maxValue = GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono10);

        // Unpacked buffer
        var unpackedImageData = TestPatternGenerator.CreateImage(width, height, PixelFormat.Mono10, TestPattern.White);
        var unpackedBuffer = new GcBuffer(unpackedImageData, width, height, PixelFormat.Mono10, 42, (ulong)DateTime.Now.Ticks);

        // Packed buffer
        uint bitsPerPixelPacked = GenICamHelper.GetBitsPerPixel(PixelFormat.Mono10p);
        uint requiredBytesPacked = (width * height * bitsPerPixelPacked + 7) / 8;
        byte[] packedImageData = new byte[requiredBytesPacked];
        var packedBuffer = new GcBuffer(packedImageData, width, height, PixelFormat.Mono10p, 42, (ulong)DateTime.Now.Ticks);

        // Set same values in both
        double testValue = maxValue * 0.75;
        unpackedBuffer.SetPixel(1, 1, 0, testValue);
        packedBuffer.SetPixel(1, 1, 0, testValue);

        // Act & Assert
        var unpackedResult = unpackedBuffer.GetPixel(1, 1, 0);
        var packedResult = packedBuffer.GetPixel(1, 1, 0);
        Assert.AreEqual(unpackedResult, packedResult);
    }

    #endregion

    #region GetSizeTests

    [TestMethod]
    [DataRow(2, 2)]
    [DataRow(3, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 3)]
    [DataRow(4, 4)]
    [DataRow(100, 100)]
    public void GetSize_SizeIsValid(int width, int height)
    {
        // Arrange
        var imageData = TestPatternGenerator.CreateImage((uint)width, (uint)height, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
        var buffer = new GcBuffer(imageData, (uint)width, (uint)height, PixelFormat.Mono8, 42, (ulong)DateTime.Now.Ticks);

        // Act
        var size = buffer.GetSize();

        // Assert
        Assert.AreEqual(height, size.Height);
        Assert.AreEqual(width, size.Width);
    }

    #endregion
}