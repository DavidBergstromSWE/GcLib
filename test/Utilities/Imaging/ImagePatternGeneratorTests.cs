using System.Linq;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class ImagePatternGeneratorTests
{
    private readonly uint _width = 10;
    private readonly uint _height = 10;

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_Black_ReturnedImageIsMinimum(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Black);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(image.All(p => p == 0));
    }


    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_White_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.White);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_8bitWhite_AllPixelValuesAreMaximum(PixelFormat pixelFormat)
    {
        // Act
        var bytearray = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.White);
        var image = NumericHelper.ToArray<byte>(bytearray);

        // Assert
        Assert.IsTrue(image.All(p => p == GenICamConverter.GetDynamicRangeMax(pixelFormat)));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    public void CreateImage_16bitWhite_AllPixelValuesAreMaximum(PixelFormat pixelFormat)
    {
        // Act
        var bytearray = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.White);
        var image = NumericHelper.ToArray<ushort>(bytearray);

        // Assert
        Assert.IsTrue(image.All(p => p == GenICamConverter.GetDynamicRangeMax(pixelFormat)));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_GrayVerticalRamp_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRamp);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_GrayVerticalRampMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _height + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRampMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_GrayHorizontalRamp_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRamp);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_GrayHorizontalRampMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _width + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRampMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_VerticalLineMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _width + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.VerticalLineMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_HorizontalLineMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _height + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.HorizontalLineMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_FrameCounter_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < GenICamConverter.GetDynamicRangeMax(pixelFormat) + 10; i++) 
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.FrameCounter, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_WhiteNoise_ReturnedImageIsRandom(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.WhiteNoise);

        // Assert
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsFalse(image.All(p => p == p + 1));
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_Red_RGB_ColorOrderIsRGB(PixelFormat pixelFormat) {
        
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Red);

        // Assert
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 255)); // R
        Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
        Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // B
    }

    [TestMethod]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_Red_BGR_ColorOrderIsBGR(PixelFormat pixelFormat)
    {

        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Red);

        // Assert
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // B
        Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
        Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 255));   // R
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_Green_ColorOrderIsCorrect(PixelFormat pixelFormat)
    {

        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Green);

        // Assert
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // R
        Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 255));   // G
        Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // B
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_Blue_RGB_ColorOrderIsRGB(PixelFormat pixelFormat)
    {

        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Blue);

        // Assert
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // R
        Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
        Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 255));   // B
    }

    [TestMethod]
    [DataRow(PixelFormat.BGR8)]
    public void CreateImage_Blue_BGR_ColorOrderIsBGR(PixelFormat pixelFormat)
    {

        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Blue);

        // Assert
        Assert.AreEqual((uint)image.Length, _width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 255)); // B
        Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
        Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // R
    }
}