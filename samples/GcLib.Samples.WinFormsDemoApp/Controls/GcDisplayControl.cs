using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using GcLib;
using GcLib.Utilities.Collections;
using System;
using System.Drawing;
using System.Linq;
using System.ComponentModel;

namespace WinFormsDemoApp.Controls;

/// <summary>
/// Display control to view image content, based on <see cref="ImageBox"/> control.
/// </summary>     
public partial class GcDisplayControl : ImageBox
{
    /// <summary>
    /// Instantiates a display control to be used for viewing image content in UI.
    /// </summary>
    public GcDisplayControl() : base()
    {
        _timeStamps = new CircularBuffer<ulong>(30, true); // will use last 30 frames for calculating FPS.
    }

    #region Fields

    /// <summary>
    /// Circular buffer of timestamps used for frame rate calculation.
    /// </summary>
    private readonly CircularBuffer<ulong> _timeStamps;

    // Backing field.
    private double _fps;

    #endregion

    #region Properties

    /// <summary>
    /// Currently displayed frame rate.
    /// </summary>
    public double FPS
    {
        get
        {
            if (_timeStamps.Size > 1)
            {
                _fps = CalcFPS([.. _timeStamps]);
                if (double.IsInfinity(_fps) == false)
                    return _fps;
            }
            return 0.0;
        }
    }

    /// <summary>
    /// Indicates whether frame ID will be shown as a text overlay in top left corner of displayed image.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowFrameID { get; set; } = false;

    /// <summary>
    /// Indicates whether image timestamp will be shown as a text overlay in top right corner of displayed image. 
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowTimeStamp { get; set; } = false;

    /// <summary>
    /// Indicates whether frame rate will be shown as a text overlay in bottom center of displayed image. 
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowFPS { get; set; } = false;

    /// <summary>
    /// Color of text overlays.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TextOverlayColor { get; set; } = Color.Black;

    /// <summary>
    /// Font size of text overlays.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FontFace TextOverlayFont { get; set; } = FontFace.HersheyPlain;

    #endregion

    #region Public methods

    /// <summary>
    /// Reset stats used for e.g. calculating frame rate.
    /// </summary>
    public void ResetStats()
    {
        _timeStamps.Clear();
    }

    /// <summary>
    /// Display image in control.
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    public void DisplayImage(GcBuffer buffer)
    {
        // Convert to Mat.
        var mat = buffer.ToMat();

        // Resize for ImageBox.
        CvInvoke.ResizeForFrame(mat, mat, Size, Inter.Nearest);

        // Convert to 8-bit.
        if (buffer.NumChannels == 1 && buffer.BitDepth > 8)
            CvInvoke.Normalize(mat, mat, 0, 255, NormType.MinMax, DepthType.Cv8U);

        // Convert 4-channel image to 3-channel RGB.
        if (buffer.NumChannels == 4)
            mat = mat.ToImage<Rgb, byte>().Mat;

        // Add timestamp to circular buffer.
        _timeStamps.Put(buffer.TimeStamp);

        // Add text overlays as requested.
        mat = OverlayChunkData(mat, buffer.FrameID, buffer.TimeStamp);

        // Display image.
        Image = mat;
    }

    /// <summary>
    /// Display image in control.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    /// <param name="frameID">Frame ID of image.</param>
    /// <param name="timeStamp">Timestamp of image (in PC ticks).</param>
    public void DisplayImage(Mat mat, long frameID, ulong timeStamp)
    {
        // Add timestamp to circular buffer.
        _timeStamps.Put(timeStamp);

        // Add requested text overlays.
        mat = OverlayChunkData(mat, frameID, timeStamp);

        // Display final mat image.
        Image = mat;
    }

    /// <summary>
    /// Display image in control.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    public void DisplayImage(Mat mat)
    {
        // Display final mat image.
        Image = mat;
    }

