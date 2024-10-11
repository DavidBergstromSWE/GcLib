using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GcLib;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp : Form
{
    /// <summary>
    /// Event-handling method delegating a buffer display task.
    /// </summary>
    private void OnBufferDisplay(object sender, GcBuffer buffer)
    {
        DisplayImage(buffer);
    }

    /// <summary>
    /// Display image in display control.
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    private void DisplayImage(GcBuffer buffer)
    {
        // Convert to Mat.
        var mat = buffer.ToMat();

        // Add image processing here...

        // Resize for DisplayControl.
        CvInvoke.ResizeForFrame(mat, mat, DisplayControl.Size, Inter.Nearest);

        // Convert higher bit to 8-bit.
        if (mat.NumberOfChannels == 1)
        {
            // Apply linear min-max contrast stretch.
            CvInvoke.Normalize(mat, mat, 0, 255, NormType.MinMax, DepthType.Cv8U);

            // alternative:
            //RangeF rangeF = mat.GetValueRange();
            //mat.ConvertTo(mat, DepthType.Cv8U, 255.0/(rangeF.Max - rangeF.Min), -255.0*rangeF.Min/(rangeF.Max - rangeF.Min));
        }

        // Convert 4-channel image (with alpha channel) to 3-channel RGB
        if (mat.NumberOfChannels == 4)
        {
            CvInvoke.CvtColor(mat, mat, ColorConversion.Bgra2Bgr);
        }

        // Extract chunk data (for text overlay).
        long frameID = buffer.FrameID;
        ulong timeStamp = buffer.TimeStamp;

        // Check if invoking is required (e.g. caller is on different thread than main UI thread).
        if (DisplayControl.InvokeRequired)
        {
            if (_stopInvoking == false)
            {
                _invokeInProgress = true;

                // Invoke display control method on UI thread.
                _ = DisplayControl.BeginInvoke((MethodInvoker)delegate { DisplayControl.DisplayImage(mat, frameID, timeStamp); }); // does not leak memory anymore?

                _invokeInProgress = false;
            }
        }
        else
        {
            DisplayControl.DisplayImage(mat, frameID, timeStamp);
        };
    }
}
