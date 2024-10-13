using System;
using System.Timers;
using System.Windows.Forms;
using GcLib;
using GcLib.Samples.WinFormsDemoApp.Properties;

namespace WinFormsDemoApp.UserControls;

/// <summary>
/// Playback control for viewing previously recorded image content from file.
/// </summary>
/// <remarks>
/// How to use: Register BufferDisplay event and handle buffer displaying in the handler. 
/// </remarks>
public partial class GcPlayBackControl : UserControl
{
    #region Fields

    /// <summary>
    /// Reader of images from file.
    /// </summary>
    private GcBufferReader _imageReader = null;

    /// <summary>
    /// Timer used for continuous image playback.
    /// </summary>
    private System.Timers.Timer _playbackTimer;

    /// <summary>
    /// True if playback control is currently playing (continuously).
    /// </summary>
    private bool _isPlaying;

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a playback control for viewing previously recorded image content.
    /// </summary>
    public GcPlayBackControl()
    {
        InitializeComponent();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Setup control to playback images using specified buffer source.
    /// </summary>
    /// <param name="imageReader">Image buffer source.</param>
    public void Open(GcBufferReader imageReader)
    {
        _imageReader = imageReader;

        if (_imageReader == null)
            throw new ArgumentException("No image source found!");

        // Setup PlaybackPanel and slider.
        if (_imageReader.FrameCount == 0)
            throw new InvalidOperationException("Image sequence is empty!");

        if (_imageReader.FrameCount > 1) // multi-image sequence
        {
            // Set slider properties.
            ColorSlider.Minimum = 0;
            ColorSlider.Maximum = _imageReader.FrameCount - 1;
            ColorSlider.SmallChange = 1;
            ColorSlider.LargeChange = 10;

            // Set slider start position.
            ColorSlider.Value = ColorSlider.Minimum;

            // Register to ValueChanged event in ColorSlider.
            ColorSlider.ValueChanged += OnColorSliderValueChanged;

            // Enable all buttons and slider.              
            EnableButtons();
            ColorSlider.Enabled = true;
            PlayPauseButton.Enabled = true;

            // Initialize timer generating new images at regular intervals corresponding to frame rate.
            _playbackTimer = new System.Timers.Timer { AutoReset = _imageReader.FrameCount > 1 };
            if (_imageReader.FrameRate > 0)
                _playbackTimer.Interval = 1 / _imageReader.FrameRate * 1000; // interval to read images
            else
                _playbackTimer.Interval = 100; // run at 10 Hz if timestamps are all wrong
        }
        else // single image sequence
        {
            // Disable all buttons and slider if sequence only contains one image.
            DisableButtons();
            ColorSlider.Enabled = false;
            PlayPauseButton.Enabled = false;
        }

        // Display first image.
        if (_imageReader.ReadImage(out GcBuffer buffer, 0))
        {
            // Raise event to display image.
            OnBufferDisplay(buffer);

            // Update textbox.
            DisplayPlaybackData(buffer.TimeStamp);
        }

        // Reset Play button.
        PlayPauseButton.BackgroundImage = Resources.Play;
    }

    /// <summary>
    /// Close playback control.
    /// </summary>
    public void Close()
    {
        // Unregister from ValueChanged event in ColorSlider.
        ColorSlider.ValueChanged -= OnColorSliderValueChanged;

        // Stop and dispose timer.
        if (_playbackTimer != null)
        {
            _playbackTimer.Elapsed -= OnTimerElapsed;
            _playbackTimer.Stop();
            _playbackTimer.Dispose();
        }
    }

    /// <summary>
    /// Start continuous playback of images.
    /// </summary>
    private void Play()
    {
        if (_isPlaying)
            Stop();

        // Register to timer event.
        _playbackTimer.Elapsed += OnTimerElapsed;

        // Start timer.
        _playbackTimer.Start();

        // Disable buttons.
        DisableButtons();

        _isPlaying = true;
    }

    /// <summary>
    /// Stop continuous playback of images.
    /// </summary>
    private void Stop()
    {
        // Unregister from timer event.
        _playbackTimer.Elapsed -= OnTimerElapsed;

        // Stop timer.
        _playbackTimer.Stop();

        // Enable buttons.
        EnableButtons();

        _isPlaying = false;

    }

    /// <summary>
    /// Display frame index and time elapsed in control.
    /// </summary>
    /// <param name="timeStamp">Timestamp of image.</param>
    private void DisplayPlaybackData(ulong timeStamp)
    {
        var dateTime = new DateTime((long)timeStamp - (long)_imageReader.GetTimeStamp(0));

        if (PlaybackTextBox.InvokeRequired)
            PlaybackTextBox.Invoke((MethodInvoker)delegate { PlaybackTextBox.Text = $"Frame {_imageReader.FrameIndex} ({dateTime:HH:mm:ss.fff})"; });
        else PlaybackTextBox.Text = $"Frame {_imageReader.FrameIndex} ({dateTime:HH:mm:ss.fff})";
    }

    /// <summary>
    /// Enable buttons and slider.
    /// </summary>
    private void EnableButtons()
    {
        if (InvokeRequired)
            Invoke(new Action(EnableButtons));
        else
        {
            StepBackButton.Enabled = true;
            StepForwardButton.Enabled = true;
            EndButton.Enabled = true;
            StartButton.Enabled = true;
        }
    }

    /// <summary>
    /// Disable buttons and slider.
    /// </summary>
    private void DisableButtons()
    {
        if (InvokeRequired)
            Invoke(new Action(DisableButtons));
        else
        {
            StepBackButton.Enabled = false;
            StepForwardButton.Enabled = false;
            EndButton.Enabled = false;
            StartButton.Enabled = false;
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event-handling method to elapsed events in <see cref="_playbackTimer"/>, reading and displaying next image in the sequence.
    /// </summary>
    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Check end-of-file.
        if (_imageReader.FrameIndex <= _imageReader.FrameCount - 1)
        {
            // Read next image.
            _imageReader.ReadImage(out GcBuffer buffer);

            // Raise event for image to be displayed.
            OnBufferDisplay(buffer);

            // Update textbox.
            DisplayPlaybackData(buffer.TimeStamp);

            // Move slider to new position.
            ColorSlider.Value += 1;
        }
        else
        {
            // Stop at end-of-file.
            Stop();

            // Reset Play button.
            PlayPauseButton.BackgroundImage = Resources.Play;
        }
    }

    /// <summary>
    /// Event announcing that an image buffer needs to be displayed, where the image buffer is supplied with the event.
    /// </summary>
    public event EventHandler<GcBuffer> BufferDisplay;

    /// <summary>
    /// Event-invoking method announcing that an image buffer needs to be displayed, where the image buffer is supplied with the event.
    /// </summary>
    /// <param name="eventArgs">Event arguments with image buffer.</param>
    protected virtual void OnBufferDisplay(GcBuffer buffer)
    {
        BufferDisplay?.Invoke(this, buffer);
    }

    /// <summary>
    /// Event-handling method to ColorSliderValueChanged events, reading image from ImageReader corresponding to slider position and invoking display event.
    /// </summary>
    private void OnColorSliderValueChanged(object sender, EventArgs e)
    {
        // Only update within limits.
        if (ColorSlider.Value >= ColorSlider.Minimum && ColorSlider.Value <= ColorSlider.Maximum)
        {
            // Get frame index from slider position (can this be done through datastream?).
            _imageReader.FrameIndex = (ulong)ColorSlider.Value;

            // Read image with frame index.
            _imageReader.ReadImage(out GcBuffer buffer, _imageReader.FrameIndex);

            // Raise event to display image.
            OnBufferDisplay(buffer);

            // Update textbox.
            DisplayPlaybackData(buffer.TimeStamp);
        }
    }

    /// <summary>
    /// Event-handling method to Start button in PlaybackPanel, moving slider thumb to start position.
    /// </summary>
    private void PlaybackStartButton_Click(object sender, EventArgs e)
    {
        ColorSlider.Value = ColorSlider.Minimum;
    }

    /// <summary>
    /// Event-handling method to End button in PlaybackPanel, moving slider thumb to end position.
    /// </summary>
    private void PlaybackEndButton_Click(object sender, EventArgs e)
    {
        ColorSlider.Value = ColorSlider.Maximum;
    }

    /// <summary>
    /// Event-handling method to StepBackward button in PlaybackPanel, moving slider thumb one step backwards. 
    /// </summary>
    private void PlaybackStepBackButton_Click(object sender, EventArgs e)
    {
        if (ColorSlider.Value > ColorSlider.Minimum)
            ColorSlider.Value -= 1;
    }

    /// <summary>
    /// Event-handling method to StepForward button in PlaybackPanel, moving slider thumb one step forwards. 
    /// </summary>
    private void PlaybackStepForwardButton_Click(object sender, EventArgs e)
    {
        if (ColorSlider.Value < ColorSlider.Maximum)
            ColorSlider.Value += 1;
    }

    /// <summary>
    /// Event-handling method to Play/Pause button, starting/stopping continuous playback of images.
    /// </summary>
    private void PlaybackPlayPauseButton_Click(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            Stop();
            PlayPauseButton.BackgroundImage = Resources.Play;
        }
        else
        {
            Play();
            PlayPauseButton.BackgroundImage = Resources.Pause;
        }
    }

    #endregion  
}
