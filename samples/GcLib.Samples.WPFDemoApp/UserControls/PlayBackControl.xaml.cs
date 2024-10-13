using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;

namespace FusionViewer.UserControls;

/// <summary>
/// Playback control with play/pause and forward/backward stepping buttons, text label and a slider.
/// </summary>
public partial class PlayBackControl : UserControl
{
    #region Public fields

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(PlayBackControl), new PropertyMetadata(false));
    public static readonly DependencyProperty StartIndexProperty = DependencyProperty.Register(nameof(StartIndex), typeof(int), typeof(PlayBackControl), new PropertyMetadata(0));
    public static readonly DependencyProperty EndIndexProperty = DependencyProperty.Register(nameof(EndIndex), typeof(int), typeof(PlayBackControl), new PropertyMetadata(100));
    public static readonly DependencyProperty CurrentIndexProperty = DependencyProperty.Register(nameof(CurrentIndex), typeof(int), typeof(PlayBackControl), new PropertyMetadata(0));
    public static readonly DependencyProperty IsSliderVisibleProperty = DependencyProperty.Register(nameof(IsSliderVisible), typeof(bool), typeof(PlayBackControl), new PropertyMetadata(true));
    public static readonly DependencyProperty SmallIndexChangeProperty = DependencyProperty.Register(nameof(SmallIndexChange), typeof(int), typeof(PlayBackControl), new PropertyMetadata(1));
    public static readonly DependencyProperty LargeIndexChangeProperty = DependencyProperty.Register(nameof(LargeIndexChange), typeof(int), typeof(PlayBackControl), new PropertyMetadata(10));
    public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(PlayBackControl), new PropertyMetadata(null));

    #endregion

    #region Properties

    /// <summary>
    /// True if currently in continuous play mode.
    /// </summary>
    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    /// <summary>
    /// First index.
    /// </summary>
    public int StartIndex
    {
        get { return (int)GetValue(StartIndexProperty); }
        set { SetValue(StartIndexProperty, value); }
    }

    /// <summary>
    /// Final index.
    /// </summary>
    public int EndIndex
    {
        get { return (int)GetValue(EndIndexProperty); }
        set { SetValue(EndIndexProperty, value); }
    }

    /// <summary>
    /// Current index.
    /// </summary>
    public int CurrentIndex
    {
        get { return (int)GetValue(CurrentIndexProperty); }
        set { SetValue(CurrentIndexProperty, value); }
    }

    /// <summary>
    /// Small incremental change of index.
    /// </summary>
    public int SmallIndexChange
    {
        get { return (int)GetValue(SmallIndexChangeProperty); }
        set { SetValue(SmallIndexChangeProperty, value); }
    }

    /// <summary>
    /// Large incremental change of index.
    /// </summary>
    public int LargeIndexChange
    {
        get { return (int)GetValue(LargeIndexChangeProperty); }
        set { SetValue(LargeIndexChangeProperty, value); }
    }

    /// <summary>
    /// Text shown in label.
    /// </summary>
    public string LabelText
    {
        get { return (string)GetValue(LabelTextProperty); }
        set { SetValue(LabelTextProperty, value); }
    }


    /// <summary>
    /// True if slider should be visible, false if it is to be collapsed/hidden.
    /// </summary>
    public bool IsSliderVisible
    {
        get { return (bool)GetValue(IsSliderVisibleProperty); }
        set { SetValue(IsSliderVisibleProperty, value); }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a request invoked by a UI command to jump to first index.
    /// </summary>
    public IRelayCommand GoToStartCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to jump to last index.
    /// </summary>
    public IRelayCommand GoToEndCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to step one index position back.
    /// </summary>
    public IRelayCommand StepBackCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to step a large index change back.
    /// </summary>
    public IRelayCommand StepLargeBackCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to step one index position forward.
    /// </summary>
    public IRelayCommand StepForwardCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to step a large index change forward.
    /// </summary>
    public IRelayCommand StepLargeForwardCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to toggle between start and pause.
    /// </summary>
    public IRelayCommand PlayPauseToggleCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new control for playback of recorded image data.
    /// </summary>
    public PlayBackControl()
    {
        // Instantiate playback commands.
        GoToStartCommand = new RelayCommand(GoToStart);
        GoToEndCommand = new RelayCommand(GoToEnd);
        StepBackCommand = new RelayCommand(StepBack);
        StepForwardCommand = new RelayCommand(StepForward);
        PlayPauseToggleCommand = new RelayCommand(PlayPauseToggle);
        StepLargeForwardCommand = new RelayCommand(StepLargeForward);
        StepLargeBackCommand = new RelayCommand(StepLargeBack);

        InitializeComponent();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Go to first image in sequence.
    /// </summary>
    private void GoToStart()
    {
        CurrentIndex = StartIndex;
    }

    /// <summary>
    /// Go to final image in sequence.
    /// </summary>
    private void GoToEnd()
    {
        CurrentIndex = EndIndex;
    }

    /// <summary>
    /// Step one image back.
    /// </summary>
    private void StepBack()
    {
        // Move slider to new position.
        if (CurrentIndex > StartIndex)
            CurrentIndex -= 1;
    }

    /// <summary>
    /// Step a larger change in image sequence forward.
    /// </summary>
    private void StepLargeBack()
    {
        if (CurrentIndex > StartIndex + LargeIndexChange)
            CurrentIndex -= LargeIndexChange;
        else
            CurrentIndex = StartIndex;
    }

    /// <summary>
    /// Step one image forward.
    /// </summary>
    private void StepForward()
    {
        // Move slider to new position.
        if (CurrentIndex < EndIndex)
            CurrentIndex += 1;
    }

    /// <summary>
    /// Step a larger change in image sequence back.
    /// </summary>
    private void StepLargeForward()
    {
        if (CurrentIndex < EndIndex - LargeIndexChange)
            CurrentIndex += LargeIndexChange;
        else
            CurrentIndex = EndIndex;
    }

    /// <summary>
    /// Toggles between playing or pausing.
    /// </summary>
    private void PlayPauseToggle()
    {
        IsPlaying = !IsPlaying;
    }

    #endregion
}