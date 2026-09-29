using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

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

        // Check that depth type of mat can be cast to double.
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64S, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64U, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");

        double[] value = new double[mat.NumberOfChannels];

        unsafe
        {        
            byte* pixelPtr = (byte*)mat.DataPointer + ((row * mat.Cols + col) * mat.ElementSize); // Pointer to pixel memory address.
            int bytesPerChannel = mat.ElementSize / mat.NumberOfChannels;

            for (int i = 0; i < mat.NumberOfChannels; i++)
            {
                byte* channelPtr = pixelPtr + i * bytesPerChannel; // Pointer to specific channel of pixel.

                value[i] = mat.Depth switch
                {
                    DepthType.CvBool => (*channelPtr) != 0 ? 1.0 : 0.0,
                    DepthType.Cv8S => *(sbyte*)channelPtr,
                    DepthType.Cv8U => *channelPtr,
                    DepthType.Cv16S => *(short*)channelPtr,
                    DepthType.Cv16U => *(ushort*)channelPtr,
                    DepthType.Cv16F => (double)BitConverter.ToHalf(new(channelPtr, bytesPerChannel)),
                    DepthType.Cv32S => *(int*)channelPtr,
                    DepthType.Cv32U => *(uint*)channelPtr,
                    DepthType.Cv32F => (double)*(float*)channelPtr,
                    DepthType.Cv64F => *(double*)channelPtr,
                    _ => throw new NotSupportedException($"Depth type {mat.Depth} is not supported!"),
                };
            }
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

        // Check that depth type can be cast to double.
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64S, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64U, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");

        unsafe
        {
            byte* pixelPtr = (byte*)mat.DataPointer + ((row * mat.Cols + col) * mat.ElementSize); // Pointer to pixel memory address.
            int bytesPerChannel = mat.ElementSize / mat.NumberOfChannels;
            byte* channelPtr = pixelPtr + ((int)channel) * bytesPerChannel; // Pointer to specific channel of pixel.

            return mat.Depth switch
            {
                DepthType.CvBool => (*channelPtr) != 0 ? 1.0 : 0.0,
                DepthType.Cv8S => *(sbyte*)channelPtr,
                DepthType.Cv8U => *channelPtr,
                DepthType.Cv16S => *(short*)channelPtr,
                DepthType.Cv16U => *(ushort*)channelPtr,
                DepthType.Cv16F => (double)BitConverter.ToHalf(new(channelPtr, bytesPerChannel)),
                DepthType.Cv32S => *(int*)channelPtr,
                DepthType.Cv32U => *(uint*)channelPtr,
                DepthType.Cv32F => (double)*(float*)channelPtr,
                DepthType.Cv64F => *(double*)channelPtr,
                _ => throw new NotSupportedException($"Depth type {mat.Depth} is not supported!"),
            };
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

        // Check that depth type can be cast to double.
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64S, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64U, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");

        unsafe
        {
            byte* pixelPtr = (byte*)mat.DataPointer + ((row * mat.Cols + col) * mat.ElementSize); // Pointer to pixel memory address.
            int bytesPerChannel = mat.ElementSize / mat.NumberOfChannels;
            byte* channelPtr = pixelPtr + ((int)channel) * bytesPerChannel; // Pointer to specific channel of pixel.

            switch (mat.Depth)
            {
                case DepthType.CvBool:
                    *channelPtr = (byte)(Convert.ToBoolean(value) ? 1 : 0);
                    break;
                case DepthType.Cv8S:
                    *(sbyte*)channelPtr = Convert.ToSByte(value);
                    break;
                case DepthType.Cv8U:
                    *channelPtr = Convert.ToByte(value);
                    break;
                case DepthType.Cv16S:
                    *(short*)channelPtr = Convert.ToInt16(value);
                    break;
                case DepthType.Cv16U:
                    *(ushort*)channelPtr = Convert.ToUInt16(value);
                    break;
                case DepthType.Cv16F:
                    *(Half*)channelPtr = (Half)value;
                    break;
                case DepthType.Cv32S:
                    *(int*)channelPtr = Convert.ToInt32(value);
                    break;
                case DepthType.Cv32U:
                    *(uint*)channelPtr = Convert.ToUInt32(value);
                    break;
                case DepthType.Cv32F:
                    *(float*)channelPtr = Convert.ToSingle(value);
                    break;
                case DepthType.Cv64F:
                    *(double*)channelPtr = value;
                    break;
                default:
                    throw new NotSupportedException($"Depth type {mat.Depth} is not supported!");
            }
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

        // Check that pixel value matches number of channels.
        ArgumentOutOfRangeException.ThrowIfNotEqual(value.Length, mat.NumberOfChannels, nameof(value));

        // Check that depth type can be cast to double.
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64S, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");
        ArgumentOutOfRangeException.ThrowIfEqual(mat.Depth, DepthType.Cv64U, $"Depth type {mat.Depth} is not supported as it cannot be cast to a {nameof(Double)} without loosing precison!");

        unsafe
        {
            byte* pixelPtr = (byte*)mat.DataPointer + row * (long)mat.Step + col * (long)mat.ElementSize; // Pointer to pixel memory address.
            int bytesPerChannel = mat.ElementSize / mat.NumberOfChannels;

            for (int i = 0; i < value.Length; i++)
            {
                byte* channelPtr = pixelPtr + i * bytesPerChannel; // Pointer to specific channel of pixel.

                switch (mat.Depth)
                {
                    case DepthType.CvBool:
                        *channelPtr = (byte)(Convert.ToBoolean(value[i]) ? 1 : 0);
                        break;
                    case DepthType.Cv8S:
                        *(sbyte*)channelPtr = Convert.ToSByte(value[i]);
                        break;
                    case DepthType.Cv8U:
                        *channelPtr = Convert.ToByte(value[i]);
                        break;
                    case DepthType.Cv16S:
                        *(short*)channelPtr = Convert.ToInt16(value[i]);
                        break;
                    case DepthType.Cv16U:
                        *(ushort*)channelPtr = Convert.ToUInt16(value[i]);
                        break;
                    case DepthType.Cv16F:
                        *(Half*)channelPtr = (Half)value[i];
                        break;
                    case DepthType.Cv32S:
                        *(int*)channelPtr = Convert.ToInt32(value[i]);
                        break;
                    case DepthType.Cv32U:
                        *(uint*)channelPtr = Convert.ToUInt32(value[i]);
                        break;
                    case DepthType.Cv32F:
                        *(float*)channelPtr = Convert.ToSingle(value[i]);
                        break;
                    case DepthType.Cv64F:
                        *(double*)channelPtr = value[i];
                        break;
                    default:
                        throw new NotSupportedException($"Depth type {mat.Depth} is not supported!");
                }
            }
        }
    }

    /// <summary>
    /// Renders text in center of image.
    /// </summary>
    /// <param name="mat">Image.</param>
    /// <param name="text">Text to be drawn.</param>
    /// <param name="foreground">Pixel value to draw text foreground with.</param>
    /// <param name="background">Pixel value to draw text background with.</param>
    public static void DrawCenteredText(this Mat mat, string text, int foreground, int background)
    {
        // Get text size.
        int baseLine = 0;
        var font = HersheyFonts.Complex;
        var fontScale = 1.0;
        var textSize = CvInvoke.GetTextSize(text: text, fontFace: font, fontScale: fontScale, thickness: 1, baseLine: ref baseLine);

        // Create rectangle background for text.
        int borderSize = 2;
        var rectSize = textSize + 2 * new Size(borderSize, borderSize);
        Point topLeft = new((int)Math.Round(mat.Width / 2.0 - rectSize.Width / 2.0), (int)Math.Round(mat.Height / 2.0 - rectSize.Height / 2.0));

        // Draw rectangle with border.
        CvInvoke.Rectangle(img: mat, rect: new Rectangle(topLeft, rectSize), color: new Bgr(background, background, background).MCvScalar, thickness: -1); // rectangle for text background
        CvInvoke.Rectangle(img: mat, rect: new Rectangle(topLeft, rectSize), color: new Bgr(foreground, foreground, foreground).MCvScalar); // border

        // Create a temporary blank 8-bit mask of the same size.
        using Mat mask = Mat.Zeros(mat.Rows, mat.Cols, DepthType.Cv8U, 1);

        // Draw text in pure white (255) onto the 8-bit mask.
        CvInvoke.PutText(img: mask,
                         text: text,
                         org: new Point((int)Math.Round(mat.Width / 2.0 - textSize.Width / 2.0), (int)Math.Round(mat.Height / 2.0 + textSize.Height / 2.0 - borderSize * 2)),
                         fontFace: font,
                         fontScale: fontScale,
                         color: new Bgr(255, 255, 255).MCvScalar,
                         thickness: 1,
                         lineType: LineType.AntiAlias, 
                         bottomLeftOrigin: false);

        // Paint black onto the image using the mask (this only applies the value where text pixels exist).
        mat.SetTo(new MCvScalar(foreground), mask);
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