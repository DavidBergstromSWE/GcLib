using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib;

namespace FusionViewer.Models;

/// <summary>
/// Processes and stores fused image data.
/// </summary>
internal sealed class FusedImageModel : ImageModel, IXmlSerializable
{
    #region Fields

    // backing-fields
    private GcBuffer _channel1Image;
    private GcBuffer _channel2Image;
    private Inter _selectedInterpolationMethod;
    private GeometricCalibration _calibrationData;
    private BorderType _selectedBorderType;
    private ScalingDirection _selectedImageScalingDirection;

    /// <summary>
    /// Indicates that input channel 1 has been filled with an image.
    /// </summary>
    private bool _channel1Filled;

    /// <summary>
    /// Indicates that input channel 2 has been filled with an image.
    /// </summary>
    private bool _channel2Filled;

    /// <summary>
    /// Counts the number of fused output images (since application start).
    /// </summary>
    private long _fusedCounter = 0;

    #endregion

    #region Properties

    /// <summary>
    /// Geometric calibration data for spatial co-registration of input image channels.
    /// </summary>
    public GeometricCalibration CalibrationData
    {
        get => _calibrationData;
        set => SetProperty(ref _calibrationData, value);
    }

    /// <summary>
    /// List of available interpolation methods (for input image resizing).
    /// </summary>
    public static List<Inter> InterpolationMethods => [.. Enum.GetValues<Inter>()];

    /// <summary>
    /// Interpolation method selected (for input image resizing).
    /// </summary>
    public Inter SelectedInterpolationMethod
    {
        get => _selectedInterpolationMethod;
        set => SetProperty(ref _selectedInterpolationMethod, value);
    }

    /// <summary>
    /// Size of output image.
    /// </summary>
    public Size OutputImageSize
    {
        get
        {
            return Channel1Image == null || Channel2Image == null
                ? Size.Empty
                : SelectedImageScalingDirection == ScalingDirection.DownScale
                ? Channel1Image.Width * Channel1Image.Height > Channel2Image.Width * Channel2Image.Height
                    ? new Size(width: (int)Channel2Image.Width, height: (int)Channel2Image.Height)
                    : new Size(width: (int)Channel1Image.Width, height: (int)Channel1Image.Height)
                : Channel1Image.Width * Channel1Image.Height < Channel2Image.Width * Channel2Image.Height
                    ? new Size(width: (int)Channel2Image.Width, height: (int)Channel2Image.Height)
                    : new Size(width: (int)Channel1Image.Width, height: (int)Channel1Image.Height);
        }
    }

    /// <summary>
    /// Available pixel extrapolation methods (for border at image registration).
    /// </summary>
    public static List<BorderType> BorderTypes => [BorderType.Constant, BorderType.Reflect, BorderType.Replicate];

    /// <summary>
    /// Selected pixel extrapolation method (for border at image registration).
    /// </summary>
    public BorderType SelectedBorderType
    {
        get => _selectedBorderType;
        set => SetProperty(ref _selectedBorderType, value);
    }

    /// <summary>
    /// Available image scaling directions.
    /// </summary>
    public static List<ScalingDirection> ImageScalingDirections => [.. Enum.GetValues<ScalingDirection>()];

    /// <summary>
    /// Selected image scaling direction.
    /// </summary>
    public ScalingDirection SelectedImageScalingDirection
    {
        get => _selectedImageScalingDirection;
        set => SetProperty(ref _selectedImageScalingDirection, value);
    }

