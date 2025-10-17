using System.Linq;
using GcLib.Utilities.Imaging;
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
    public void CreateImage_Black_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Black);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
        Assert.IsTrue(image.All(p => p == 0));
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_White_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.White);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_GrayVerticalRamp_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRamp);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_GrayVerticalRampMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _height + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRampMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_GrayHorizontalRamp_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRamp);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_GrayHorizontalRampMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _width + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRampMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_VerticalLineMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _width + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.VerticalLineMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_HorizontalLineMoving_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < _height + 10; i++)
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.HorizontalLineMoving, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_FrameCounter_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        for (ulong i = 0; i < GenICamConverter.GetDynamicRangeMax(pixelFormat) + 10; i++) 
        {
            // Act
            var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.FrameCounter, i);

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void CreateImage_WhiteNoise_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var image = ImagePatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.WhiteNoise);

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual(_width * _height * GenICamConverter.GetBitsPerPixel(pixelFormat) / 8, (uint)image.Length);
    }
}