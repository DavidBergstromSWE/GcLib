using System;
using Emgu.CV;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

public static partial class GcBufferExtensions
{
    /// <summary>
    /// Exports image data in buffer to <see cref="Mat"/> format. The method converts image data to a Mono or BGR(a) format compatible with image processing in <see cref="Emgu.CV"/>.
    /// </summary>
    /// <remarks>
    /// Note: Conversion allocates new memory to store the image data in <see cref="Mat"/> format. 
    /// The method preserves the bit depth and number of channels of the original image data, but may change the channel order to match the expected format in <see cref="Emgu.CV"/> (e.g., BGR instead of RGB).
    /// </remarks>
    /// <param name="buffer">The buffer containing the image data to convert.</param>
    /// <returns><see cref="Mat"/> image.</returns>
    public static Mat ToMat(this GcBuffer buffer)
    {
        // Convert to Mat with compatible depth and number of channels.
        var mat = new Mat(rows: (int)buffer.Height, cols: (int)buffer.Width, EmguHelper.GetDepthType(buffer.PixelFormat), (int)buffer.NumChannels);

        // Copy image data to Mat based on depth type.
        switch (mat.Depth)
        {
            case Emgu.CV.CvEnum.DepthType.Cv8U:
                mat.SetTo(buffer.ImageData);
                break;
            case Emgu.CV.CvEnum.DepthType.Cv16U:
                mat.SetTo(NumericHelper.ToArray<ushort>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv8S:
                mat.SetTo(NumericHelper.ToArray<sbyte>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv16S:
                mat.SetTo(NumericHelper.ToArray<short>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv32S:
                mat.SetTo(NumericHelper.ToArray<int>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv32U:
                mat.SetTo(NumericHelper.ToArray<uint>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv32F:
                mat.SetTo(NumericHelper.ToArray<float>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv64F:
                mat.SetTo(NumericHelper.ToArray<double>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv16F:
                mat.SetTo(NumericHelper.ToArray<Half>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.CvBool:
                mat.SetTo(NumericHelper.ToArray<int>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv64U:
                mat.SetTo(NumericHelper.ToArray<ulong>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv64S:
                mat.SetTo(NumericHelper.ToArray<long>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv16BF:
            case Emgu.CV.CvEnum.DepthType.Default:
            default:
                throw new NotSupportedException($"Mat depth {mat.Depth} is not supported!");
        }

        // As Emgu CV assumes image data is stored in BGR(a) channel order, convert if necessary (preserving bit depth and number of channels).
        mat = EmguHelper.ConvertMat(mat, buffer.PixelFormat, EmguHelper.GetPixelFormat(mat.Depth, mat.NumberOfChannels));

        return mat;
    }
}
