using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Image test pattern generator. 
/// Supported pixel formats are listed in the <see cref="SupportedPixelFormats"/> property.
/// Supported test patterns are listed in the <see cref="SupportedTestPatterns"/> property.
/// </summary>
/// ToDo: Add support for more pixel formats in the image test pattern generator.
public static class ImagePatternGenerator
{
    /// <summary>
    /// Random number generator.
    /// </summary>
    private static readonly Randomizer _random = new();

    /// <summary>
    /// Lists the currently supported pixel formats of the class.
    /// </summary>
    public static List<PixelFormat> SupportedPixelFormats =>
    [
        PixelFormat.Mono8,
        PixelFormat.Mono10,
        PixelFormat.Mono12,
        PixelFormat.Mono14,
        PixelFormat.Mono16,
        PixelFormat.RGB8,
        PixelFormat.BGR8
    ];

    /// <summary>
    /// Lists the currently supported test patterns of the class.
    /// </summary>
    public static List<TestPattern> SupportedTestPatterns =>
    [
        TestPattern.Black,
        TestPattern.White,
        TestPattern.GrayVerticalRamp,
        TestPattern.GrayVerticalRampMoving,
        TestPattern.GrayHorizontalRamp,
        TestPattern.GrayHorizontalRampMoving,
        TestPattern.VerticalLineMoving,
        TestPattern.HorizontalLineMoving,
        TestPattern.FrameCounter,
        TestPattern.WhiteNoise,
        TestPattern.Red,
        TestPattern.Green,
        TestPattern.Blue
    ];

    #region Public methods

    /// <summary>
    /// Creates image using specified size, pixel format and test pattern.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format (according to GenICam PFNC).</param>
    /// <param name="testPattern">Test pattern.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to create dynamic test patterns which changes from frame to frame.</param>
    /// <returns>Image test pattern as byte array.</returns>
    public static byte[] CreateImage(uint width, uint height, PixelFormat pixelFormat, TestPattern testPattern, ulong frameNumber = 0)
    {
        if (SupportedPixelFormats.Contains(pixelFormat) == false)
            throw new NotSupportedException("PixelFormat is not supported!");

        if (SupportedTestPatterns.Contains(testPattern) == false)
            throw new NotSupportedException("Testpattern is not supported!");

        byte[] image;

        uint numChannels = GenICamConverter.GetNumChannels(pixelFormat);
        uint bitDepth = GenICamConverter.GetBitsPerPixelPerChannel(pixelFormat);

        if (numChannels == 1)
        {
            if (bitDepth <= 8)
            {
                image = CreateMono<byte>(width, height, pixelFormat, testPattern, frameNumber);
                return image;
            }
            if (bitDepth <= 16)
            {
                image = NumericHelper.ToBytes(CreateMono<ushort>(width, height, pixelFormat, testPattern, frameNumber));
                return image;
            }
        }
        else if (numChannels == 3)
        {
            image = CreateColor(width, height, pixelFormat, testPattern, frameNumber);
            return image;
        }
        
        throw new NotImplementedException("PixelFormat not implemented!");
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Creates a monochrome image using specified size, bit depth and test pattern.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format (according to GenICam PFNC).</param>
    /// <param name="testPattern">Test pattern.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to create dynamic test patterns which changes from frame to frame.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] CreateMono<T>(uint width, uint height, PixelFormat pixelFormat, TestPattern testPattern, ulong frameNumber = 0) where T : INumber<T>
    {
        // Minimum and maximum values of PixelSize
        var min = T.Zero;
        var max = (T)Convert.ChangeType(GenICamConverter.GetDynamicRangeMax(pixelFormat), typeof(T));

        T[] image = testPattern switch
        {
            TestPattern.Black => UniformGray(width, height, min),
            TestPattern.White => UniformGray(width, height, max),
            TestPattern.GrayVerticalRamp => GrayVerticalRamp(width, height, max),
            TestPattern.GrayVerticalRampMoving => GrayVerticalRamp(width, height, max, frameNumber),
            TestPattern.GrayHorizontalRamp => GrayHorizontalRamp(width, height, max),
            TestPattern.GrayHorizontalRampMoving => GrayHorizontalRamp(width, height, max, frameNumber),
            TestPattern.VerticalLineMoving => VerticalLineMoving(width, height, max, frameNumber),
            TestPattern.HorizontalLineMoving => HorizontalLineMoving(width, height, max, frameNumber),
            TestPattern.FrameCounter => DrawCenteredText(CycleUniformGray(width, height, max, frameNumber), width, height, pixelFormat, frameNumber.ToString()),
            TestPattern.WhiteNoise => RandomWhiteNoise(width, height, max),
            _ => throw new NotImplementedException()
        };

        return image;
    }

    /// <summary>
    /// Creates RGB image using specified size, pixel format and test pattern.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format (according to GenICam PFNC).</param>
    /// <param name="testPattern">Test pattern.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to create dynamic test patterns which changes from frame to frame.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] CreateColor(uint width, uint height, PixelFormat pixelFormat, TestPattern testPattern, ulong frameNumber = 0)
    {
        byte[] image = testPattern switch
        {
            TestPattern.Black => UniformColor(width, height, pixelFormat, Color.Black),
            TestPattern.White => UniformColor(width, height, pixelFormat, Color.White),
            TestPattern.GrayVerticalRamp => VerticalBarsColor(width, height, pixelFormat),
            TestPattern.GrayHorizontalRamp => HorizontalBarsColor(width, height, pixelFormat),
            TestPattern.GrayHorizontalRampMoving => HorizontalRainbowColor(width, height, pixelFormat, frameNumber),
            TestPattern.GrayVerticalRampMoving => VerticalRainbowColor(width, height, pixelFormat, frameNumber),
            TestPattern.VerticalLineMoving => VerticalLineMovingColor(width, height, frameNumber),
            TestPattern.HorizontalLineMoving => HorizontalLineMovingColor(width, height, frameNumber),
            TestPattern.FrameCounter => DrawCenteredText(CycleUniformColor(width, height, pixelFormat, frameNumber), width, height, pixelFormat, frameNumber.ToString()),
            TestPattern.WhiteNoise => RandomWhiteNoise<byte>(width, height, 255, 3),
            TestPattern.Red => UniformColor(width, height, pixelFormat, Color.FromArgb(255, 0, 0)),
            TestPattern.Green => UniformColor(width, height, pixelFormat, Color.FromArgb(0, 255, 0)),
            TestPattern.Blue => UniformColor(width, height, pixelFormat, Color.FromArgb(0, 0, 255)),
            _ => throw new NotImplementedException()
        };

        return image;
    }

    #endregion

    #region Monochrome methods

    /// <summary>
    /// Creates a uniform monochrome image.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="level">Gray level.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] UniformGray<T>(uint width, uint height, T level) where T : INumber<T>
    {
        // Boundary check.
        if (level < NumericHelper.GetMinValue<T>() || level > NumericHelper.GetMaxValue<T>())
            throw new ArgumentOutOfRangeException(nameof(level), $"level must be >= {NumericHelper.GetMinValue<T>()} and <= {NumericHelper.GetMaxValue<T>()})");

        var image = new T[width * height];

        // Build array.
        for (int i = 0; i < image.Length; i++)
        {
            image[i] = level;
        }

        return image;
    }

    /// <summary>
    /// Creates a uniform monochrome image that cycles in tone from minimum to maximum gray level.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum gray level.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] CycleUniformGray<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        return UniformGray(width, height, (T)Convert.ChangeType(frameNumber % Convert.ToUInt64(max), typeof(T)));
    }

