using System;
using System.Linq;
using GcLib.Utilities.Imaging;
using GcLib.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class TestPatternGeneratorTests
{
    private readonly uint _width = 10;
    private readonly uint _height = 10;

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(2, 1)]
    [DataRow(1, 2)]
    public void CreateImage_SizeIsTooSmall_ThrowsArgumentException(int width, int height)
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => _ = TestPatternGenerator.CreateImage((uint)width, (uint)height, PixelFormat.Mono8, TestPattern.Black));
    }


    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_Black_ReturnedImageIsMinimum(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Black);

        // Assert
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsTrue(bytes.All(b => b == 0));
    }


    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_White_ReturnedImageIsMaximum(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.White);

        // Assert
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.IsTrue(image.All(p => p == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.IsTrue(image.All(p => p == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_GrayVerticalRamp_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRamp);

        // Assert
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
            Assert.AreEqual(0, image.Min());
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
            Assert.AreEqual(0, image.Min());
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_GrayVerticalRampMoving_ReturnedImagesChangeWithFrameNumber(PixelFormat pixelFormat)
    {
        var oldBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRampMoving);

        for (ulong i = 1; i < _height + 10; i++)
        {
            // Act
            var newBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayVerticalRampMoving, i);

            // Assert
            Assert.IsNotNull(newBytes);
            Assert.AreEqual((uint)newBytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
            Assert.AreNotEqual(oldBytes, newBytes); // Ensure the image changes with frame number

            oldBytes = newBytes;
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_GrayHorizontalRamp_ReturnedImageIsNotNull(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRamp);

        // Assert
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
            Assert.AreEqual(0, image.Min());
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
            Assert.AreEqual(0, image.Min());
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_GrayHorizontalRampMoving_ReturnedImagesChangeWithFrameNumber(PixelFormat pixelFormat)
    {
        var oldBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRampMoving);

        for (ulong i = 1; i < _width + 10; i++)
        {
            // Act
            var newBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.GrayHorizontalRampMoving, i);

            // Assert
            Assert.IsNotNull(newBytes);
            Assert.AreEqual((uint)newBytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
            Assert.AreNotEqual(oldBytes, newBytes); // Ensure the image changes with frame number

            oldBytes = newBytes;
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_VerticalLineMoving_ReturnedImagesChangeWithFrameNumber(PixelFormat pixelFormat)
    {
        var oldBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.VerticalLineMoving);

        for (ulong i = 1; i < _width + 10; i++)
        {
            // Act
            var newBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.VerticalLineMoving, i);

            // Assert
            Assert.IsNotNull(newBytes);
            Assert.AreEqual((uint)newBytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
            Assert.AreNotEqual(oldBytes, newBytes); // Ensure the image changes with frame number

            if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
            {
                var image = NumericHelper.ToArray<byte>(newBytes);
                Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
                Assert.AreEqual(0, image.Min());
            }
            else
            {
                var image = NumericHelper.ToArray<ushort>(newBytes);
                Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
                Assert.AreEqual(0, image.Min());
            }

            oldBytes = newBytes;
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_HorizontalLineMoving_ReturnedImagesChangeWithFrameNumber(PixelFormat pixelFormat)
    {
        var oldBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.HorizontalLineMoving);

        for (ulong i = 1; i < _width + 10; i++)
        {
            // Act
            var newBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.HorizontalLineMoving, i);

            // Assert
            Assert.IsNotNull(newBytes);
            Assert.AreEqual((uint)newBytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
            Assert.AreNotEqual(oldBytes, newBytes); // Ensure the image changes with frame number

            if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
            {
                var image = NumericHelper.ToArray<byte>(newBytes);
                Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
                Assert.AreEqual(0, image.Min());
            }
            else
            {
                var image = NumericHelper.ToArray<ushort>(newBytes);
                Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max());
                Assert.AreEqual(0, image.Min());
            }

            oldBytes = newBytes;
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_FrameCounter_ReturnedImagesChangeWithFrameNumber(PixelFormat pixelFormat)
    {
        var oldBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.FrameCounter);

        for (ulong i = 1; i < GenICamHelper.GetPixelDynamicRangeMax(pixelFormat) + 1; i++) 
        {
            // Act
            var newBytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.FrameCounter, i);

            // Assert
            Assert.IsNotNull(newBytes);
            Assert.AreEqual((uint)newBytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
            Assert.AreNotEqual(oldBytes, newBytes); // Ensure the image changes with frame number

            if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
            {
                var image = NumericHelper.ToArray<byte>(newBytes);
                Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max()); // Frame counter should be white
                Assert.AreEqual(0, image.Min()); // Frame counter background should be black
            }
            else
            {
                var image = NumericHelper.ToArray<ushort>(newBytes);
                Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), image.Max()); // Frame counter should be white
                Assert.AreEqual(0, image.Min()); // Frame counter background should be black
            }

            oldBytes = newBytes;
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_WhiteNoise_ReturnedImageIsRandom(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.WhiteNoise);

        // Assert
        Assert.IsNotNull(bytes);
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);
        Assert.IsFalse(bytes.All(p => p == p + 1));
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    public void CreateImage_Red_RGB_ColorOrderIsRGB(PixelFormat pixelFormat) 
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Red);

        // Assert
        Assert.IsNotNull(bytes);
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat))); // R
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // B
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat))); // R
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // B
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_Red_BGR_ColorOrderIsBGR(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Red);

        // Assert
        Assert.IsNotNull(bytes);
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // B
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));   // R
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // B
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));   // R
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_Green_ColorOrderIsCorrect(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Green);

        // Assert
        Assert.IsNotNull(bytes);
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // R
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // B
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // R
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // B
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.RGB8)]
    [DataRow(PixelFormat.RGB10)]
    [DataRow(PixelFormat.RGB12)]
    [DataRow(PixelFormat.RGB14)]
    [DataRow(PixelFormat.RGB16)]
    public void CreateImage_Blue_RGB_ColorOrderIsRGB(PixelFormat pixelFormat)
    {
        // Act
        var bytes= TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Blue);

        // Assert
        Assert.IsNotNull(bytes);
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // R
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));   // B
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == 0)); // R
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat)));   // B
        }
    }

    [TestMethod]
    [DataRow(PixelFormat.BGR8)]
    [DataRow(PixelFormat.BGR10)]
    [DataRow(PixelFormat.BGR12)]
    [DataRow(PixelFormat.BGR14)]
    [DataRow(PixelFormat.BGR16)]
    public void CreateImage_Blue_BGR_ColorOrderIsBGR(PixelFormat pixelFormat)
    {
        // Act
        var bytes = TestPatternGenerator.CreateImage(_width, _height, pixelFormat, TestPattern.Blue);

        // Assert
        Assert.IsNotNull(bytes);
        Assert.AreEqual((uint)bytes.Length, _width * _height * GenICamHelper.GetBitsPerPixel(pixelFormat) / 8);

        if (GenICamHelper.GetBitsPerPixelPerChannel(pixelFormat) <= 8)
        {
            var image = NumericHelper.ToArray<byte>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat))); // B
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // R
        }
        else
        {
            var image = NumericHelper.ToArray<ushort>(bytes);
            Assert.IsTrue(image.Where((b, i) => i % 3 == 0).All(b => b == GenICamHelper.GetPixelDynamicRangeMax(pixelFormat))); // B
            Assert.IsTrue(image.Where((b, i) => i % 3 == 1).All(b => b == 0));   // G
            Assert.IsTrue(image.Where((b, i) => i % 3 == 2).All(b => b == 0));   // R
        }

    }
}