    /// <summary>
    /// Processed image of input channel 1.
    /// </summary>
    public GcBuffer Channel1Image
    {
        get => _channel1Image;
        set
        {
            _channel1Image = value;
            if (_channel1Image != null)
            {
                _channel1Filled = true;

                // Check if both image channels have been filled.
                if (_channel1Filled && _channel2Filled)
                {
                    try
                    {
                        FuseImages(useColorFuse: false);
                    }
                    catch (Exception ex)
                    {
                        OnProcessingException(ex);
                        _channel1Filled = _channel2Filled = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Processed image of input channel 2.
    /// </summary>
    public GcBuffer Channel2Image
    {
        get => _channel2Image;
        set
        {
            _channel2Image = value;
            if (_channel2Image != null)
            {
                _channel2Filled = true;

                // Check if both image channels have been filled.
                if (_channel1Filled && _channel2Filled)
                {
                    try
                    {
                        FuseImages(useColorFuse: false);
                    }
                    catch (Exception ex)
                    {
                        OnProcessingException(ex);
                        _channel1Filled = _channel2Filled = false;
                    }
                }
            }
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new model for the storage and processing of fused image data.
    /// </summary>
    public FusedImageModel() : base(DisplayChannel.FusedChannel)
    {
        // Default interpolation method.
        SelectedInterpolationMethod = Inter.Linear;

        // Default border type.
        SelectedBorderType = BorderType.Constant;

        // Default image scaling mode.
        SelectedImageScalingDirection = ScalingDirection.DownScale;

        // Initialize calibration data to an identity transformation.
        CalibrationData = new GeometricCalibration();

        IsActive = true;
    }

    #endregion

    #region Public methods

    public override void InitializeSettings()
    {
        base.InitializeSettings();

        CalibrationData = new GeometricCalibration();
    }

    /// <inheritdoc/>
    public override void ReadXml(XmlReader reader)
    {
        // Read basic image settings.
        base.ReadXml(reader);

        // Read selected interpolation method.
        if (reader.Name == nameof(SelectedInterpolationMethod))
        {
            if (Enum.TryParse(reader.ReadElementContentAsString(), ignoreCase: true, out Inter interpolationMethod) && InterpolationMethods.Contains(interpolationMethod))
                SelectedInterpolationMethod = interpolationMethod;
        }
            
        // Read calibration data (if available).
        if (reader.Name == "CalibrationData" && reader.IsEmptyElement == false)
        {
            var deviceIndex = Enum.Parse<DeviceIndex>(reader.GetAttribute("Device"));
            reader.ReadStartElement();

            // Read elements of affine transform matrix.
            XmlReader subReader = reader.ReadSubtree();
            _ = subReader.ReadToFollowing("MatrixElement");
            var matrix = new Matrix<double>(2, 3);
            int row, col;
            do
            {
                row = Convert.ToInt32(subReader.GetAttribute("row"));
                col = Convert.ToInt32(subReader.GetAttribute("col"));
                matrix[row, col] = Convert.ToDouble(subReader.ReadElementContentAsString());
            } while (subReader.Name != nameof(GeometricCalibration.AffineTransform));
            CalibrationData = new GeometricCalibration(deviceIndex, matrix);
            reader.ReadEndElement(); // affine transform
            reader.ReadEndElement(); // calibration data
        }
        else
        {
            reader.ReadStartElement();
        }

        // Read selected border type.
        if (reader.Name == nameof(SelectedBorderType))
        {
            if (Enum.TryParse(reader.ReadElementContentAsString(), ignoreCase: true, out BorderType borderType) && BorderTypes.Contains(borderType))
                SelectedBorderType = borderType;
        }

        // Read selected image scaling direction.
        if (reader.Name == nameof(SelectedImageScalingDirection))
        {
            if (Enum.TryParse(reader.ReadElementContentAsString(), ignoreCase: true, out ScalingDirection scalingDirection) && ImageScalingDirections.Contains(scalingDirection))
                SelectedImageScalingDirection = scalingDirection;
        }
    }

    /// <inheritdoc/>
    public override void WriteXml(XmlWriter writer)
    {
        // Write basic image settings.
        base.WriteXml(writer);

        // Write selected interpolation method.
        writer.WriteElementString(nameof(SelectedInterpolationMethod), SelectedInterpolationMethod.ToString());

        // Write calibration data.
        writer.WriteStartElement("CalibrationData");
        if (_calibrationData != null)
        {
            writer.WriteAttributeString("Device", DeviceIndex.Device1.ToString());

            // Write elements of affine transform matrix.
            writer.WriteStartElement(nameof(_calibrationData.AffineTransform));
            for (int row = 0; row < _calibrationData.AffineTransform.Rows; row++)
            {
                for (int col = 0; col < _calibrationData.AffineTransform.Cols; col++)
                {
                    writer.WriteStartElement("MatrixElement");
                    writer.WriteAttributeString("row", row.ToString());
                    writer.WriteAttributeString("col", col.ToString());
                    writer.WriteString(_calibrationData.AffineTransform[row, col].ToString());
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement(); // affine transform               
        }
        writer.WriteEndElement(); // calibration data

        // Write selected border type.
        writer.WriteElementString(nameof(SelectedBorderType), SelectedBorderType.ToString());

        // Write selected image scaling direction.
        writer.WriteElementString(nameof(SelectedImageScalingDirection), SelectedImageScalingDirection.ToString());
    }

    #endregion

    public override void ClearImages()
    {
        RawImage = null;
        Channel1Image = null;
        Channel2Image = null;
    }

    #region Private methods

    /// <summary>
    /// Fuse the current input channel images into an output fused image, using the selected fusion method.
    /// </summary>
    private void FuseImages()
    {
    }

    /// <summary>
    /// Fuse the current input channel images into an output fused image, using the selected fusion method. 
    /// <para>WARNING: Method is experimental and needs further testing!</para>
    /// </summary>
    /// <param name="useColorFuse">If true, color image fusion will be used. If false, input images will be converted to grayscale.</param>
    private void FuseImages(bool useColorFuse = true)
    {

    }

    /// <summary>
    /// Calibrate images using provided geometric calibration data with selected scaling direction (upscale or downscale).
    /// </summary>
    /// <param name="image1">First image.</param>
    /// <param name="image2">Second image.</param>
    private void CalibrateImages(ref Mat image1, ref Mat image2)
    {
        if (SelectedImageScalingDirection == CalibrationData.ScalingDirection)
        {
            CvInvoke.WarpAffine(src: image1, dst: image1,
                                dsize: new Size(image2.Width, image2.Height),
                                mapMatrix: CalibrationData.AffineTransform,
                                interMethod: SelectedInterpolationMethod, warpMethod: Warp.Default,
                                borderMode: SelectedBorderType, borderValue: default);
        }
        else
        {
            CvInvoke.WarpAffine(src: image2, dst: image2,
                                dsize: new Size(image1.Width, image1.Height),
                                mapMatrix: CalibrationData.AffineTransform,
                                interMethod: SelectedInterpolationMethod, warpMethod: Warp.InverseMap,
                                borderMode: SelectedBorderType, borderValue: default);
        }
    }

    /// <summary>
    /// Converts input image to grayscale image.
    /// </summary>
    /// <param name="mat">Input image.</param>
    /// <returns>Input image converted to grayscale.</returns>
    private static Mat ConvertToGray(Mat mat)
    {
        if (mat.NumberOfChannels == 3)
            CvInvoke.CvtColor(src: mat, dst: mat, code: ColorConversion.Bgr2Gray);
        else if (mat.NumberOfChannels == 4)
            CvInvoke.CvtColor(src: mat, dst: mat, code: ColorConversion.Bgra2Gray);

        return mat;
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Process fused image.
    /// </summary>
    /// <param name="buffer">Input image.</param>
    /// <returns>Processed output image.</returns>
    protected override GcBuffer ProcessImage(GcBuffer buffer)
    {
        // Convert to Mat.
        var mat = buffer.ToMat();

        // Adjust brightness.
        mat += Brightness;

        // Instantiate new output buffer (allocates new memory!).
        var output = new GcBuffer(mat, buffer.PixelDynamicRangeMax, buffer.FrameID, buffer.TimeStamp);

        // Dipose mat.
        mat.Dispose();

        return output;
    }

    #endregion
}