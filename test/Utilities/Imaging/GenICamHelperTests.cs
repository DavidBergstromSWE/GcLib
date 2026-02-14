using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GenICamHelperTests
{
    [TestMethod]
    public void GetBitsPerPixel_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixel(PixelFormat.Mono8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixel(PixelFormat.Mono10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixel(PixelFormat.Mono16));

        Assert.AreEqual<uint>(24, GenICamHelper.GetBitsPerPixel(PixelFormat.BGR8));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.BGR10));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.BGR16));

        Assert.AreEqual<uint>(24, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB8));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB10));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB16));

        Assert.AreEqual<uint>(32, GenICamHelper.GetBitsPerPixel(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(64, GenICamHelper.GetBitsPerPixel(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(64, GenICamHelper.GetBitsPerPixel(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(32, GenICamHelper.GetBitsPerPixel(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(64, GenICamHelper.GetBitsPerPixel(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(64, GenICamHelper.GetBitsPerPixel(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixel(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixel(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(14, GenICamHelper.GetBitsPerPixel(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(30, GenICamHelper.GetBitsPerPixel(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(36, GenICamHelper.GetBitsPerPixel(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(30, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(36, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixel(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(40, GenICamHelper.GetBitsPerPixel(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(40, GenICamHelper.GetBitsPerPixel(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(24, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(48, GenICamHelper.GetBitsPerPixel(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetBitsPerPixelPerChannel_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(14, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(10, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(12, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(8, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(16, GenICamHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetPixelSize_ReturnsCorrectValues()
    {
        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.Mono8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.Mono10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.Mono16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.BGR8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BGR10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.BGR16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.RGB8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.RGB10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.RGB16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.BGRa8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BGRa10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.BGRa16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.RGBa8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.RGBa10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.RGBa16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.BayerBG8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BayerBG10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.BayerBG16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.BayerGR8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BayerGR10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamHelper.GetPixelSize(PixelFormat.BayerGR16));

        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.Mono10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.Mono12p));
        Assert.AreEqual(PixelSize.Bpp14, GenICamHelper.GetPixelSize(PixelFormat.Mono14p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BGR10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.BGR12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.RGB10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.RGB12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BayerGB10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.BayerGB12p));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BayerGR10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.BayerGR12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.RGBa10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.RGBa12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.BGRa10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.BGRa12p));

        Assert.AreEqual(PixelSize.Bpp8, GenICamHelper.GetPixelSize(PixelFormat.RGB8_Planar));
        Assert.AreEqual(PixelSize.Bpp10, GenICamHelper.GetPixelSize(PixelFormat.RGB10_Planar));
        Assert.AreEqual(PixelSize.Bpp12, GenICamHelper.GetPixelSize(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetDynamicRangeMax_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono16));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGR8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGR10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGR16));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB16));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(65535, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(16383, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(255, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(1023, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(4095, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetNumChannels_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.Mono8));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.Mono10));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.Mono16));

        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.BGR8));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.BGR10));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.BGR16));

        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB8));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB10));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB16));

        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(1, GenICamHelper.GetNumChannels(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(4, GenICamHelper.GetNumChannels(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(3, GenICamHelper.GetNumChannels(PixelFormat.RGB12_Planar));
    }
}
