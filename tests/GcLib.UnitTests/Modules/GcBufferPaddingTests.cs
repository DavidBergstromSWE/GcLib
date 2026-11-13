using System;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcBufferPaddingTests
{
    [TestMethod]
    public void Constructor_RequiresRowAndFramePadding()
    {
        Assert.Throws<ArgumentException>(() => new GcBuffer(new byte[8], 2, 2, PixelFormat.Mono8, 1, 2, 1, 3));
        var buffer = new GcBuffer(new byte[9], 2, 2, PixelFormat.Mono8, 1, 2, 1, 3);
        Assert.AreEqual(3, buffer.GetStride());
        Assert.AreEqual(1u, buffer.PaddingX);
        Assert.AreEqual(3u, buffer.PaddingY);
        Assert.IsTrue(buffer.Equals(new GcBuffer(buffer)));
        Assert.AreEqual(buffer.GetHashCode(), new GcBuffer(buffer).GetHashCode());
        Assert.IsFalse(buffer.Equals(new GcBuffer(new byte[9], 2, 2, PixelFormat.Mono8, 1, 2)));
    }

    [TestMethod]
    public void PixelAccess_InterleavedPadding_PreservesPaddingBytes()
    {
        byte[] data = [1, 2, 3, 90, 4, 5, 6, 91, 92];
        var buffer = new GcBuffer(data, 1, 2, PixelFormat.RGB8, 1, 2, 1, 1);
        Assert.AreSequenceEqual([4, 5, 6], buffer.GetPixel(1, 0));
        Assert.AreEqual(5d, buffer.GetPixel(1, 0, 1));
        buffer.SetPixel(1, 0, [7, 8, 9]);
        buffer.SetPixel(0, 0, 1, 10);
        Assert.AreSequenceEqual(new byte[] { 1, 10, 3, 90, 7, 8, 9, 91, 92 }, data);
    }

    [TestMethod]
    public void PackedRows_WithPadding_AccessAndUnpackCorrectly()
    {
        byte[] data = [1, 0, 90, 2, 0, 91, 92];
        Assert.Throws<ArgumentException>(() => new GcBuffer(new byte[6], 1, 2, PixelFormat.Mono10p, 1, 2, 1, 1));
        var buffer = new GcBuffer(data, 1, 2, PixelFormat.Mono10p, 1, 2, 1, 1);
        Assert.AreEqual(3, buffer.GetStride());
        Assert.AreEqual(1d, buffer.GetPixel(0, 0, 0));
        Assert.AreEqual(2d, buffer.GetPixel(1, 0, 0));
        buffer.SetPixel(1, 0, 0, 3);
        Assert.AreSequenceEqual(new byte[] { 1, 0, 90, 3, 0, 91, 92 }, data);
        var unpacked = buffer.Unpack();
        Assert.AreSequenceEqual(new byte[] { 1, 0, 3, 0 }, unpacked.ImageData);
        Assert.AreEqual(0u, unpacked.PaddingX);
        Assert.AreEqual(0u, unpacked.PaddingY);
    }

    [TestMethod]
    public void PackedColorRows_WithPadding_UnpackEachChannel()
    {
        byte[] data = [0, 0, 0, 0, 90, 0, 0, 0, 0, 91, 92];
        var buffer = new GcBuffer(data, 1, 2, PixelFormat.RGB10p, 1, 2, 1, 1);
        buffer.SetPixel(0, 0, [1, 2, 3]);
        buffer.SetPixel(1, 0, [4, 5, 6]);
        var unpacked = buffer.Unpack();
        Assert.AreSequenceEqual([1, 2, 3], unpacked.GetPixel(0, 0));
        Assert.AreSequenceEqual([4, 5, 6], unpacked.GetPixel(1, 0));
        Assert.AreEqual((byte)90, data[4]);
        Assert.AreEqual((byte)91, data[9]);
        Assert.AreEqual((byte)92, data[10]);
    }

    [TestMethod]
    public void FramePadding_IsExcludedFromConversions()
    {
        var buffer = new GcBuffer([1, 0, 2, 0, 90, 91], 1, 2, PixelFormat.Mono10, 1, 2, 0, 2);
        Assert.AreSequenceEqual(new byte[] { 1, 8, 0 }, buffer.Pack().ImageData);
        using var mat = buffer.ToMat(false);
        Assert.AreEqual(2d, mat.GetPixel(1, 0, 0));
    }

    [TestMethod]
    public void Pack_PaddedRows_SkipsPadding()
    {
        var buffer = new GcBuffer([1, 0, 90, 2, 0, 91, 92], 1, 2, PixelFormat.Mono10, 1, 2, 1, 1);
        var packed = buffer.Pack();
        Assert.AreSequenceEqual(new byte[] { 1, 8, 0 }, packed.ImageData);
        Assert.AreEqual(2d, packed.GetPixel(1, 0, 0));
    }

    [TestMethod]
    public void Histogram_PaddedInterleavedAndPlanarRows_IgnorePadding()
    {
        var interleaved = new GcBuffer([1, 2, 90, 3, 4, 91, 92], 2, 2, PixelFormat.Mono8, 1, 2, 1, 1);
        double[,] mono = null;
        interleaved.CalculateHistogram(256, ref mono, false);
        Assert.AreEqual(1d, mono[0, 1]);
        Assert.AreEqual(1d, mono[0, 4]);
        Assert.AreEqual(0d, mono[0, 90]);

        var planar = new GcBuffer([1, 90, 2, 91, 3, 92, 4, 93, 5, 94, 6, 95, 96], 1, 2, PixelFormat.RGB8_Planar, 1, 2, 1, 1);
        Assert.AreSequenceEqual([2, 4, 6], planar.GetPixel(1, 0));
        planar.SetPixel(1, 0, [7, 8, 9]);
        double[,] color = null;
        planar.CalculateHistogram(256, ref color, false);
        Assert.AreEqual(1d, color[0, 9]);
        Assert.AreEqual(1d, color[1, 8]);
        Assert.AreEqual(1d, color[2, 7]);
        Assert.AreEqual(0d, color[0, 95]);
        using var mat = planar.ToMat(false);
        Assert.AreEqual(7d, mat.GetPixel(1, 0, 0));
        Assert.AreEqual(8d, mat.GetPixel(1, 0, 1));
        Assert.AreEqual(9d, mat.GetPixel(1, 0, 2));
        var fromMat = new GcBuffer(mat, PixelFormat.RGB8_Planar, 1, 2);
        Assert.AreSequenceEqual(new byte[] { 1, 7, 3, 8, 5, 9 }, fromMat.ImageData);
        Assert.AreSequenceEqual([7, 8, 9], fromMat.GetPixel(1, 0));
    }

    [TestMethod]
    public void ToMatAndDeBayer_PaddedRows_MatchTightImages()
    {
        var padded = new GcBuffer([1, 2, 90, 3, 4, 91, 92], 2, 2, PixelFormat.Mono8, 1, 2, 1, 1);
        using var mat = padded.ToMat();
        Assert.AreEqual(3d, mat.GetPixel(1, 0, 0));
        using var rawMat = padded.ToMat(false);
        Assert.AreEqual(4d, rawMat.GetPixel(1, 1, 0));

        var bayerPadded = new GcBuffer([1, 2, 90, 3, 4, 91, 92], 2, 2, PixelFormat.BayerRG8, 1, 2, 1, 1);
        var bayerTight = new GcBuffer([1, 2, 3, 4], 2, 2, PixelFormat.BayerRG8, 1, 2);
        Assert.AreSequenceEqual(bayerTight.DeBayer().ImageData, bayerPadded.DeBayer().ImageData);
    }
}
