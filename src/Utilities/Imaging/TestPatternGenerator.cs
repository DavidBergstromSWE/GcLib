using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Image test pattern generator, supporting a list of common test patterns and pixel formats used and defined in the GenICam standard.
/// Images are created as byte arrays, using Little-Endian byte ordering.
/// </summary>
/// <remarks>
/// For a list of supported test patterns and pixel formats, see <see cref="TestPatterns"/> and  <see cref="PixelFormats"/> properties.
/// </remarks>
public static class TestPatternGenerator
{
    /// <summary>
    /// Random number generator.
    /// </summary>
    private static readonly Randomizer _random = new();

    /// <summary>
    /// Lists the currently supported pixel formats of the class.
    /// </summary>
    public static List<PixelFormat> PixelFormats =>
    [
        PixelFormat.Mono8,
        PixelFormat.Mono10,
        PixelFormat.Mono12,
        PixelFormat.Mono14,
        PixelFormat.Mono16,
        PixelFormat.RGB8,
        PixelFormat.RGB10,
        PixelFormat.RGB12,
        PixelFormat.RGB14,
        PixelFormat.RGB16,
        PixelFormat.BGR8,
        PixelFormat.BGR10,
        PixelFormat.BGR12,
        PixelFormat.BGR14,
        PixelFormat.BGR16,
    ];

