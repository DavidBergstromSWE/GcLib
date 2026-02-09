using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GenICamConverterTests
{
    [TestMethod]
    public void GetBitsPerPixel_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.Mono8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.Mono10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.Mono16));

        Assert.AreEqual<uint>(24, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGR8));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGR10));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGR16));

        Assert.AreEqual<uint>(24, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB8));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB10));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB16));

        Assert.AreEqual<uint>(32, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(64, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(64, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(32, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(64, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(64, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(14, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(30, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(36, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(30, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(36, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(40, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(40, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(24, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(48, GenICamPixelFormatHelper.GetBitsPerPixel(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetBitsPerPixelPerChannel_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(14, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(10, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(12, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(8, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(16, GenICamPixelFormatHelper.GetBitsPerPixelPerChannel(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetPixelSize_ReturnsCorrectValues()
    {
        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.Mono8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.Mono10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.Mono16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGR8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGR10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGR16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGRa8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGRa10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGRa16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGBa8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGBa10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGBa16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerBG8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerBG10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerBG16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGR8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGR10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGR16));

        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.Mono10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.Mono12p));
        Assert.AreEqual(PixelSize.Bpp14, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.Mono14p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGR10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGR12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGB10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGB12p));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGR10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BayerGR12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGBa10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGBa12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGRa10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.BGRa12p));

        Assert.AreEqual(PixelSize.Bpp8, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB8_Planar));
        Assert.AreEqual(PixelSize.Bpp10, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB10_Planar));
        Assert.AreEqual(PixelSize.Bpp12, GenICamPixelFormatHelper.GetPixelSize(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetDynamicRangeMax_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.Mono10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.Mono16));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGR8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGR10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGR16));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB16));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(65535, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(16383, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(255, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(1023, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(4095, GenICamPixelFormatHelper.GetPixelDynamicRangeMax(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetNumChannels_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.Mono8));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.Mono10));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.Mono16));

        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGR8));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGR10));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGR16));

        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB8));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB10));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB16));

        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(1, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(4, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(3, GenICamPixelFormatHelper.GetNumChannels(PixelFormat.RGB12_Planar));
    }
}