    /// <summary>
    /// Clears control.
    /// </summary>
    public void Clear()
    {
        Image = null;
        BackColor = Color.Black;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Add text overlay to image.
    /// </summary>
    /// <param name="mat">Mat image.</param>
    /// <param name="frameID">Frame ID of image.</param>
    /// <param name="timeStamp">Image timestamp.</param>
    /// <returns>Mat image with text overlayed.</returns>
    private Mat OverlayChunkData(Mat mat, long frameID, ulong timeStamp)
    {
        // Display frame ID in top left corner of image (if requested).
        if (ShowFrameID)
            _ = AddText(mat, frameID.ToString(), ContentAlignment.TopLeft);

        // Display timestamp in top right corner of image (if requested).
        if (ShowTimeStamp)
            _ = AddText(mat, new DateTime((long)timeStamp).ToString("HH:mm:ss.fff"), ContentAlignment.TopRight);

        // Add displayed frame rate to bottom center of image.
        if (ShowFPS)
            _ = AddText(mat, FPS.ToString("0.0"), ContentAlignment.BottomCenter);

        return mat;
    }

    /// <summary>
    /// Adds text overlay to image at specified location.
    /// </summary>
    /// <param name="mat">Mat input image.</param>
    /// <param name="text">Text to be displayed.</param>
    /// <param name="contentAlignment">Location in image.</param>
    /// <returns>Mat output image (with text overlayed).</returns>
    private Mat AddText(Mat mat, string text, ContentAlignment contentAlignment)
    {
        if (mat == null)
            return null;

        if (string.IsNullOrEmpty(text))
            return mat;

        // Calculate width and height of text to be displayed.
        double fontScale = 1.0; int thickness = 1; int baseLine = 0;
        Size textSize = CvInvoke.GetTextSize(text: text, fontFace: TextOverlayFont, fontScale: fontScale, thickness: thickness, baseLine: ref baseLine);

        // Calculate position (x,y) and size (width & height) of background rectangle.
        int x = 0, y = 0, width = textSize.Width + 4, height = textSize.Height + 4;
        switch (contentAlignment)
        {
            case ContentAlignment.TopLeft:
                x = 0; y = 0;
                break;
            case ContentAlignment.TopCenter:
                x = mat.Width / 2 - width / 2; y = 0;
                break;
            case ContentAlignment.TopRight:
                x = mat.Width - width - 1; y = 0;
                break;
            case ContentAlignment.MiddleLeft:
                x = 0; y = mat.Height / 2 - height / 2;
                break;
            case ContentAlignment.MiddleCenter:
                x = mat.Width / 2 - width / 2; y = mat.Height / 2 - height / 2;
                break;
            case ContentAlignment.MiddleRight:
                x = mat.Width - width - 1; y = mat.Height / 2 - height / 2;
                break;
            case ContentAlignment.BottomLeft:
                x = 0; y = mat.Height - height - 1;
                break;
            case ContentAlignment.BottomCenter:
                x = mat.Width / 2 - width / 2; y = mat.Height - height - 1;
                break;
            case ContentAlignment.BottomRight:
                x = mat.Width - width - 1; y = mat.Height - height - 1;
                break;
        }

        // Draw white background rectangle with black border.
        CvInvoke.Rectangle(mat, new Rectangle(x, y, width, height), new Bgr(Color.White).MCvScalar, -1);
        CvInvoke.Rectangle(mat, new Rectangle(x, y, width, height), new Bgr(Color.Black).MCvScalar);

        // Add text to image.
        CvInvoke.PutText(img: mat, text: text, org: new Point(x + 2, y + textSize.Height + 2), fontFace: TextOverlayFont, fontScale: fontScale, color: new Bgr(TextOverlayColor).MCvScalar, thickness);

        return mat;
    }

    /// <summary>
    /// Calculates effective frame rate based on timestamp circular buffer.
    /// </summary>
    /// <param name="timeStamps"></param>
    /// <returns></returns>
    private static double CalcFPS(ulong[] timeStamps)
    {
        ulong[] array = [.. timeStamps.Where(x => x > 0)];
        return (double)TimeSpan.TicksPerSecond / (array.Max() - array.Min()) * (array.Length - 1);
    }

    #endregion

}