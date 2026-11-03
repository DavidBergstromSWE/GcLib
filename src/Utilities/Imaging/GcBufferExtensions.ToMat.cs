using System;
using Emgu.CV;
using GcLib.Utilities.Numbers;

namespace GcLib.Utilities.Imaging;

public static partial class GcBufferExtensions
{
    /// <summary>
    /// Exports image data in buffer to <see cref="Mat"/> format. If buffer contains color data, the method converts it to a BGR(a) format compatible with image processing in <see cref="Emgu.CV"/>.
    /// </summary>
    /// <remarks>
    /// Note: Conversion allocates new memory to store the image data in <see cref="Mat"/> format. 
    /// The method scales the image data to match bit depth and channel order expected by <see cref="Emgu.CV"/>. For example, if the buffer contains RGB data, it will be converted to BGR format.
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
            case Emgu.CV.CvEnum.DepthType.Cv32U:
                mat.SetTo(NumericHelper.ToArray<uint>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv64U:
                mat.SetTo(NumericHelper.ToArray<ulong>(buffer.ImageData));
                break;
            case Emgu.CV.CvEnum.DepthType.Cv32F:
            case Emgu.CV.CvEnum.DepthType.Cv64F:
            case Emgu.CV.CvEnum.DepthType.Cv16F:
            case Emgu.CV.CvEnum.DepthType.CvBool:
            case Emgu.CV.CvEnum.DepthType.Cv8S:
            case Emgu.CV.CvEnum.DepthType.Cv16S:
            case Emgu.CV.CvEnum.DepthType.Cv16BF:
            case Emgu.CV.CvEnum.DepthType.Cv32S:
            case Emgu.CV.CvEnum.DepthType.Cv64S:
            case Emgu.CV.CvEnum.DepthType.Default:
            default:
                throw new NotSupportedException($"Mat depth {mat.Depth} is not supported!");
        }

        // As Emgu CV assumes image data is stored in BGR(a) channel order, convert if necessary (preserving bit depth and number of channels).
        mat = EmguHelper.ConvertMat(mat: mat, sourcePixelFormat: buffer.PixelFormat, targetPixelFormat: EmguHelper.GetPixelFormat(mat.Depth, mat.NumberOfChannels));

        return mat;
    }
}
