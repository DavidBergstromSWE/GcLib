using System;
using System.Windows.Forms;
using GcLib;
using GcLib.Utilities.Threading;
using System.ComponentModel;

namespace WinFormsDemoApp;

/// <summary>
/// Simple control for showing image streaming, displaying and recording status.
/// </summary>
public partial class StatusControl : UserControl
{
    // backing-field
    private GcProcessingThread _displayThread = null;

    /// <summary>
    /// Datastream source for status display.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public GcDataStream DataStream { get; set; }

    /// <summary>
    /// Display thread source for status display.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public GcProcessingThread DisplayThread
    {
        get => _displayThread;
        set
        {
            if (value != null)
            {
                _displayThread = value;

                // Hook display event to display thread.
                _displayThread.BufferProcess += OnBufferDisplay;
            }
        }
    }

    /// <summary>
    /// Recording source for status display.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public GcBufferWriter ImageWriter { get; set; }

    /// <summary>
    /// Instantiates a simple control for showing image data streaming, displaying and recording status in UI.
    /// </summary>
    public StatusControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Display current stats in control.
    /// </summary>
    private void DisplayStats()
    {
        if (InvokeRequired)
            Invoke(new Action(DisplayStats));
        else
        {
            if (DataStream != null)
            {
                ImagesDropped.Text = DataStream.LostFrameCount.ToString();
                ImagesGrabbed.Text = DataStream.DeliveredFrameCount.ToString();
                FrameRateCurrent.Text = DataStream.FrameRate.ToString("0.0");
                FrameRateAverage.Text = DataStream.FrameRateAverage.ToString("0.0");
                OutputBufferQueueSize.Text = DataStream.AwaitDeliveryCount.ToString();
            }

            if (DisplayThread != null)
                DisplayQueueSize.Text = DisplayThread.QueuedCount.ToString();

            if (ImageWriter != null)
                RecordingQueueSize.Text = ImageWriter.BuffersQueued.ToString();
        }
    }

    /// <summary>
    /// Callback delegating buffer display.
    /// </summary>
    private void OnBufferDisplay(object sender, GcBuffer buffer) => DisplayStats();
}