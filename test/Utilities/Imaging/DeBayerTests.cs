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
        var rawMat = GetBayerPatternedMat(pixelFormat);

        // Act
        var rgbMat = DeBayer.Transform2BGR(rawMat, pixelFormat);

        // Assert
        Assert.IsNotNull(rgbMat);
        Assert.AreEqual(rawMat.Width, rgbMat.Width);
        Assert.AreEqual(rawMat.Height, rgbMat.Height);
        Assert.AreEqual(rawMat.Depth, rgbMat.Depth);
        Assert.AreEqual(3, rgbMat.NumberOfChannels);
    }

    [TestMethod]
    public void Transform2BGR_MatInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var rawMat = new Mat(100, 100, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        var unsupportedFormat = PixelFormat.Mono8;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.Transform2BGR(rawMat, unsupportedFormat));
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
        var rawMat = GetBayerPatternedMat(pixelFormat);

        // Act
        var rgbMat = DeBayer.Transform2RGB(rawMat, pixelFormat);

        // Assert
        Assert.IsNotNull(rgbMat);
        Assert.AreEqual(rawMat.Width, rgbMat.Width);
        Assert.AreEqual(rawMat.Height, rgbMat.Height);
        Assert.AreEqual(rawMat.Depth, rgbMat.Depth);
        Assert.AreEqual(3, rgbMat.NumberOfChannels);
    }

    [TestMethod]
    public void Transform2RGB_MatInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var rawMat = new Mat(100, 100, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        var unsupportedFormat = PixelFormat.Mono8;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.Transform2RGB(rawMat, unsupportedFormat));
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
        var bgrBuffer = DeBayer.Transform2BGR(rawBuffer);

        // Assert
        Assert.IsNotNull(bgrBuffer);
        Assert.AreEqual(rawBuffer.Width, bgrBuffer.Width);
        Assert.AreEqual(rawBuffer.Height, bgrBuffer.Height);
        Assert.AreEqual(rawBuffer.BitDepth, bgrBuffer.BitDepth);
        Assert.AreEqual(rawBuffer.PixelDynamicRangeMax, bgrBuffer.PixelDynamicRangeMax);
        Assert.AreEqual(3, (int)bgrBuffer.NumChannels);
    }

    [TestMethod]
    public void Transform2BGR_GcBufferInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer(10, 10, PixelFormat.Mono8);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.Transform2BGR(buffer));
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
        var rgbBuffer = DeBayer.Transform2RGB(rawBuffer);

        // Assert
        Assert.IsNotNull(rgbBuffer);
        Assert.AreEqual(rawBuffer.Width, rgbBuffer.Width);
        Assert.AreEqual(rawBuffer.Height, rgbBuffer.Height);
        Assert.AreEqual(rawBuffer.BitDepth, rgbBuffer.BitDepth);
        Assert.AreEqual(rawBuffer.PixelDynamicRangeMax, rgbBuffer.PixelDynamicRangeMax);
        Assert.AreEqual(3, (int)rgbBuffer.NumChannels);
    }

    [TestMethod]
    public void Transform2RGB_GcBufferInputWithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer(10, 10, PixelFormat.Mono8);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DeBayer.Transform2RGB(buffer));
    }

    #region Private Methods

    private static GcBuffer GetBayerPatternedBuffer(PixelFormat bayerFormat)
    {
        uint[] imageData = [
            0,  1,  2 ,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
           12, 13, 14, 15
        ];

        return new GcBuffer(imageData: NumericHelper.ToBytes(imageData), width: 4, height: 4, pixelFormat: bayerFormat, pixelDynamicRangeMax: GenICamConverter.GetDynamicRangeMax(bayerFormat), frameID: 0, timeStamp: 0);
    }

    private static Mat GetBayerPatternedMat(PixelFormat bayerFormat)
    {
        byte[] imageData = [
            0,  1,  2 ,  3,
            4,  5,  6,  7,
            8,  9, 10, 11,
           12, 13, 14, 15
        ];
        var mat = new Mat(4, 4, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        mat.SetTo(imageData);
        return mat;
    }

    #endregion
}
