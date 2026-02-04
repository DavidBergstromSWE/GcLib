using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

/// <summary>
/// Collection of extension methods for the <see cref="Mat"/> class. 
/// </summary>
public static class MatExtensions
{
    #region Public methods

    /// <summary>
    /// Get pixel value(s) from specified image coordinate.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <returns>Pixel value(s) from all image channels.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double[] GetPixel(this Mat mat, int row, int col)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, mat.Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, mat.Width, nameof(col));

        double[] value = new double[mat.NumberOfChannels];

        unsafe
        {
            var bytes = new ReadOnlySpan<byte>((void*)(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize), mat.ElementSize);
            for (int i = 0; i < mat.NumberOfChannels; i++)
                value[i] = NumericHelper.SpanToDouble(bytes.Slice(i * mat.ElementSize / mat.NumberOfChannels, mat.ElementSize / mat.NumberOfChannels));
        }

        return value;
    }

    /// <summary>
    /// Get pixel value from specified image coordinate and image channel.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <returns>Pixel value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double GetPixel(this Mat mat, int row, int col, uint channel = 0)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, mat.Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, mat.Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)channel, mat.NumberOfChannels, nameof(channel));

        unsafe
        {
            var bytes = new ReadOnlySpan<byte>((void*)(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize + channel * mat.ElementSize / mat.NumberOfChannels), mat.ElementSize / mat.NumberOfChannels);
            return NumericHelper.SpanToDouble(bytes);
        }
        
    }

    /// <summary>
    /// Set pixel value for a specific channel.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <param name="value">Pixel value.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SetPixel(this Mat mat, int row, int col, uint channel, double value)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, mat.Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, mat.Width, nameof(col));

        // Check that channel number falls within number of channels.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)channel, mat.NumberOfChannels, nameof(channel));

        unsafe
        {
            var bytes = new Span<byte>((void*)(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize + channel * mat.ElementSize / mat.NumberOfChannels), mat.ElementSize / mat.NumberOfChannels);
            NumericHelper.DoubleToSpan(ref bytes, value);
            bytes.CopyTo(new Span<byte>((void*)(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize + channel * mat.ElementSize / mat.NumberOfChannels), bytes.Length));
        }
    }

    /// <summary>
    /// Set pixel value for specified image coordinate.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="row">Row number, zero-based and numbered from top to bottom.</param>
    /// <param name="col">Column number, zero-based and numbered from left to right.</param>
    /// <param name="value">Pixel value.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SetPixel(this Mat mat, int row, int col, double[] value)
    {
        // Check that pixel coordinate falls within image boundaries.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, mat.Height, nameof(row));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, mat.Width, nameof(col));

        unsafe
        {
            var bytes = new Span<byte>((void*)(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize), mat.ElementSize);
            NumericHelper.DoubleToSpan(ref bytes, value);
            bytes.CopyTo(new Span<byte>((void*)(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize), bytes.Length));
        }
    }

    /// <summary>
    /// Draws text in center of image.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="text">Text to be drawn.</param>
    public static void DrawCenteredText(this Mat mat, string text, int grayLevel)
    {
        // Get text size.
        int baseLine = 0;
        var size = CvInvoke.GetTextSize(text: text, fontFace: FontFace.HersheyDuplex, fontScale: 1, thickness: 1, baseLine: ref baseLine);

        // Calculate text position.
        int x = (int)Math.Round((mat.Width - size.Width) / 2.0);
        int y = (int)Math.Round(mat.Height / 2.0);

        // Get white and black levels based on image depth.
        var white = grayLevel;
        var black = EmguConverter.GetMin(mat.Depth);

        // Draw white background rectangle with black border.
        CvInvoke.Rectangle(img: mat, rect: new Rectangle(x: x, y: y - size.Height - 2, width: size.Width, height: size.Height + 5), color: new Emgu.CV.Structure.Bgr(white, white, white).MCvScalar, thickness: -1); // White background
        CvInvoke.Rectangle(img: mat, rect: new Rectangle(x: x, y: y - size.Height - 2, width: size.Width, height: size.Height + 5), color: new Emgu.CV.Structure.Bgr(black, black, black).MCvScalar); // Black border

        // Draw text in rectangle.
        CvInvoke.PutText(img: mat,
                         text: text,
                         org: new Point(x, y),
                         fontFace: FontFace.HersheyDuplex,
                         fontScale: 1,
                         color: new Emgu.CV.Structure.Bgr(black, black, black).MCvScalar, // Black text
                         thickness: 1,
                         lineType: LineType.AntiAlias,
                         bottomLeftOrigin: false);
    }

    /// <summary>
    /// Calculates the gray-scale value distribution of the image, showing the frequency of occurrence of each gray-level value.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="bins">Number of bins (e.g. histogram size).</param>
    /// <param name="maximumValue">Maximum gray-level value.</param>
    /// <param name="histogramData">Output histogram data (can optionally be pre-allocated for performance).</param>
    /// <param name="decimate">Decimate input data (for performance).</param>
    public static void CalculateHistogram(this Mat mat, int bins, uint maximumValue, ref double[,] histogramData, bool decimate = true)
    {
        // Allocate array if necessary.
        histogramData ??= new double[mat.NumberOfChannels, bins];

        // Decimate input data for performance.
        if (decimate && (int)mat.Total > 400000)
            CvInvoke.ResizeForFrame(src: mat, dst: mat, frameSize: new Size(640, 640), interpolationMethod: Inter.Nearest, scaleDownOnly: true);

        // Convert 4-channel image to 3.
        if (mat.NumberOfChannels == 4)
            CvInvoke.CvtColor(mat, mat, ColorConversion.Bgra2Bgr);

        // Split image into array of single-channel (grayscale) images.
        Mat[] matChannels = mat.Split();

        using var histogram = new Mat(bins, 1, DepthType.Cv32F, 1);

        // Calculate histogram for each image channel.
        for (int i = 0; i < mat.NumberOfChannels; i++)
        {
            // Calculate histogram using EmguCV.
            using var vMat = new VectorOfMat(matChannels[i]);
            CvInvoke.CalcHist(images: vMat, channels: [0], mask: null, hist: histogram, histSize: [bins], ranges: [0, maximumValue + 1], accumulate: false);

            // Update histogram data via Span.
            unsafe
            {
                ReadOnlySpan<float> floatSpan = new(histogram.DataPointer.ToPointer(), bins);
                for (int j = 0; j < floatSpan.Length; j++)
                    histogramData[i, j] = floatSpan[j];
            }
        }
    }

    #endregion

    #region Obsolete Methods

    /// <summary>
    /// Get pixel value at specified row and column position in specified channel.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    /// <param name="row">Row position.</param>
    /// <param name="col">Column position.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <returns>Pixel value.</returns>
    [Obsolete(message: "Use GetPixel instead")]
    public static dynamic GetPixelDynamic(this Mat mat, int row, int col, int channel = 0)
    {
        if (row < 0 || row >= mat.Height || col < 0 || col >= mat.Width)
            throw new IndexOutOfRangeException("Pixel position is out of range!");

        if (channel < 0 || channel >= mat.NumberOfChannels)
            throw new IndexOutOfRangeException("Channel is out of range!");

        dynamic value = CreateElement(mat.Depth, mat.NumberOfChannels);
        Marshal.Copy(mat.DataPointer + (((row * mat.Cols) + col) * mat.ElementSize), value, 0, mat.NumberOfChannels);
        return value[channel];
    }

    /// <summary>
    /// Get pixel values at specified row and column position in all channels.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    /// <param name="row">Row position.</param>
    /// <param name="col">Column position.</param>
    /// <returns>Array of pixel values for all channels.</returns>
    [Obsolete(message: "Use GetPixel instead")]
    public static dynamic GetPixelDynamic(this Mat mat, int row, int col)
    {
        if (row < 0 || row >= mat.Height || col < 0 || col >= mat.Width)
            throw new IndexOutOfRangeException("Pixel position is out of range!");

        dynamic value = CreateElement(mat.Depth, mat.NumberOfChannels);
        Marshal.Copy(mat.DataPointer + (((row * mat.Cols) + col) * mat.ElementSize), value, 0, mat.NumberOfChannels);
        return value;
    }

    /// <summary>
    /// Set pixel value at specified row and column position.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    /// <param name="row">Row position.</param>
    /// <param name="col">Column position.</param>
    /// <param name="value">Pixel value.</param>
    [Obsolete("Use SetPixel instead")]
    public static void SetPixelDynamic(this Mat mat, int row, int col, dynamic value)
    {
        if (row < 0 || row >= mat.Height || col < 0 || col >= mat.Width)
            throw new IndexOutOfRangeException("Pixel position is out of range!");

        if (value is Array)
        {
            if (mat.NumberOfChannels != value.Length)
                throw new IndexOutOfRangeException($"Image only has {mat.NumberOfChannels} channels!");

            dynamic element = CreateElement(mat.Depth, mat.NumberOfChannels);

            for (int i = 0; i < mat.NumberOfChannels; i++)
                element[i] = Convert.ChangeType(value[i], element?.GetType().GetElementType());

            Marshal.Copy(element, 0, mat.DataPointer + ((row * mat.Cols + col) * mat.ElementSize), element.Length);

            return;
        }

        dynamic target = CreateElement(mat.Depth, value);
        Marshal.Copy(target, 0, mat.DataPointer + (((row * mat.Cols) + col) * mat.ElementSize), 1);
    }

    /// <summary>
    /// Set pixel value at specified row and column position in specified channel.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    /// <param name="row">Row position.</param>
    /// <param name="col">Column position.</param>
    /// <param name="channel">Channel index (zero-based).</param>
    /// <param name="value">Pixel value.</param>
    [Obsolete("Use SetPixel instead")]
    public static void SetPixelDynamic(this Mat mat, int row, int col, int channel, dynamic value)
    {
        if (row < 0 || row >= mat.Height || col < 0 || col >= mat.Width)
            throw new IndexOutOfRangeException("Pixel position is out of range!");

        if (channel < 0 || channel >= mat.NumberOfChannels)
            throw new IndexOutOfRangeException("Channel is out of range!");

        dynamic target = CreateElement(mat.Depth, value);
        Marshal.Copy(target, 0, mat.DataPointer + (((row * mat.Cols) + col) * mat.ElementSize + channel), 1);
    }

    private static dynamic CreateElement(DepthType depthType, dynamic value)
    {
        dynamic element = CreateElement(depthType, 1);
        dynamic val = Convert.ChangeType(value, element?.GetType().GetElementType());
        element[0] = val;
        return element;
    }

    private static dynamic CreateElement(DepthType depthType, int numChannels)
    {
        return depthType switch
        {
            DepthType.Cv8S => new sbyte[numChannels],
            DepthType.Cv8U => new byte[numChannels],
            DepthType.Cv16S => new short[numChannels],
            DepthType.Cv16U => new ushort[numChannels],
            DepthType.Cv32S => new int[numChannels],
            DepthType.Cv32F => new float[numChannels],
            DepthType.Cv64F => new double[numChannels],
            _ => new float[numChannels]
        };
    }

    #endregion
}