    /// <summary>
    /// Lists the currently supported test patterns of the class.
    /// </summary>
    public static List<TestPattern> TestPatterns =>
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
        TestPattern.Blue,
    ];

    #region Public methods

    /// <summary>
    /// Creates an image using specified size, pixel format and test pattern.
    /// </summary>
    /// <remarks>
    /// For a list of supported test patterns and pixel formats, see <see cref="TestPatterns"/> and  <see cref="PixelFormats"/> properties.
    /// </remarks>
    /// <param name="width">Width of image (number of pixels). Needs to larger than 1.</param>
    /// <param name="height">Height of image (number of pixels). Needs to larger than 1.</param>
    /// <param name="pixelFormat">Pixel format (according to GenICam PFNC).</param>
    /// <param name="testPattern">Test pattern.</param>
    /// <param name="frameNumber">(optional) Running index which can be used to create dynamic test patterns which changes from frame to frame.</param>
    /// <returns>Image test pattern as byte array.</returns>
    public static byte[] CreateImage(uint width, uint height, PixelFormat pixelFormat, TestPattern testPattern, ulong frameNumber = 0)
    {
        if (PixelFormats.Contains(pixelFormat) == false)
            throw new NotSupportedException("PixelFormat is not supported!");

        if (TestPatterns.Contains(testPattern) == false)
            throw new NotSupportedException("Testpattern is not supported!");

        // Width and height needs to be larger than one.
        if (width <= 1 || height <= 1)
            throw new ArgumentException("Image width and height needs to larger than 1!");

        byte[] image;

        uint numChannels = GenICamConverter.GetNumChannels(pixelFormat);
        uint bitDepth = GenICamConverter.GetBitsPerPixelPerChannel(pixelFormat);

        if (numChannels == 1) // Monochrome images
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
        else if (numChannels == 3) // Color images
        {
            if (bitDepth <= 8)
            {
                image = CreateColor<byte>(width, height, pixelFormat, testPattern, frameNumber);
                return image;
            }
            if (bitDepth <= 16)
            {
                image = NumericHelper.ToBytes(CreateColor<ushort>(width, height, pixelFormat, testPattern, frameNumber));
                return image;
            }
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
        // Maximum pixel value for the specified pixel format.
        var max = (T)Convert.ChangeType(GenICamConverter.GetDynamicRangeMax(pixelFormat), typeof(T));

        T[] image = testPattern switch
        {
            TestPattern.Black => UniformGray(width, height, T.Zero),
            TestPattern.White => UniformGray(width, height, max),
            TestPattern.GrayVerticalRamp => GrayVerticalRamp(width, height, max),
            TestPattern.GrayVerticalRampMoving => GrayVerticalRamp(width, height, max, frameNumber),
            TestPattern.GrayHorizontalRamp => GrayHorizontalRamp(width, height, max),
            TestPattern.GrayHorizontalRampMoving => GrayHorizontalRamp(width, height, max, frameNumber),
            TestPattern.VerticalLineMoving => VerticalLineMoving(width, height, max, frameNumber),
            TestPattern.HorizontalLineMoving => HorizontalLineMoving(width, height, max, frameNumber),
            TestPattern.FrameCounter => DrawCenteredText(CycleUniformGray(width, height, max, frameNumber), width, height, max, pixelFormat, frameNumber.ToString()),
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
    private static T[] CreateColor<T>(uint width, uint height, PixelFormat pixelFormat, TestPattern testPattern, ulong frameNumber = 0) where T : INumber<T>
    {
        // Maximum pixel value for the specified pixel format.
        var max = (T)Convert.ChangeType(GenICamConverter.GetDynamicRangeMax(pixelFormat), typeof(T));

        T[] image = testPattern switch
        {
            TestPattern.Black => UniformColor(width, height, max, pixelFormat, Color.Black),
            TestPattern.White => UniformColor(width, height, max, pixelFormat, Color.White),
            TestPattern.GrayVerticalRamp => VerticalBarsColor(width, height, max, pixelFormat),
            TestPattern.GrayHorizontalRamp => HorizontalBarsColor(width, height, max, pixelFormat),
            TestPattern.GrayHorizontalRampMoving => HorizontalRainbowColor(width, height, max, pixelFormat, frameNumber),
            TestPattern.GrayVerticalRampMoving => VerticalRainbowColor(width, height, max, pixelFormat, frameNumber),
            TestPattern.VerticalLineMoving => VerticalLineMovingColor(width, height, max, frameNumber),
            TestPattern.HorizontalLineMoving => HorizontalLineMovingColor(width, height, max, frameNumber),
            TestPattern.FrameCounter => DrawCenteredText(CycleUniformColor(width, height, max, pixelFormat, frameNumber), width, height, max, pixelFormat, frameNumber.ToString()),
            TestPattern.WhiteNoise => RandomWhiteNoise<T>(width, height, max, 3),
            TestPattern.Red => UniformColor(width, height, max, pixelFormat, Color.FromArgb(255, 0, 0)),
            TestPattern.Green => UniformColor(width, height, max, pixelFormat, Color.FromArgb(0, 255, 0)),
            TestPattern.Blue => UniformColor(width, height, max, pixelFormat, Color.FromArgb(0, 0, 255)),
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
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] CycleUniformGray<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        return UniformGray(width, height, (T)Convert.ChangeType(frameNumber % Convert.ToUInt64(max - T.One), typeof(T)));
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
            T level = (T)Convert.ChangeType(Math.Round(maxValue - ((double)i - frameNumber) * maxValue / (height - 1)) % (maxValue + 1), typeof(T));
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
            T level = (T)Convert.ChangeType(Math.Round(maxValue - ((double)j - frameNumber) * maxValue / (width - 1)) % (maxValue + 1), typeof(T));
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
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] HorizontalLineMoving<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        var image = new T[width * height];

        int lineRow = (int)Math.Round((height + (double)frameNumber) % height);

        // build image array
        for (int j = 0; j < width; j++)
            image[lineRow * width + j] = max;

        return image;
    }

    /// <summary>
    /// Creates a black image with a vertical white line moving from left to right.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as numeric array (of size <paramref name="width"/> x <paramref name="height"/>).</returns>
    private static T[] VerticalLineMoving<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        var image = new T[width * height];

        int lineColumn = (int)Math.Round((width + (double)frameNumber) % width);

        // build image array
        for (int i = 0; i < height; i++)
            image[i * width + lineColumn] = max;

        return image;
    }

    #endregion

    #region Color methods

    /// <summary>
    /// Creates an image of random white noise.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="numChannel">Number of image channels.</param>
    /// <returns>Test image of type <typeparamref name="T"/> and of size <paramref name="width"/> x <paramref name="height"/> in <paramref name="numChannel"/> channels.</returns>
    private static T[] RandomWhiteNoise<T>(uint width, uint height, T max, uint numChannel = 1) where T : INumber<T>
    {
        T[] img = new T[height * width * numChannel];

        for (int i = 0; i < img.Length; i++)
            img[i] = _random.Next(max);

        return img;
    }

    /// <summary>
    /// Creates a uniform 8-bit color image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="color">Color of image.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] UniformColor<T>(uint width, uint height, T max, PixelFormat pixelFormat, Color color) where T : INumber<T>
    {
        T[] image = new T[width * height * 3];

        var colorOrder = pixelFormat.ToString()[..3];

        T color1 = colorOrder.Equals("RGB")? (T)Convert.ChangeType(color.R / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T)) : (T)Convert.ChangeType(color.B / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
        T color2 = (T)Convert.ChangeType(color.G / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
        T color3 = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(color.B / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T)) : (T)Convert.ChangeType(color.R / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));

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
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] CycleUniformColor<T>(uint width, uint height, T max, PixelFormat pixelFormat, ulong frameNumber) where T : INumber<T>
    {
        Color[] colors = GetRainbowColorRange(256); // GetRainbowColorRange(256);
        return UniformColor(width, height, max, pixelFormat, colors[(byte)Math.Round((255 + (double)frameNumber) % (255 + 1))]);
    }

    /// <summary>
    /// Creates an RGB image filled vertically with horizontal bars of color in the order White, Black, Red, Green, Blue, Cyan, Magenta and Yellow.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] VerticalBarsColor<T>(uint width, uint height, T max, PixelFormat pixelFormat) where T : INumber<T>
    {
        T[] image = new T[width * height * 3];

        var colorOrder = pixelFormat.ToString()[..3];

        Color color;

        // build image array
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width * 3; j += 3)
            {
                if (i < height / 8)             // 1st bar
                    color = Color.White;
                else if (i < height * 2 / 8)    // 2nd
                    color = Color.Black;
                else if (i < height * 3 / 8)    // 3rd
                    color = Color.Red;
                else if (i < height * 4 / 8)    // 4th
                    color = Color.FromArgb(0, 255, 0); // (green)
                else if (i < height * 5 / 8)    // 5th
                    color = Color.Blue;
                else if (i < height * 6 / 8)    // 6th
                    color = Color.Cyan;
                else if (i < height * 7 / 8)    // 7th
                    color = Color.Magenta;
                else color = Color.Yellow;      // 8th

                image[(i * width * 3) + j] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(color.R / byte.MaxValue, typeof(T)) * max : (T)Convert.ChangeType(color.B / byte.MaxValue, typeof(T)) * max;
                image[(i * width * 3) + j + 1] = (T)Convert.ChangeType(color.G / byte.MaxValue, typeof(T)) * max;
                image[(i * width * 3) + j + 2] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(color.B / byte.MaxValue, typeof(T)) * max : (T)Convert.ChangeType(color.R / byte.MaxValue, typeof(T)) * max;
            }
        }

        return image;
    }

    /// <summary>
    /// Creates an RGB image filled horizontally with vertical bars of color in the order White, Black, Red, Green, Blue, Cyan, Magenta and Yellow.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] HorizontalBarsColor<T>(uint width, uint height, T max, PixelFormat pixelFormat) where T : INumber<T>
    {
        T[] image = new T[width * height * 3];

        var colorOrder = pixelFormat.ToString()[..3];

        Color color;

        // build image array
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width * 3; j += 3)
            {
                if (j < width * 3 * 1 / 8)          // 1st bar
                    color = Color.White;
                else if (j < width * 3 * 2 / 8)     // 2nd
                    color = Color.Black;
                else if (j < width * 3 * 3 / 8)     // 3rd
                    color = Color.Red;
                else if (j < width * 3 * 4 / 8)     // 4th
                    color = Color.FromArgb(0, 255, 0); // (green)
                else if (j < width * 3 * 5 / 8)     // 5th
                    color = Color.Blue;
                else if (j < width * 3 * 6 / 8)     // 6th
                    color = Color.Cyan;
                else if (j < width * 3 * 7 / 8)     // 7th
                    color = Color.Magenta;
                else color = Color.Yellow;          // 8th

                image[(i * width * 3) + j] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(color.R / byte.MaxValue, typeof(T)) * max : (T)Convert.ChangeType(color.B / byte.MaxValue, typeof(T)) * max;
                image[(i * width * 3) + j + 1] = (T)Convert.ChangeType(color.G / byte.MaxValue, typeof(T)) * max;
                image[(i * width * 3) + j + 2] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(color.B / byte.MaxValue, typeof(T)) * max : (T)Convert.ChangeType(color.R / byte.MaxValue, typeof(T)) * max;
            }
        }

        return image;
    }

    /// <summary>
    /// Creates an RGB image with a rainbow range of colors which horizontally spans the width of the image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to make the pattern move horizontally (frame-by-frame) from left to right.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] HorizontalRainbowColor<T>(uint width, uint height, T max, PixelFormat pixelFormat, ulong frameNumber = 0) where T : INumber<T>
    {
        T[] image = new T[width * height * 3];

        Color[] colors = GetRainbowColorRange(width);

        var colorOrder = pixelFormat.ToString()[..3];

        // build image array
        for (int j = 0; j < width * 3; j += 3)
        {
            int a = j / 3 - (int)frameNumber;

            for (int i = 0; i < height; i++)
            {
                image[(i * width * 3) + j] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(colors[((a % width) + width) % width].R / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T)) : (T)Convert.ChangeType(colors[((a % width) + width) % width].B / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
                image[(i * width * 3) + j + 1] = (T)Convert.ChangeType(colors[((a % width) + width) % width].G / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
                image[(i * width * 3) + j + 2] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(colors[((a % width) + width) % width].B / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T)) : (T)Convert.ChangeType(colors[((a % width) + width) % width].R / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
            }
        }

        return image;
    }

    /// <summary>
    /// Creates an RGB image with a rainbow range of colors which vertically spans the height of the image.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="pixelFormat">Pixel format.</param>
    /// <param name="frameNumber">(optional) Frame number index, can be used to make the pattern move vertically (frame-by-frame) from top to bottom.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] VerticalRainbowColor<T>(uint width, uint height, T max, PixelFormat pixelFormat, ulong frameNumber = 0) where T : INumber<T>
    {
        T[] image = new T[width * height * 3];

        var colorOrder = pixelFormat.ToString()[..3];

        Color[] colors = GetRainbowColorRange(height);

        // build image array
        for (int i = 0; i < height; i++)
        {
            int a = i - (int)frameNumber;

            for (int j = 0; j < width * 3; j += 3)
            {
                image[(i * width * 3) + j] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(colors[((a % height) + height) % height].R / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T)) : (T)Convert.ChangeType(colors[((a % height) + height) % height].B / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
                image[(i * width * 3) + j + 1] = (T)Convert.ChangeType(colors[((a % height) + height) % height].G / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
                image[(i * width * 3) + j + 2] = colorOrder.Equals("RGB") ? (T)Convert.ChangeType(colors[((a % height) + height) % height].B / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T)) : (T)Convert.ChangeType(colors[((a % height) + height) % height].R / (double)byte.MaxValue * (double)Convert.ChangeType(max, typeof(double)), typeof(T));
            }
        }

        return image;
    }

    /// <summary>
    /// Creates a black image with a horizontal white line moving from top to bottom with every frame.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] HorizontalLineMovingColor<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        var image = new T[width * height * 3];

        int lineRow = (int)Math.Round((height + (double)frameNumber) % height); 

        // build image array
        for (int j = 0; j < width * 3; j += 3)
        {
            // white line
            image[(lineRow * width * 3) + j] = max;
            image[(lineRow * width * 3) + j + 1] = max;
            image[(lineRow * width * 3) + j + 2] = max;
        }

        return image;
    }

    /// <summary>
    /// Creates a black image with a vertical white line moving from left to right.
    /// </summary>
    /// <param name="width">Width of image (number of pixels).</param>
    /// <param name="height">Height of image (number of pixels).</param>
    /// <param name="max">Maximum pixel value.</param>
    /// <param name="frameNumber">Frame number index.</param>
    /// <returns>Test image as byte array (of size <paramref name="width"/> x <paramref name="height"/> x 3).</returns>
    private static T[] VerticalLineMovingColor<T>(uint width, uint height, T max, ulong frameNumber) where T : INumber<T>
    {
        var image = new T[width * height * 3];

        int lineColumn = (int)Math.Round((width + (double)frameNumber) % width);

        // build image array
        for (int i = 0; i < height; i++)
        {
            image[(i * width * 3) + lineColumn * 3] = max;
            image[(i * width * 3) + lineColumn * 3 + 1] = max;
            image[(i * width * 3) + lineColumn * 3 + 2] = max;
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
            colors[j] = ColorConverter.HSL2RGB(i, 1, 0.5);
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
    private static T[] DrawCenteredText<T>(T[] image, uint width, uint height, T max, PixelFormat pixelFormat, string text) where T : INumber<T>
    {
        var mat = new Emgu.CV.Mat((int)height, (int)width, EmguConverter.GetDepthType(pixelFormat), (int)GenICamConverter.GetNumChannels(pixelFormat));
        mat.SetTo(image);

        mat.DrawCenteredText(text, (int)Convert.ChangeType(max, typeof(int)));

        mat.CopyTo(image);
        return image;
    }

    #endregion
}