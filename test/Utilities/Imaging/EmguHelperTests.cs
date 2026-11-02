using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;
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

    [TestMethod]
    [DataRow(10, 1)]
    [DataRow(12, 1)]
    [DataRow(14, 1)]
    [DataRow(10, 3)]
    [DataRow(12, 4)]
    [DataRow(1, 1)]
    [DataRow(2, 1)]
    [DataRow(4, 1)]
    public void UnpackPacked_ReturnsValuesForDepthAndChannelCount(int bitDepth, int channels)
    {
        // Arrange
        const int width = 2;
        const int height = 2;
        int samplesPerRow = width * channels;
        int rowBytes = (samplesPerRow * bitDepth + 7) / 8;
        ushort maxValue = (ushort)((1 << bitDepth) - 1);
        ushort[] expectedValues = new ushort[width * height * channels];
        byte[] packedData = new byte[rowBytes * height];

        for (int i = 0; i < expectedValues.Length; i++)
        {
            expectedValues[i] = i % 2 == 0 ? (ushort)Math.Min(i + 1, maxValue) : maxValue;
            int row = i / samplesPerRow;
            int sampleInRow = i % samplesPerRow;
            int bitOffset = sampleInRow * bitDepth;

            for (int bit = 0; bit < bitDepth; bit++)
            {
                if ((expectedValues[i] & (1 << bit)) != 0)
                    packedData[row * rowBytes + (bitOffset + bit) / 8] |= (byte)(1 << ((bitOffset + bit) % 8));
            }
        }

        // Act
        using var result = EmguHelper.Unpack(packedData, width, height, bitDepth, channels, ByteExtensions.Endianness.LittleEndian);

        // Assert
        Assert.AreEqual(width, result.Cols);
        Assert.AreEqual(height, result.Rows);
        Assert.AreEqual(channels, result.NumberOfChannels);
        for (int i = 0; i < expectedValues.Length; i++)
        {
            int row = i / samplesPerRow;
            int sampleInRow = i % samplesPerRow;
            Assert.AreEqual(expectedValues[i], result.GetPixel(row, sampleInRow / channels, (uint)(sampleInRow % channels)), $"Sample {i}");
        }
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(2, 1)]
    [DataRow(4, 1)]
    [DataRow(10, 1)]
    [DataRow(12, 3)]
    [DataRow(14, 4)]
    public void Pack_RoundTripsPackedValues(int bitDepth, int channels)
    {
        const int width = 2;
        const int height = 2;
        ushort maxValue = (ushort)((1 << bitDepth) - 1);
        var expectedValues = new ushort[channels];
        for (int channel = 0; channel < channels; channel++)
            expectedValues[channel] = channel % 2 == 0 ? (ushort)(channel + 1) : maxValue;

        using var mat = new Mat(height, width, DepthType.Cv16U, channels);
        mat.SetTo(new MCvScalar(expectedValues.Length > 0 ? expectedValues[0] : 0,
                                expectedValues.Length > 1 ? expectedValues[1] : 0,
                                expectedValues.Length > 2 ? expectedValues[2] : 0,
                                expectedValues.Length > 3 ? expectedValues[3] : 0));

        byte[] packedData = EmguHelper.Pack(mat, bitDepth);
        using var result = EmguHelper.Unpack(packedData, width, height, bitDepth, channels);

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                for (int channel = 0; channel < channels; channel++)
                    Assert.AreEqual(expectedValues[channel], result.GetPixel(row, col, (uint)channel), $"Pixel ({row}, {col}), channel {channel}");
            }
        }
    }

    [TestMethod]
    public void Pack_UsesByteOrderAcrossRowsAndPreservesSource()
    {
        // Arrange
        using var mat = new Mat(2, 1, DepthType.Cv16U, 1);
        using (var firstRow = mat.Row(0))
            firstRow.SetTo(new MCvScalar(0x155));
        using (var secondRow = mat.Row(1))
            secondRow.SetTo(new MCvScalar(0x2AA));

        // Act
        byte[] bigEndian = EmguHelper.Pack(mat, 10, ByteExtensions.Endianness.BigEndian);
        byte[] littleEndian = EmguHelper.Pack(mat, 10, ByteExtensions.Endianness.LittleEndian);
        byte[] defaultEndian = EmguHelper.Pack(mat, 10);

        // Assert
        CollectionAssert.AreEqual(new byte[] { 0x02, 0xAA, 0x01, 0x55 }, bigEndian);
        CollectionAssert.AreEqual(new byte[] { 0x55, 0x01, 0xAA, 0x02 }, littleEndian);
        CollectionAssert.AreEqual(littleEndian, defaultEndian);
        Assert.AreEqual(0x155, mat.GetPixel(0, 0, 0));
        Assert.AreEqual(0x2AA, mat.GetPixel(1, 0, 0));
    }

    [TestMethod]
    public void Pack_ForCv8UWithNonContiguousRows_ReturnsExactBytesAndPreservesSource()
    {
        using var source = new Mat(2, 3, DepthType.Cv8U, 3);
        source.SetTo(new MCvScalar(15, 15, 15));
        using var roi = new Mat(source, new System.Drawing.Rectangle(1, 0, 1, 2));
        using (var firstRow = roi.Row(0))
            firstRow.SetTo(new MCvScalar(1, 2, 3));
        using (var secondRow = roi.Row(1))
            secondRow.SetTo(new MCvScalar(4, 5, 6));

        Assert.IsTrue(roi.Step > roi.Cols * roi.NumberOfChannels);
        CollectionAssert.AreEqual(new byte[] { 0x06, 0x54, 0x03, 0x21 }, EmguHelper.Pack(roi, 4, ByteExtensions.Endianness.BigEndian));
        CollectionAssert.AreEqual(new byte[] { 0x21, 0x03, 0x54, 0x06 }, EmguHelper.Pack(roi, 4));
        CollectionAssert.AreEqual(new double[] { 1, 2, 3 }, source.GetPixel(0, 1));
        CollectionAssert.AreEqual(new double[] { 4, 5, 6 }, source.GetPixel(1, 1));
        for (int row = 0; row < source.Rows; row++)
        {
            CollectionAssert.AreEqual(new double[] { 15, 15, 15 }, source.GetPixel(row, 0));
            CollectionAssert.AreEqual(new double[] { 15, 15, 15 }, source.GetPixel(row, 2));
        }
    }

    [TestMethod]
    public void Unpack_UsesByteOrderAcrossRowsAndPreservesInput()
    {
        // Arrange
        byte[] bigEndian = [0x02, 0xAA, 0x01, 0x55];
        byte[] littleEndian = [0x55, 0x01, 0xAA, 0x02];
        byte[] originalLittleEndian = (byte[])littleEndian.Clone();

        // Act
        using var bigEndianResult = EmguHelper.Unpack(bigEndian, 1, 2, 10, 1, ByteExtensions.Endianness.BigEndian);
        using var littleEndianResult = EmguHelper.Unpack(littleEndian, 1, 2, 10, 1, ByteExtensions.Endianness.LittleEndian);

        // Assert
        Assert.AreEqual(0x155, bigEndianResult.GetPixel(0, 0, 0));
        Assert.AreEqual(0x2AA, bigEndianResult.GetPixel(1, 0, 0));
        Assert.AreEqual(0x155, littleEndianResult.GetPixel(0, 0, 0));
        Assert.AreEqual(0x2AA, littleEndianResult.GetPixel(1, 0, 0));
        CollectionAssert.AreEqual(new byte[] { 0x02, 0xAA, 0x01, 0x55 }, bigEndian);
        CollectionAssert.AreEqual(originalLittleEndian, littleEndian);
    }

    [TestMethod]
    public void PackAndUnpack_ForUnsupportedEndianness_ThrowArgumentOutOfRangeException()
    {
        // Arrange
        using var mat = new Mat(1, 1, DepthType.Cv16U, 1);
        mat.SetTo(new MCvScalar(1));
        var unsupportedEndianness = (ByteExtensions.Endianness)123;

        // Act
        Action packAction = () => EmguHelper.Pack(mat, 10, unsupportedEndianness);
        Action unpackAction = () => EmguHelper.Unpack(new byte[] { 1, 0 }, 1, 1, 10, 1, unsupportedEndianness);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(packAction);
        Assert.Throws<ArgumentOutOfRangeException>(unpackAction);
    }

    [TestMethod]
    public void Pack_ForSampleOutsideBitDepth_ThrowsArgumentException()
    {
        using var mat = new Mat(1, 1, DepthType.Cv16U, 1);
        mat.SetTo(new MCvScalar(1024));

        Assert.Throws<ArgumentException>(() => EmguHelper.Pack(mat, 10));
    }

    [TestMethod]
    public void UnpackPacked_ForShortInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EmguHelper.Unpack(new byte[1], 2, 1, 12, 1));
    }
}