    /// <summary>
    /// Creates a monochrome image which vertically goes from the darkest possible value (bottom) to the brightest (top).
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to make the pattern move vertically (frame-by-frame) from top to bottom.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] GrayVerticalRamp<T>(uint width, uint height, T max, ulong frameNumber = 0) where T : INumber<T>
    {
        var image = new T[width * height];

        ulong maxValue = Convert.ToUInt64(max);

        for (int i = 0; i < height; i++)
        {
            T level = (T)Convert.ChangeType(Math.Round(maxValue - ((double)i - frameNumber) * maxValue / height) % (maxValue + 1), typeof(T));
            for (int j = 0; j < width; j++)
            {
                image[i * width + j] = level;
            }
        }

        return image;
    }

    /// <summary>
    /// Creates a monochrome image which horizontally goes from the darkest possible value (right) to the brightest (left).
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// /// <param name="frameNumber">(optional) Frame number index, can be used to make the pattern move horizontally (frame-by-frame) from left to right.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] GrayHorizontalRamp<T>(uint width, uint height, T max, ulong frameNumber = 0) where T : INumber<T>
    {
        var image = new T[width * height];

        ulong maxValue = Convert.ToUInt64(max);

        for (int j = 0; j < width; j++)
        {
            T level = (T)Convert.ChangeType(Math.Round(maxValue - ((double)j - frameNumber) * maxValue / width) % (maxValue + 1), typeof(T));
            for (int i = 0; i < height; i++)
            {
                image[i * width + j] = level;
            }
        }

        return image;
    }

    /// <summary>
    /// Creates a black image with a horizontal white line moving from top to bottom.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum gray level.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] HorizontalLineMoving<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        var image = new T[width * height];

        int lineRow = (int)Math.Round((height + (double)frameNumber) % height);

        // build image array
        for (int j = 0; j < width; j++)
        {
            image[lineRow * width + j] = max;
        }

        return image;
    }

    /// <summary>
    /// Creates a black image with a vertical white line moving from left to right.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum gray level.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] VerticalLineMoving<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        var image = new T[width * height];

        int lineColumn = (int)Math.Round((width + (double)frameNumber) % width);

        // build image array
        for (int i = 0; i < height; i++)
        {
            image[i * width + lineColumn] = max;
        }

        return image;
    }

    /// <summary>
    /// Creates an image of random white noise.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum gray level.</param>
    /// <param name="numChannel">Number of image channels.</param>
    /// <returns>Test image of type <typeparamref name="T"/> and of size <paramref name="width"/> x <paramref name="height"/> in <paramref name="numChannel"/> channels.</returns>
    private static T[] RandomWhiteNoise<T>(uint width, uint height, T max, uint numChannel = 1) where T : INumber<T>
    {
        T[] img = new T[height * width * numChannel];

        for (int i = 0; i < img.Length; i++)
            img[i] = _random.Next(max);

        return img;
    }

    #endregion

    #region Color methods

    /// <summary>
    /// Creates a uniform 8-bit color image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="color">Color of image.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] UniformColor(uint width, uint height, PixelFormat pixelFormat, Color color)
    {
        byte[] image = new byte[width * height * 3];

        byte color1 = pixelFormat.ToString().Contains("RGB")? color.R : color.B;
        byte color2 = color.G;
        byte color3 = pixelFormat.ToString().Contains("RGB") ? color.B : color.R;

        for (int i = 0; i < image.Length; i += 3)
        {
            image[i] = color1;
            image[i + 1] = color2;
            image[i + 2] = color3;
        }
        return image;
    }

    /// <summary>
    /// Creates a uniform image that cycles in color.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] CycleUniformColor(uint width, uint height, PixelFormat pixelFormat, ulong frameNumber)
    {
        Color[] colors = GetRainbowColorRange(256);
        return UniformColor(width, height, pixelFormat, colors[(byte)Math.Round((255 + (double)frameNumber) % (255 + 1))]);
    }

    /// <summary>
    /// Creates an RGB image filled vertically with (horizontal) stripes of color including White, Black, Red, Green, Blue, Cyan, Magenta and Yellow.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] VerticalBarsColor(uint width, uint height, PixelFormat pixelFormat)
    {
        byte[] image = new byte[width * height * 3];

        Color color;

        // build image array
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width * 3; j += 3)
            {
                if (i < height / 8)
                {
                    color = Color.White;
                }
                else if (i < height * 2 / 8)
                {
                    color = Color.Black;
                }
                else if (i < height * 3 / 8)
                {
                    color = Color.Red;
                }
                else if (i < height * 4 / 8)
                {
                    color = Color.Green;
                }
                else if (i < height * 5 / 8)
                {
                    color = Color.Blue;
                }
                else if (i < height * 6 / 8)
                {
                    color = Color.Cyan;
                }
                else if (i < height * 7 / 8)
                {
                    color = Color.Magenta;
                }
                else
                {
                    color = Color.Yellow;
                }

                image[(i * width * 3) + j] = pixelFormat.ToString().Contains("RGB") ? color.R : color.B;
                image[(i * width * 3) + j + 1] = color.G;
                image[(i * width * 3) + j + 2] = pixelFormat.ToString().Contains("RGB") ? color.B : color.R;
            }

        }
        return image;
    }

    /// <summary>
    /// Creates an RGB image filled horizontally with (vertical) stripes of color including White, Black, Red, Green, Blue, Cyan, Magenta and Yellow.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] HorizontalBarsColor(uint width, uint height, PixelFormat pixelFormat)
    {
        byte[] image = new byte[width * height * 3];

        Color color;

        // build image array
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width * 3; j += 3)
            {
                if (j < width * 3 * 1 / 8)
                {
                    color = Color.White;
                }
                else if (j < width * 3 * 2 / 8)
                {
                    color = Color.Black;
                }
                else if (j < width * 3 * 3 / 8)
                {
                    color = Color.Red;
                }
                else if (j < width * 3 * 4 / 8)
                {
                    color = Color.Green;
                }
                else if (j < width * 3 * 5 / 8)
                {
                    color = Color.Blue;
                }
                else if (j < width * 3 * 6 / 8)
                {
                    color = Color.Cyan;
                }
                else if (j < width * 3 * 7 / 8)
                {
                    color = Color.Magenta;
                }
                else
                {
                    color = Color.Yellow;
                }

                image[(i * width * 3) + j] = pixelFormat.ToString().Contains("RGB") ? color.R : color.B;
                image[(i * width * 3) + j + 1] = color.G;
                image[(i * width * 3) + j + 2] = pixelFormat.ToString().Contains("RGB") ? color.B : color.R;
            }

        }
        return image;
    }

    /// <summary>
    /// Creates an RGB image with a rainbow range of colors which horizontally spans the width of the image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to make the pattern move horizontally (frame-by-frame) from left to right.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] HorizontalRainbowColor(uint width, uint height, PixelFormat pixelFormat, ulong frameNumber = 0)
    {
        byte[] image = new byte[width * height * 3];

        Color[] colors = GetRainbowColorRange(width);

        // build image array
        for (int j = 0; j < width * 3; j += 3)
        {
            int a = j / 3 - (int)frameNumber;

            for (int i = 0; i < height; i++)
            {
                image[(i * width * 3) + j] = pixelFormat.ToString().Contains("RGB") ? colors[((a % width) + width) % width].R : colors[((a % width) + width) % width].B;
                image[(i * width * 3) + j + 1] = colors[((a % width) + width) % width].G;
                image[(i * width * 3) + j + 2] = pixelFormat.ToString().Contains("RGB") ? colors[((a % width) + width) % width].B : colors[((a % width) + width) % width].R;
            }
        }

        return image;
    }

    /// <summary>
    /// Creates an RGB image with a rainbow range of colors which vertically spans the height of the image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to make the pattern move vertically (frame-by-frame) from top to bottom.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] VerticalRainbowColor(uint width, uint height, PixelFormat pixelFormat, ulong frameNumber = 0)
    {
        byte[] image = new byte[width * height * 3];

        Color[] colors = GetRainbowColorRange(height);

        // build image array
        for (int i = 0; i < height; i++)
        {
            int a = i - (int)frameNumber;

            for (int j = 0; j < width * 3; j += 3)
            {
                image[(i * width * 3) + j] = pixelFormat.ToString().Contains("RGB") ? colors[((a % height) + height) % height].R : colors[((a % height) + height) % height].B;
                image[(i * width * 3) + j + 1] = colors[((a % height) + height) % height].G;
                image[(i * width * 3) + j + 2] = pixelFormat.ToString().Contains("RGB") ? colors[((a % height) + height) % height].B : colors[((a % height) + height) % height].R;
            }
        }

        return image;
    }

    /// <summary>
    /// Creates a black image with a horizontal white line moving from top to bottom.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] HorizontalLineMovingColor(uint width, uint height, ulong frameNumber)
    {
        var image = new byte[width * height * 3];

        int lineRow = (int)Math.Round((height + (double)frameNumber) % height);

        // build image array
        for (int j = 0; j < width * 3; j += 3)
        {
            // white line
            image[(lineRow * width * 3) + j] = 255;
            image[(lineRow * width * 3) + j + 1] = 255;
            image[(lineRow * width * 3) + j + 2] = 255;
        }

        return image;
    }

    /// <summary>
    /// Creates a black image with a vertical white line moving from left to right.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static byte[] VerticalLineMovingColor(uint width, uint height, ulong frameNumber)
    {
        var image = new byte[width * height * 3];

        int lineColumn = (int)Math.Round((width + (double)frameNumber) % width);

        // build image array
        for (int i = 0; i < height; i++)
        {
            image[(i * width * 3) + lineColumn * 3] = 255;
            image[(i * width * 3) + lineColumn * 3 + 1] = 255;
            image[(i * width * 3) + lineColumn * 3 + 2] = 255;
        }

        return image;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Returns a range of (8-bit) RGB colors of the rainbow, spaced equally over the specified number of sampling points.
    /// </summary>
    /// <param name="pixels">Number of sampling points.</param>
    /// <returns>Array of colors (of size <paramref name="pixels"/>).</returns>
    private static Color[] GetRainbowColorRange(uint pixels)
    {
        Color[] colors = new Color[pixels];

        //build range of rainbow colors
        double i = 0;
        for (int j = 0; j < pixels; j++)
        {
            colors[j] = ColorConverter.HSL2RGB(i, 0.5, 0.5);
            i += 1.0 / pixels;
        }

        return colors;
    }

    /// <summary>
    /// Draws text centered on image.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="image">Input image as an array of type <see cref="T"/>.</param>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="pixelFormat">Pixel format (according to GenICam PFNC).</param>
    /// <param name="text">Text to be written.</param>
    /// <returns>Input image with centered text.</returns>
    private static T[] DrawCenteredText<T>(T[] image, uint width, uint height, PixelFormat pixelFormat, string text)
    {
        var mat = new Emgu.CV.Mat((int)height, (int)width, EmguConverter.GetDepthType(pixelFormat), (int)GenICamConverter.GetNumChannels(pixelFormat));
        mat.SetTo(image);

        mat.DrawCenteredText(text);

        mat.CopyTo(image);
        return image;
    }

    #endregion
}