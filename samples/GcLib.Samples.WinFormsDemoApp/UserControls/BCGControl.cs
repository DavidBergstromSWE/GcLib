using System.Windows.Forms;
using System.Drawing;
using GcLib;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using GcLib.Utilities.Threading;

namespace WinFormsDemoApp;

/// <summary>
/// (Incomplete) Control to adjust brightness and contrast of images, based on a histogram view. (work-in-progress)
/// </summary>
public partial class BCGControl : UserControl
{
    private DenseHistogram histogram;

    private GcProcessingThread _displayThread;

    public BCGControl()
    {
        InitializeComponent();
        HistogramBox.BackColor = Color.Black;
    }

    public void Open(GcProcessingThread displayThread)
    {
        _displayThread = displayThread;
        _displayThread.BufferProcess += OnBufferDisplay;
    }

    private void OnBufferDisplay(object sender, GcBuffer buffer)
    {
        Mat mat = buffer.ToMat();

        histogram?.Clear();

        if (buffer.NumChannels == 1)
        {
            if (buffer.BitDepth <= 8)
            {
                histogram = new DenseHistogram(byte.MaxValue + 1, new RangeF(0, byte.MaxValue));
                histogram.Calculate(new Image<Gray, byte>[] { mat.ToImage<Gray, byte>() }, false, null);
            }
            else
            {
                histogram = new DenseHistogram(ushort.MaxValue + 1, new RangeF(0, ushort.MaxValue));
                histogram.Calculate(new Image<Gray, ushort>[] { mat.ToImage<Gray, ushort>() }, false, null);
            }
        }
        else
        {
            histogram = new DenseHistogram([byte.MaxValue + 1, byte.MaxValue + 1, byte.MaxValue + 1], [new(0, byte.MaxValue), new(0, byte.MaxValue), new(0, byte.MaxValue)]);
            histogram.Calculate(new Image<Gray, byte>[] { mat.ToImage<Bgr, byte>().Convert<Gray, byte>() }, false, null);
        }

        //HistogramBox.GenerateHistogram("test", Color.White, histogram, byte.MaxValue + 1, new float[] { 0F, 255F });
        HistogramBox.GenerateHistograms(histogram, 256);
        //float[] GrayHist = new float[256];

        //DenseHistogram histogram = new DenseHistogram(255, new RangeF(0, 255));
        //histogram.Calculate<byte>(new Image<Gray, byte>[] { mat.ToImage<Gray, byte>() }, true, null);

        //HistogramBox.GenerateHistogram("test", Color.White, mat, 255, new float[] { 0.0F, 255.0F });
        //HistogramBox.GenerateHistograms(mat, 256);
    }

    public void Close()
    {
        _displayThread.BufferProcess -= OnBufferDisplay;
    }
}
