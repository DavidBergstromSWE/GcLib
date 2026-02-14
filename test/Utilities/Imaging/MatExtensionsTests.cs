using System;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class MatExtensionsTests
{
    [TestMethod]
    [DataRow(DepthType.Cv8U)]
    [DataRow(DepthType.Cv16U)]
    public void GetPixel_ForSpecificChannel_ValidatePixelValues(DepthType depthType)
    {
        // Arrange
        double expected = EmguHelper.GetMax(depthType);
        var mat = Mat.Zeros(3, 3, depthType, 3) + expected;

        // Act/Assert
        Assert.AreEqual(expected, mat.GetPixel(0, 0, 0));
        Assert.AreEqual(expected, mat.GetPixel(0, 0, 1));
        Assert.AreEqual(expected, mat.GetPixel(0, 0, 2));
        Assert.AreEqual(expected, mat.GetPixel(0, 1, 0));
        Assert.AreEqual(expected, mat.GetPixel(0, 1, 1));
        Assert.AreEqual(expected, mat.GetPixel(0, 1, 2));
        Assert.AreEqual(expected, mat.GetPixel(0, 2, 0));
        Assert.AreEqual(expected, mat.GetPixel(0, 2, 1));
        Assert.AreEqual(expected, mat.GetPixel(0, 2, 2));
        Assert.AreEqual(expected, mat.GetPixel(1, 0, 0));
        Assert.AreEqual(expected, mat.GetPixel(1, 0, 1));
        Assert.AreEqual(expected, mat.GetPixel(1, 0, 2));
        Assert.AreEqual(expected, mat.GetPixel(1, 1, 0));
        Assert.AreEqual(expected, mat.GetPixel(1, 1, 1));
        Assert.AreEqual(expected, mat.GetPixel(1, 1, 2));
        Assert.AreEqual(expected, mat.GetPixel(1, 2, 0));
        Assert.AreEqual(expected, mat.GetPixel(1, 2, 1));
        Assert.AreEqual(expected, mat.GetPixel(1, 2, 2));
        Assert.AreEqual(expected, mat.GetPixel(2, 0, 0));
        Assert.AreEqual(expected, mat.GetPixel(2, 0, 1));
        Assert.AreEqual(expected, mat.GetPixel(2, 0, 2));
        Assert.AreEqual(expected, mat.GetPixel(2, 1, 0));
        Assert.AreEqual(expected, mat.GetPixel(2, 1, 1));
        Assert.AreEqual(expected, mat.GetPixel(2, 1, 2));
        Assert.AreEqual(expected, mat.GetPixel(2, 2, 0));
        Assert.AreEqual(expected, mat.GetPixel(2, 2, 1));
        Assert.AreEqual(expected, mat.GetPixel(2, 2, 2));
    }

    [TestMethod]
    [DataRow(DepthType.Cv8U)]
    [DataRow(DepthType.Cv16U)]
    public void GetPixel_ForAllChannels_ValidatePixelValues(DepthType depthType)
    {
        // Arrange
        double[] expected = [EmguHelper.GetMax(depthType), EmguHelper.GetMax(depthType), EmguHelper.GetMax(depthType)];
        var mat = Mat.Zeros(3, 3, depthType, 3) + expected[0];

        // Act/Assert
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(0, 0)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(0, 1)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(0, 2)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(1, 0)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(1, 1)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(1, 2)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(2, 0)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(2, 1)));
        Assert.IsTrue(Enumerable.SequenceEqual(expected, mat.GetPixel(2, 2)));
    }

    [TestMethod]
    public void GetPixel_RowOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 3);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.GetPixel(3, 2));
        Assert.AreEqual("row", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.GetPixel(3, 2, 0));
        Assert.AreEqual("row", ex.ParamName);
    }

    [TestMethod]
    public void GetPixel_ColOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 3);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.GetPixel(2, 3));
        Assert.AreEqual("col", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.GetPixel(2, 3, 0));
        Assert.AreEqual("col", ex.ParamName);
    }

    [TestMethod]
    public void GetPixel_ChannelOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 1);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.GetPixel(2, 2, 1));
        Assert.AreEqual("channel", ex.ParamName);
    }

    [TestMethod]
    [DataRow(DepthType.Cv8U)]
    [DataRow(DepthType.Cv16U)]
    public void SetPixel_ForSpecificChannel_ValidateChangedPixelValues(DepthType depthType)
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, depthType, 3);

        // Act
        mat.SetPixel(0, 0, 0, EmguHelper.GetMax(depthType));
        mat.SetPixel(1, 2, 1, EmguHelper.GetMax(depthType) - 33);
        mat.SetPixel(2, 0, 2, EmguHelper.GetMax(depthType) - 42);

        // Assert
        Assert.AreEqual(mat.GetPixel(0, 0, 0), EmguHelper.GetMax(depthType));
        Assert.AreEqual(mat.GetPixel(1, 2, 1), EmguHelper.GetMax(depthType) - 33);
        Assert.AreEqual(mat.GetPixel(2, 0, 2), EmguHelper.GetMax(depthType) - 42);
    }

    [TestMethod]
    [DataRow(DepthType.Cv8U)]
    [DataRow(DepthType.Cv16U)]
    public void SetPixel_ForAllChannels_ValidateChangedPixelValues(DepthType depthType)
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, depthType, 3);

        // Act
        mat.SetPixel(0, 0, [EmguHelper.GetMax(depthType), EmguHelper.GetMax(depthType) - 1, EmguHelper.GetMax(depthType) - 4]);
        mat.SetPixel(1, 2, [EmguHelper.GetMax(depthType) - 33, EmguHelper.GetMax(depthType) - 11, EmguHelper.GetMax(depthType) - 67]);
        mat.SetPixel(2, 0, [EmguHelper.GetMax(depthType) - 42, EmguHelper.GetMax(depthType) - 111, EmguHelper.GetMax(depthType) - 74]);

        // Assert
        Assert.IsTrue(Enumerable.SequenceEqual(mat.GetPixel(0, 0), [EmguHelper.GetMax(depthType), EmguHelper.GetMax(depthType) - 1, EmguHelper.GetMax(depthType) - 4]));
        Assert.IsTrue(Enumerable.SequenceEqual(mat.GetPixel(1, 2), [EmguHelper.GetMax(depthType) - 33, EmguHelper.GetMax(depthType) - 11, EmguHelper.GetMax(depthType) - 67]));
        Assert.IsTrue(Enumerable.SequenceEqual(mat.GetPixel(2, 0), [EmguHelper.GetMax(depthType) - 42, EmguHelper.GetMax(depthType) - 111, EmguHelper.GetMax(depthType) - 74]));
    }

    [TestMethod]
    public void SetPixel_RowOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 3);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.SetPixel(3, 2, [0, 0, 0]));
        Assert.AreEqual("row", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.SetPixel(3, 2, 0, 0));
        Assert.AreEqual("row", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_ColOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 3);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.SetPixel(2, 3, [0, 0, 0]));
        Assert.AreEqual("col", ex.ParamName);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.SetPixel(2, 3, 0, 0));
        Assert.AreEqual("col", ex.ParamName);
    }

    [TestMethod]
    public void SetPixel_ChannelOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 1);

        // Act/Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => mat.SetPixel(2, 2, 1, 0));
        Assert.AreEqual("channel", ex.ParamName);
    }

    [TestMethod]
    public void CalculateHistogram_NoPreAllocation_HistogramIsNotNull()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 3) + 33;
        double[,] histogramData = null;

        // Act
        mat.CalculateHistogram(32, 256, ref histogramData);

        //Assert
        Assert.IsNotNull(histogramData);
    }

    [TestMethod]
    public void CalculateHistogram_PreAllocation_HistogramIsNotNull()
    {
        // Arrange
        var mat = Mat.Zeros(3, 3, DepthType.Cv8U, 3) + 33;
        double[,] histogramData = new double[mat.NumberOfChannels, 32];

        // Act
        mat.CalculateHistogram(32, 256, ref histogramData);

        //Assert
        Assert.IsNotNull(histogramData);
    }

    [TestMethod]
    [DataRow(1, DepthType.Cv8U)]
    [DataRow(1, DepthType.Cv16U)]
    [DataRow(3, DepthType.Cv8U)]
    [DataRow(3, DepthType.Cv16U)]
    public void DrawCenteredText_MatIsChanged(int numChannels, DepthType depthType)
    {
        // Arrange
        var originalMat = Mat.Zeros(10, 10, depthType, numChannels);
        var newMat = originalMat.Clone();

        // Act
        newMat.DrawCenteredText("Test", (int)EmguHelper.GetMax(depthType));

        // Assert
        Assert.IsFalse(newMat.Equals(originalMat));
    }
}