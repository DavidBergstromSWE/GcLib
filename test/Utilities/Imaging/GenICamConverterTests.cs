using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GenICamConverterTests
{
    [TestMethod]
    public void GetBitsPerPixel_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixel(PixelFormat.Mono8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixel(PixelFormat.Mono10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixel(PixelFormat.Mono16));

        Assert.AreEqual<uint>(24, GenICamConverter.GetBitsPerPixel(PixelFormat.BGR8));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.BGR10));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.BGR16));

        Assert.AreEqual<uint>(24, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB8));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB10));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB16));

        Assert.AreEqual<uint>(32, GenICamConverter.GetBitsPerPixel(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(64, GenICamConverter.GetBitsPerPixel(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(64, GenICamConverter.GetBitsPerPixel(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(32, GenICamConverter.GetBitsPerPixel(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(64, GenICamConverter.GetBitsPerPixel(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(64, GenICamConverter.GetBitsPerPixel(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixel(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixel(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(14, GenICamConverter.GetBitsPerPixel(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(30, GenICamConverter.GetBitsPerPixel(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(36, GenICamConverter.GetBitsPerPixel(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(30, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(36, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixel(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(40, GenICamConverter.GetBitsPerPixel(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(40, GenICamConverter.GetBitsPerPixel(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(24, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(48, GenICamConverter.GetBitsPerPixel(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetBitsPerPixelPerChannel_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.Mono8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.Mono10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.Mono16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGR8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGR10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGR16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(14, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(10, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(12, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(8, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(16, GenICamConverter.GetBitsPerPixelPerChannel(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetPixelSize_ReturnsCorrectValues()
    {
        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.Mono8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.Mono10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.Mono16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.BGR8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BGR10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.BGR16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.RGB8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.RGB10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.RGB16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.BGRa8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BGRa10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.BGRa16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.RGBa8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.RGBa10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.RGBa16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.BayerBG8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BayerBG10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.BayerBG16));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.BayerGR8));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BayerGR10));
        Assert.AreEqual(PixelSize.Bpp16, GenICamConverter.GetPixelSize(PixelFormat.BayerGR16));

        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.Mono10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.Mono12p));
        Assert.AreEqual(PixelSize.Bpp14, GenICamConverter.GetPixelSize(PixelFormat.Mono14p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BGR10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.BGR12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.RGB10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.RGB12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BayerGB10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.BayerGB12p));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BayerGR10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.BayerGR12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.RGBa10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.RGBa12p));

        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.BGRa10p));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.BGRa12p));

        Assert.AreEqual(PixelSize.Bpp8, GenICamConverter.GetPixelSize(PixelFormat.RGB8_Planar));
        Assert.AreEqual(PixelSize.Bpp10, GenICamConverter.GetPixelSize(PixelFormat.RGB10_Planar));
        Assert.AreEqual(PixelSize.Bpp12, GenICamConverter.GetPixelSize(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetDynamicRangeMax_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono16));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGR8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGR10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGR16));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB16));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(65535, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(16383, GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(255, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(1023, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(4095, GenICamConverter.GetDynamicRangeMax(PixelFormat.RGB12_Planar));
    }

    [TestMethod]
    public void GetNumChannels_ReturnsCorrectValues()
    {
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.Mono8));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.Mono10));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.Mono16));

        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.BGR8));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.BGR10));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.BGR16));

        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB8));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB10));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB16));

        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.BGRa8));
        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.BGRa10));
        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.BGRa16));

        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.RGBa8));
        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.RGBa10));
        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.RGBa16));

        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerBG8));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerBG10));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerBG16));

        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGR8));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGR10));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGR16));

        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.Mono10p));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.Mono12p));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.Mono14p));

        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.BGR10p));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.BGR12p));

        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB10p));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB12p));

        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGB10p));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGB12p));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGR10p));
        Assert.AreEqual<uint>(1, GenICamConverter.GetNumChannels(PixelFormat.BayerGR12p));

        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.RGBa10p));
        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.RGBa12p));

        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.BGRa10p));
        Assert.AreEqual<uint>(4, GenICamConverter.GetNumChannels(PixelFormat.BGRa12p));

        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB8_Planar));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB10_Planar));
        Assert.AreEqual<uint>(3, GenICamConverter.GetNumChannels(PixelFormat.RGB12_Planar));
    }
}
