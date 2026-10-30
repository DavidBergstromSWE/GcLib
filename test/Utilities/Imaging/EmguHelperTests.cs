using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class EmguHelperTests
{
    [TestMethod]
    [DataRow(PixelSize.Bpp1, DepthType.Cv8U)]
    [DataRow(PixelSize.Bpp8, DepthType.Cv8U)]
    [DataRow(PixelSize.Bpp10, DepthType.Cv16U)]
    [DataRow(PixelSize.Bpp16, DepthType.Cv16U)]
    [DataRow(PixelSize.Bpp32, DepthType.Cv32S)]
    [DataRow(PixelSize.Bpp64, DepthType.Cv64F)]
    [DataRow(PixelSize.Bpp96, DepthType.Default)]
    public void GetDepthType_ForPixelSize_ReturnsExpectedDepth(PixelSize pixelSize, DepthType expected)
    {
        Assert.AreEqual(expected, EmguHelper.GetDepthType(pixelSize));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8, DepthType.Cv8U)]
    [DataRow(PixelFormat.Mono10p, DepthType.Cv16U)]
    [DataRow(PixelFormat.Mono16, DepthType.Cv16U)]
    [DataRow(PixelFormat.Mono32, DepthType.Cv32S)]
    [DataRow(PixelFormat.RGBa12p, DepthType.Cv16U)]
    public void GetDepthType_ForPixelFormat_ReturnsExpectedDepth(PixelFormat pixelFormat, DepthType expected)
    {
        Assert.AreEqual(expected, EmguHelper.GetDepthType(pixelFormat));
    }

    [TestMethod]
    [DataRow(typeof(byte), DepthType.Cv8U)]
    [DataRow(typeof(sbyte), DepthType.Cv8S)]
    [DataRow(typeof(ushort), DepthType.Cv16U)]
    [DataRow(typeof(short), DepthType.Cv16S)]
    [DataRow(typeof(int), DepthType.Cv32S)]
    [DataRow(typeof(float), DepthType.Cv32F)]
    [DataRow(typeof(double), DepthType.Cv64F)]
    [DataRow(typeof(uint), DepthType.Default)]
    [DataRow(typeof(long), DepthType.Default)]
    public void GetDepthType_ForType_ReturnsExpectedDepth(Type type, DepthType expected)
    {
        Assert.AreEqual(expected, EmguHelper.GetDepthType(type));
    }

    [TestMethod]
    public void GetDepthType_ForNonNumericType_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => EmguHelper.GetDepthType(typeof(string)));

        Assert.AreEqual("Type is not a numeric type!", exception.Message);
    }

    [TestMethod]
    [DataRow(-1, DepthType.Cv8U)]
    [DataRow(8, DepthType.Cv8U)]
    [DataRow(9, DepthType.Cv16U)]
    [DataRow(16, DepthType.Cv16U)]
    [DataRow(17, DepthType.Cv32S)]
    [DataRow(32, DepthType.Cv32S)]
    [DataRow(33, DepthType.Cv64F)]
    [DataRow(64, DepthType.Cv64F)]
    [DataRow(65, DepthType.Default)]
    public void GetDepthType_ForBitDepth_ReturnsExpectedDepth(int bitDepth, DepthType expected)
    {
        Assert.AreEqual(expected, EmguHelper.GetDepthType(bitDepth));
    }

    [TestMethod]
    public void GetBitDepth_ForSupportedDepths_ReturnsExpectedBitCounts()
    {
        (DepthType depthType, int expected)[] cases =
        [
            (DepthType.CvBool, 1),
            (DepthType.Cv8U, 8),
            (DepthType.Cv8S, 8),
            (DepthType.Cv16U, 16),
            (DepthType.Cv16S, 16),
            (DepthType.Cv16F, 16),
            (DepthType.Cv32U, 32),
            (DepthType.Cv32S, 32),
            (DepthType.Cv32F, 32),
            (DepthType.Cv64U, 64),
            (DepthType.Cv64S, 64),
            (DepthType.Cv64F, 64)
        ];

        foreach (var (depthType, expected) in cases)
            Assert.AreEqual(expected, EmguHelper.GetBitDepth(depthType), depthType.ToString());
    }

    [TestMethod]
    public void GetBitDepth_ForUnsupportedDepth_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EmguHelper.GetBitDepth(DepthType.Default));
    }

    [TestMethod]
    public void GetMaxAndGetMin_ForSupportedDepths_ReturnExpectedValues()
    {
        (DepthType depthType, double min, double max)[] cases =
        [
            (DepthType.CvBool, 0, 1),
            (DepthType.Cv8U, byte.MinValue, byte.MaxValue),
            (DepthType.Cv8S, sbyte.MinValue, sbyte.MaxValue),
            (DepthType.Cv16U, ushort.MinValue, ushort.MaxValue),
            (DepthType.Cv16S, short.MinValue, short.MaxValue),
            (DepthType.Cv16F, (double)Half.MinValue, (double)Half.MaxValue),
            (DepthType.Cv32U, uint.MinValue, uint.MaxValue),
            (DepthType.Cv32S, int.MinValue, int.MaxValue),
            (DepthType.Cv32F, float.MinValue, float.MaxValue),
            (DepthType.Cv64U, ulong.MinValue, ulong.MaxValue),
            (DepthType.Cv64S, long.MinValue, long.MaxValue),
            (DepthType.Cv64F, double.MinValue, double.MaxValue)
        ];

        foreach (var (depthType, min, max) in cases)
        {
            Assert.AreEqual(min, EmguHelper.GetMin(depthType), $"Minimum for {depthType}");
            Assert.AreEqual(max, EmguHelper.GetMax(depthType), $"Maximum for {depthType}");
        }
    }

    [TestMethod]
    public void GetMaxAndGetMin_ForUnsupportedDepth_ThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EmguHelper.GetMin(DepthType.Default));
        Assert.Throws<ArgumentException>(() => EmguHelper.GetMax(DepthType.Default));
    }

    [TestMethod]
    [DataRow(DepthType.Cv8U, 1, PixelFormat.Mono8)]
    [DataRow(DepthType.Cv16U, 1, PixelFormat.Mono16)]
    [DataRow(DepthType.Cv8U, 3, PixelFormat.BGR8)]
    [DataRow(DepthType.Cv16U, 3, PixelFormat.BGR16)]
    [DataRow(DepthType.Cv8U, 4, PixelFormat.BGRa8)]
    [DataRow(DepthType.Cv16U, 4, PixelFormat.BGRa16)]
    public void GetPixelFormat_ForSupportedDepthAndChannelCounts_ReturnsExpectedFormat(DepthType depthType, int channels, PixelFormat expected)
    {
        Assert.AreEqual(expected, EmguHelper.GetPixelFormat(depthType, channels));
    }

    [TestMethod]
    [DataRow(DepthType.Cv8S, 1)]
    [DataRow(DepthType.Cv8U, 2)]
    [DataRow(DepthType.Cv16F, 3)]
    [DataRow(DepthType.Cv32F, 4)]
    [DataRow(DepthType.Cv8U, 0)]
    [DataRow(DepthType.Cv8U, 5)]
    public void GetPixelFormat_ForUnsupportedDepthOrChannelCount_ReturnsInvalidFormat(DepthType depthType, int channels)
    {
        Assert.AreEqual(PixelFormat.InvalidPixelFormat, EmguHelper.GetPixelFormat(depthType, channels));
    }

    [TestMethod]
    public void ConvertMat_WhenFormatsAreSame_ReturnsOriginalMatrixWithoutChangingIt()
    {
        using var mat = new Mat(1, 1, DepthType.Cv8U, 1);
        mat.SetTo(new MCvScalar(42));

        var result = EmguHelper.ConvertMat(mat, PixelFormat.Mono8, PixelFormat.Mono8);

        Assert.AreSame(mat, result);
        Assert.AreEqual(DepthType.Cv8U, result.Depth);
        Assert.AreEqual(42, result.GetPixel(0, 0, 0));
    }

    [TestMethod]
    public void ConvertMat_ForGrayscaleFormats_ScalesValuesToTargetDynamicRange()
    {
        using var mat = new Mat(1, 1, DepthType.Cv8U, 1);
        mat.SetTo(new MCvScalar(100));

        var result = EmguHelper.ConvertMat(mat, PixelFormat.Mono8, PixelFormat.Mono16);

        Assert.AreSame(mat, result);
        Assert.AreEqual(DepthType.Cv16U, result.Depth);
        Assert.AreEqual(25700, result.GetPixel(0, 0, 0));
    }

    [TestMethod]
    public void ConvertMat_ForColorOrderConversion_ConvertsBgrToBgraAndScalesAlphaToTargetRange()
    {
        using var mat = new Mat(1, 1, DepthType.Cv8U, 3);
        mat.SetTo(new MCvScalar(10, 20, 30));

        var result = EmguHelper.ConvertMat(mat, PixelFormat.BGR8, PixelFormat.BGRa10);

        Assert.AreSame(mat, result);
        Assert.AreEqual(DepthType.Cv16U, result.Depth);
        Assert.AreEqual(4, result.NumberOfChannels);
        CollectionAssert.AreEqual(new double[] { 40, 80, 120, 1023 }, result.GetPixel(0, 0));
    }

    [TestMethod]
    public void ConvertMat_ForColorOrderSwap_ReversesRedAndBlueChannels()
    {
        using var mat = new Mat(1, 1, DepthType.Cv8U, 3);
        mat.SetTo(new MCvScalar(10, 20, 30));

        var result = EmguHelper.ConvertMat(mat, PixelFormat.BGR8, PixelFormat.RGB8);

        CollectionAssert.AreEqual(new double[] { 30, 20, 10 }, result.GetPixel(0, 0));
    }

    [TestMethod]
    [DataRow(PixelFormat.InvalidPixelFormat, PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono8, PixelFormat.InvalidPixelFormat)]
    [DataRow(PixelFormat.Mono10p, PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono8, PixelFormat.Mono10p)]
    [DataRow(PixelFormat.BayerBG8, PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono8, PixelFormat.BayerBG8)]
    public void ConvertMat_ForUnsupportedFormats_ThrowsNotSupportedException(PixelFormat source, PixelFormat target)
    {
        using var mat = new Mat(1, 1, DepthType.Cv8U, 1);

        Assert.Throws<NotSupportedException>(() => EmguHelper.ConvertMat(mat, source, target));
    }
}
