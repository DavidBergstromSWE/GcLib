using System;
using System.Numerics;
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
        byte r = 200, g = 100, b = 50;
        var rawMat = GetBayerPatternedMat(pixelColorFilter: GenICamHelper.GetPixelColorFilter(pixelFormat), r: r, g: g, b: b);

        // Act
        var outputMat = DeBayer.ToBGR(rawMat, pixelFormat);

        // Assert
        Assert.IsNotNull(outputMat);
        Assert.AreEqual(rawMat.Width, outputMat.Width);
        Assert.AreEqual(rawMat.Height, outputMat.Height);
        Assert.AreEqual(rawMat.Depth, outputMat.Depth);
        Assert.AreEqual(3, outputMat.NumberOfChannels);

        for (int row = 0; row < outputMat.Rows; row++)
        {
            for (int col = 0; col < outputMat.Cols; col++)
            {
                Assert.AreEqual(actual: outputMat.GetPixel(row, col, 0), expected: b);  // Blue channel
                Assert.AreEqual(actual: outputMat.GetPixel(row, col, 1), expected: g);  // Green
                Assert.AreEqual(actual: outputMat.GetPixel(row, col, 2), expected: r);  // Red
            }
        }
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
        byte r = 200, g = 100, b = 50;
        var rawMat = GetBayerPatternedMat(pixelColorFilter: GenICamHelper.GetPixelColorFilter(pixelFormat), r: r, g: g, b: b);

        // Act
        var outputMat = DeBayer.ToRGB(rawMat, pixelFormat);

        // Assert
        Assert.IsNotNull(outputMat);
        Assert.AreEqual(rawMat.Width, outputMat.Width);
        Assert.AreEqual(rawMat.Height, outputMat.Height);
        Assert.AreEqual(rawMat.Depth, outputMat.Depth);
        Assert.AreEqual(3, outputMat.NumberOfChannels);

        for (int row = 0; row < outputMat.Rows; row++)
        {
            for (int col = 0; col < outputMat.Cols; col++)
            {
                Assert.AreEqual(actual: outputMat.GetPixel(row, col, 0), expected: r);  // Red channel
                Assert.AreEqual(actual: outputMat.GetPixel(row, col, 1), expected: g);  // Green
                Assert.AreEqual(actual: outputMat.GetPixel(row, col, 2), expected: b);  // Blue
            }
        }
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
        byte r = 200, g = 100, b = 50;
        var rawBuffer = GetBayerPatternedBuffer(pixelFormat: pixelFormat, r: r, g: g, b: b);

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

        for (uint row = 0; row < outputBuffer.Height; row++)
        {
            for (uint col = 0; col < outputBuffer.Width; col++)
            {
                Assert.AreEqual(actual: outputBuffer.GetPixel(row, col, 0), expected: b); // Blue channel
                Assert.AreEqual(actual: outputBuffer.GetPixel(row, col, 1), expected: g); // Green
                Assert.AreEqual(actual: outputBuffer.GetPixel(row, col, 2), expected: r); // Red
            }
        }
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
        byte r = 200, g = 100, b = 50;
        var rawBuffer = GetBayerPatternedBuffer(pixelFormat: pixelFormat, r: r, g: g, b: b);

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

        for (uint row = 0; row < outputBuffer.Height; row++)
        {
            for (uint col = 0; col < outputBuffer.Width; col++)
            {
                Assert.AreEqual(actual: outputBuffer.GetPixel(row, col, 0), expected: r); // Red channel
                Assert.AreEqual(actual: outputBuffer.GetPixel(row, col, 1), expected: g); // Green
                Assert.AreEqual(actual: outputBuffer.GetPixel(row, col, 2), expected: b);  // Blue
            }
        }
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

    /// <summary>
    /// Retrieves an image buffer in Bayer pattern as specified by <paramref name="pixelFormat"/>,
    /// where all red-filtered pixels are given the value <paramref name="r"/>, all green-filtered pixels are given the value <paramref name="g"/> 
    /// and all blue-filtered pixels are given the value <paramref name="b"/>.
    /// </summary>
    /// <param name="pixelFormat">Pixel format, as defined in GenICam PFNC.</param>
    /// <param name="r">Red channel value.</param>
    /// <param name="g">Green channel value.</param>
    /// <param name="b">Blue channel value.</param>
    /// <returns>Image stored as s <see cref="GcBuffer"/>.</returns>
    private static GcBuffer GetBayerPatternedBuffer(PixelFormat pixelFormat, byte r, byte g, byte b)
    {
        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var imageData = GetBayerPatternedImageData(GenICamHelper.GetPixelColorFilter(pixelFormat), r, g, b);
            return new GcBuffer(imageData: imageData, width: 4, height: 4, pixelFormat: pixelFormat, pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), frameID: 42, timeStamp: (ulong)DateTime.Now.Ticks);
        }
        else
        {
            var imageData = GetBayerPatternedImageData<ushort>(GenICamHelper.GetPixelColorFilter(pixelFormat), r, g, b);
            return new GcBuffer(imageData: NumericHelper.ToBytes(imageData), width: 4, height: 4, pixelFormat: pixelFormat, pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), frameID: 42, timeStamp: (ulong)DateTime.Now.Ticks);
        }
    }

    /// <summary>
    /// Retrieves a <see cref="Mat"/> image in a Bayer pattern as specified by parameter <paramref name="pixelColorFilter"/>,
    /// where all red-filtered pixels are given the value <paramref name="r"/>, all green-filtered pixels are given the value <paramref name="g"/> 
    /// and all blue-filtered pixels are given the value <paramref name="b"/>.
    /// </summary>
    /// <param name="pixelColorFilter">Bayer pixel color filter.</param>
    /// <param name="r">Red channel value.</param>
    /// <param name="g">Green channel value.</param>
    /// <param name="b">Blue channel value.</param>
    /// <returns><see cref="Mat"/> image.</returns>
    private static Mat GetBayerPatternedMat(PixelColorFilter pixelColorFilter, byte r, byte g, byte b)
    {
        byte[] imageData = GetBayerPatternedImageData(pixelColorFilter, r, g, b);

        var mat = new Mat(4, 4, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
        mat.SetTo(imageData);
        return mat;
    }

    /// <summary>
    /// Retrieves image data in a Bayer pattern as specified by parameter <paramref name="pixelColorFilter"/>, 
    /// where all red-filtered pixels are given the value <paramref name="r"/>, all green-filtered pixels are given the value <paramref name="g"/> 
    /// and all blue-filtered pixels are given the value <paramref name="b"/>.
    /// </summary>
    /// <typeparam name="T">Numeric type.</typeparam>
    /// <param name="pixelColorFilter">Bayer pixel color filter.</param>
    /// <param name="r">Red channel value.</param>
    /// <param name="g">Green channel value.</param>
    /// <param name="b">Blue channel value.</param>
    /// <returns>Image data.</returns>
    private static T[] GetBayerPatternedImageData<T>(PixelColorFilter pixelColorFilter, T r, T g, T b) where T : INumber<T>
    {
        T[] imageData = new T[16];

        switch (pixelColorFilter)
        {
            case PixelColorFilter.BayerRGGB:
                imageData = [ r, g, r, g,
                              g, b, g, b,
                              r, g, r, g,
                              g, b, g, b ];
                break;
            case PixelColorFilter.BayerGBRG:
                imageData = [ g, b, g, b,
                              r, g, r, g,
                              g, b, g, b,
                              r, g, r, g ];
                break;
            case PixelColorFilter.BayerGRBG:
                imageData = [ g, r, g, r,
                              b, g, b, g,
                              g, r, g, r,
                              b, g, b, g ];
                break;
            case PixelColorFilter.BayerBGGR:
                imageData = [ b, g, b, g,
                              g, r, g, r,
                              b, g, b, g,
                              g, r, g, r ];
                break;
            default:
                break;
        }

        return imageData;
    }

    #endregion
}
