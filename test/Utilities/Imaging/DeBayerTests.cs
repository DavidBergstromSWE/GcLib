using System;
using Emgu.CV;
using GcLib.UnitTests.Utilities;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class DeBayerTests
{
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
    public void Transform2BGR_MatInputWithSupportedFormat_ReturnsTransformedMat(PixelFormat pixelFormat)
    {
        // Arrange
        var rawMat = GetBayerPatternedMat();

        // Act
        var outputMat = DeBayer.ToBGR(rawMat, pixelFormat);

        // Assert
        Assert.IsNotNull(outputMat);
        Assert.AreEqual(rawMat.Width, outputMat.Width);
        Assert.AreEqual(rawMat.Height, outputMat.Height);
        Assert.AreEqual(rawMat.Depth, outputMat.Depth);
        Assert.AreEqual(3, outputMat.NumberOfChannels);
        Assert.AreNotEqual(rawMat, outputMat.Split()[0]);
        Assert.AreNotEqual(rawMat, outputMat.Split()[1]);
        Assert.AreNotEqual(rawMat, outputMat.Split()[2]);
    }

    [TestMethod]
    public void Transform2BGR_MatInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var rawMat = new Mat(100, 100, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        var unsupportedFormat = PixelFormat.Mono8;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.ToBGR(rawMat, unsupportedFormat));
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
    public void Transform2RGB_MatInputWithSupportedFormat_ReturnsTransformedMat(PixelFormat pixelFormat)
    {
        // Arrange
        var rawMat = GetBayerPatternedMat();

        // Act
        var outputMat = DeBayer.ToRGB(rawMat, pixelFormat);

        // Assert
        Assert.IsNotNull(outputMat);
        Assert.AreEqual(rawMat.Width, outputMat.Width);
        Assert.AreEqual(rawMat.Height, outputMat.Height);
        Assert.AreEqual(rawMat.Depth, outputMat.Depth);
        Assert.AreEqual(3, outputMat.NumberOfChannels);
        Assert.AreNotEqual(rawMat, outputMat.Split()[0]);
        Assert.AreNotEqual(rawMat, outputMat.Split()[1]);
        Assert.AreNotEqual(rawMat, outputMat.Split()[2]);
    }

    [TestMethod]
    public void Transform2RGB_MatInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var rawMat = new Mat(100, 100, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        var unsupportedFormat = PixelFormat.Mono8;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.ToRGB(rawMat, unsupportedFormat));
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
    public void Transform2BGR_GcBufferInputWithSupportedFormat_ReturnsTransformedBuffer(PixelFormat pixelFormat)
    {
        // Arrange
        var rawBuffer = GetBayerPatternedBuffer(pixelFormat);

        // Act
        var outputBuffer = DeBayer.ToBGR(rawBuffer);

        // Assert
        Assert.IsNotNull(outputBuffer);
        Assert.AreEqual(rawBuffer.Width, outputBuffer.Width);
        Assert.AreEqual(rawBuffer.Height, outputBuffer.Height);
        Assert.AreEqual(rawBuffer.BitDepth, outputBuffer.BitDepth);
        Assert.AreEqual(rawBuffer.PixelDynamicRangeMax, outputBuffer.PixelDynamicRangeMax);
        Assert.AreEqual(3, (int)outputBuffer.NumChannels);
        Assert.Contains(outputBuffer.PixelFormat, [PixelFormat.BGR8, PixelFormat.BGR10, PixelFormat.BGR12, PixelFormat.BGR14, PixelFormat.BGR16]);
        Assert.AreEqual(rawBuffer.FrameID, outputBuffer.FrameID);
        Assert.AreEqual(rawBuffer.TimeStamp, outputBuffer.TimeStamp);
    }

    [TestMethod]
    public void Transform2BGR_GcBufferInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer(10, 10, PixelFormat.Mono8);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.ToBGR(buffer));
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
    public void Transform2RGB_GcBufferInputWithSupportedFormat_ReturnsTransformedBuffer(PixelFormat pixelFormat)
    {
        // Arrange
        var rawBuffer = GetBayerPatternedBuffer(pixelFormat);

        // Act
        var outputBuffer = DeBayer.ToRGB(rawBuffer);

        // Assert
        Assert.IsNotNull(outputBuffer);
        Assert.AreEqual(rawBuffer.Width, outputBuffer.Width);
        Assert.AreEqual(rawBuffer.Height, outputBuffer.Height);
        Assert.AreEqual(rawBuffer.BitDepth, outputBuffer.BitDepth);
        Assert.AreEqual(rawBuffer.PixelDynamicRangeMax, outputBuffer.PixelDynamicRangeMax);
        Assert.AreEqual(3, (int)outputBuffer.NumChannels);
        Assert.Contains(outputBuffer.PixelFormat, [PixelFormat.RGB8, PixelFormat.RGB10, PixelFormat.RGB12, PixelFormat.RGB14, PixelFormat.RGB16]);
        Assert.AreEqual(rawBuffer.FrameID, outputBuffer.FrameID);
        Assert.AreEqual(rawBuffer.TimeStamp, outputBuffer.TimeStamp);
    }

    [TestMethod]
    public void Transform2RGB_GcBufferInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer(10, 10, PixelFormat.Mono8);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.ToRGB(buffer));
    }

    #region Private Methods

    private static GcBuffer GetBayerPatternedBuffer(PixelFormat bayerFormat)
    {
        uint[] imageData = [
            0,  1,  2 ,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
           12, 13, 14, 15 ];

        return new GcBuffer(imageData: NumericHelper.ToBytes(imageData), width: 4, height: 4, pixelFormat: bayerFormat, pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax(bayerFormat), frameID: 42, timeStamp: (ulong)DateTime.Now.Ticks);
    }

    private static Mat GetBayerPatternedMat()
    {
        byte[] imageData = [
            0,  1,  2 ,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
           12, 13, 14, 15 ];

        var mat = new Mat(4, 4, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        mat.SetTo(imageData);
        return mat;
    }

    #endregion
}
