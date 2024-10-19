using System;
using Emgu.CV;

namespace ImagerViewer;

/// <summary>
/// Represents data used in spatial co-registration of two imaging channels.
/// </summary>
internal sealed class GeometricCalibration
{
    #region Properties

    /// <summary>
    /// Affine transformation matrix (2x3) for spatial registration of two image channels. 
    /// </summary>
    /// <remarks>Note: Channel 1 is always the target for the transformation.</remarks>
    public Matrix<double> AffineTransform { get; }

    /// <summary>
    /// Provides easy access to the inverse affine transform.
    /// </summary>
    public Matrix<double> InverseTransform { get; }

    /// <summary>
    /// Scaling direction of the affine transformation (upscale/downscale).
    /// </summary>
    public ScalingDirection ScalingDirection => AffineTransform[0, 0] < 1 ? ScalingDirection.DownScale : ScalingDirection.UpScale;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates an empty calibration for spatial co-registration of input channels, represented by an identity affine transformation matrix.
    /// </summary>
    public GeometricCalibration()
    {
        // Create identity matrix.
        AffineTransform = new Matrix<double>(2, 3);
        AffineTransform[0, 0] = 1;
        AffineTransform[0, 1] = 0;
        AffineTransform[0, 2] = 0;
        AffineTransform[1, 0] = 0;
        AffineTransform[1, 1] = 1;
        AffineTransform[1, 2] = 0;

        InverseTransform = new Matrix<double>(AffineTransform.Size);
        CvInvoke.InvertAffineTransform(AffineTransform, InverseTransform);
    }

    /// <summary>
    /// Creates a new calibration for spatial co-registration of input channels. 
    /// <para>
    /// Note: Internally, <see cref="DeviceIndex.Device1"/> is always used as the target for the transformation. If the target of the input transform is set to <see cref="DeviceIndex.Device2"/>, the input transform will be inverted.
    /// </para>
    /// </summary>
    /// <param name="deviceIndex">Target channel for transformation.</param>
    /// <param name="affineTransform">Affine transformation matrix (2x3).</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public GeometricCalibration(DeviceIndex deviceIndex, Matrix<double> affineTransform) : this()
    {
        ArgumentNullException.ThrowIfNull(affineTransform);
        if (affineTransform.Rows != 2)
            throw new ArgumentException("Affine transformation matrix must have two rows!", nameof(affineTransform));
        if (affineTransform.Cols != 3)
            throw new ArgumentException("Affine transformation matrix must have three columns!", nameof(affineTransform));

        // Always use Device1 as target.
        if (deviceIndex == DeviceIndex.Device1)
        {
            AffineTransform = affineTransform;
            InverseTransform = new Matrix<double>(AffineTransform.Size);
            CvInvoke.InvertAffineTransform(AffineTransform, InverseTransform);
        }
        else // Invert transform if Device2 is specified as target.
        {
            InverseTransform = affineTransform;
            CvInvoke.InvertAffineTransform(InverseTransform, AffineTransform);
        }
    }

    #endregion
}