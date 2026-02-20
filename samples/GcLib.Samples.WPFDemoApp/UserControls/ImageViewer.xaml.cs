using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using GcLib;
using GcLib.Utilities.Collections;
using Microsoft.Win32;

namespace ImagerViewer.UserControls;

/// <summary>
/// Control for displaying image content inside a scrollviewer, with pan and zoom and pixel info inspection functionality.
/// </summary>
public partial class ImageViewer : UserControl, INotifyPropertyChanged
{
    /// <summary>
    /// Event raised when the view of the image in the <see cref="ImageViewer"/> control has been changed.
    /// </summary>
    public event EventHandler ViewChanged;

    /// <summary>
    /// Number of samples to use in calculation of average frame rate.
    /// </summary>
    private const int NUMSAMPLES = 20;

    #region Private fields

    /// <summary>
    /// Last mouse position over target.
    /// </summary>
    private Point? _lastMousePositionOnTarget;

    /// <summary>
    /// Last mouse position of a dragging operation.
    /// </summary>
    private Point? _lastDragPoint;

    /// <summary>
    /// Circular buffer of timestamps used for frame rate calculations.
    /// </summary>
    private readonly CircularBuffer<ulong> _timeStamps = new(NUMSAMPLES, true);

    // backing-fields
    private uint _pixelCoordinateX;
    private uint _pixelCoordinateY;
    private double[] _pixelValue;
    private double _fps;
    private double? _scale;
    private System.Windows.Visibility _selectionBoxVisibility;
    private double _selectionBoxLeft;
    private double _selectionBoxTop;
    private double _selectionBoxWidth;
    private double _selectionBoxHeight;

    #endregion

    #region Public fields

    public static readonly DependencyProperty ToolbarVisibilityProperty = DependencyProperty.Register(nameof(ToolbarVisibility), typeof(System.Windows.Visibility), typeof(ImageViewer), new PropertyMetadata(System.Windows.Visibility.Visible));
    public static readonly DependencyProperty ShowPixelInspectorProperty = DependencyProperty.Register(nameof(ShowPixelInspector), typeof(bool), typeof(ImageViewer), new PropertyMetadata(true));
    public static readonly DependencyProperty ImageBorderBrushProperty = DependencyProperty.Register(nameof(ImageBorderBrush), typeof(Brush), typeof(ImageViewer), new PropertyMetadata(Brushes.Black));
    public static readonly DependencyProperty ImageBackgroundProperty = DependencyProperty.Register(nameof(ImageBackground), typeof(Brush), typeof(ImageViewer), new PropertyMetadata(Brushes.Transparent));
    public static readonly DependencyProperty BitmapScalingProperty = DependencyProperty.Register(nameof(BitmapScaling), typeof(BitmapScalingMode), typeof(ImageViewer), new PropertyMetadata(BitmapScalingMode.Linear));
    public static readonly DependencyProperty DisplayedImageProperty = DependencyProperty.Register(nameof(DisplayedImage), typeof(BitmapSource), typeof(ImageViewer), new PropertyMetadata(null, OnDisplayedImageChanged));
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(GcBuffer), typeof(ImageViewer), new PropertyMetadata(null, OnImageSourceChanged));
    public static readonly DependencyProperty ShowImageMetaDataProperty = DependencyProperty.Register(nameof(ShowImageMetaData), typeof(bool), typeof(ImageViewer), new PropertyMetadata(true));
    public static readonly DependencyProperty ShowImageFormatProperty = DependencyProperty.Register(nameof(ShowImageFormat), typeof(bool), typeof(ImageViewer), new PropertyMetadata(true));
    public static readonly DependencyProperty MouseWheelZoomingEnabledProperty = DependencyProperty.Register(nameof(MouseWheelZoomingEnabled), typeof(bool), typeof(ImageViewer), new PropertyMetadata(true));
    public static readonly DependencyProperty SelectionBoxZoomingEnabledProperty = DependencyProperty.Register(nameof(SelectionBoxZoomingEnabled), typeof(bool), typeof(ImageViewer), new PropertyMetadata(true));
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(ImageViewer), new PropertyMetadata(Stretch.Uniform, OnStretchChanged));

    #endregion

    #region DependencyProperties

    /// <summary>
    /// Visibility of toolbar.
    /// </summary>
    public System.Windows.Visibility ToolbarVisibility
    {
        get => (System.Windows.Visibility)GetValue(ToolbarVisibilityProperty);
        set => SetValue(ToolbarVisibilityProperty, value);
    }

    /// <summary>
    /// Color of image border.
    /// </summary>
    public Brush ImageBorderBrush
    {
        get => (Brush)GetValue(ImageBorderBrushProperty);
        set => SetValue(ImageBorderBrushProperty, value);
    }

    /// <summary>
    /// Color of image background.
    /// </summary>
    public Brush ImageBackground
    {
        get => (Brush)GetValue(ImageBackgroundProperty);
        set => SetValue(ImageBackgroundProperty, value);
    }

    /// <summary>
    /// Resampling algorithm for scaling displayed images.
    /// </summary>
    public BitmapScalingMode BitmapScaling
    {
        get => (BitmapScalingMode)GetValue(BitmapScalingProperty);
        set => SetValue(BitmapScalingProperty, value);
    }


    /// <summary>
    /// Stretch mode for displayed image.
    /// </summary>
    public Stretch Stretch
    {
        get { return (Stretch)GetValue(StretchProperty); }
        set { SetValue(StretchProperty, value); }
    }

    /// <summary>
    /// Image that is displayed. Image should be a processed and 8-bit converted version of <see cref="ImageSource"/>.
    /// </summary>
    public BitmapSource DisplayedImage
    {
        get => (BitmapSource)GetValue(DisplayedImageProperty);
        set => SetValue(DisplayedImageProperty, value);
    }

    /// <summary>
    /// Raw (unprocessed) image source.
    /// </summary>
    public GcBuffer ImageSource
    {
        get => (GcBuffer)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    /// <summary>
    /// True if pixel value inspector is enabled.
    /// </summary>
    public bool ShowPixelInspector
    {
        get => (bool)GetValue(ShowPixelInspectorProperty);
        set => SetValue(ShowPixelInspectorProperty, value);
    }

    /// <summary>
    /// True if image meta data (frame ID, timestamp and frame rate) should be shown.
    /// </summary>
    public bool ShowImageMetaData
    {
        get { return (bool)GetValue(ShowImageMetaDataProperty); }
        set { SetValue(ShowImageMetaDataProperty, value); }
    }

    /// <summary>
    /// True if image format (resolution and pixel format) should be shown.
    /// </summary>
    public bool ShowImageFormat
    {
        get { return (bool)GetValue(ShowImageFormatProperty); }
        set { SetValue(ShowImageFormatProperty, value); }
    }

    /// <summary>
    /// True if mouse wheel for zooming in and out is enabled.
    /// </summary>
    public bool MouseWheelZoomingEnabled
    {
        get => (bool)GetValue(MouseWheelZoomingEnabledProperty);
        set => SetValue(MouseWheelZoomingEnabledProperty, value);
    }

    /// <summary>
    /// True if selection box for zooming (using middle mouse button) is enabled.
    /// </summary>
    public bool SelectionBoxZoomingEnabled
    {
        get { return (bool)GetValue(SelectionBoxZoomingEnabledProperty); }
        set { SetValue(SelectionBoxZoomingEnabledProperty, value); }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Horizontal (x) coordinate of inspected pixel.
    /// </summary>
    public uint PixelCoordinateX
    {
        get => _pixelCoordinateX;
        private set
        {
            _pixelCoordinateX = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Vertical (y) coordinate of inspected pixel.
    /// </summary>
    public uint PixelCoordinateY
    {
        get => _pixelCoordinateY;
        private set
        {
            _pixelCoordinateY = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gray or BGR value of inspected pixel.
    /// </summary>
    public double[] PixelValue
    {
        get => _pixelValue;
        private set
        {
            _pixelValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Average frame rate (calculated).
    /// </summary>
    public double FPS
    {
        get => _fps;
        private set
        {
            _fps = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Scale of displayed image.
    /// </summary>
    public double? Scale
    {
        get => _scale;
        set
        {
            if (value != _scale)
            {
                _scale = value;
                if (_scale != null)
                    ScaleTransform.ScaleX = ScaleTransform.ScaleY = (double)_scale;

                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Visibility of selection box.
    /// </summary>
    public System.Windows.Visibility SelectionBoxVisibility
    {
        get => _selectionBoxVisibility;
        private set
        {
            _selectionBoxVisibility = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Left position of selection box.
    /// </summary>
    public double SelectionBoxLeft
    {
        get => _selectionBoxLeft;
        private set
        {
            _selectionBoxLeft = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Top position of selection box.
    /// </summary>
    public double SelectionBoxTop
    {
        get => _selectionBoxTop;
        private set
        {
            _selectionBoxTop = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Width of selection box.
    /// </summary>
    public double SelectionBoxWidth
    {
        get => _selectionBoxWidth;
        private set
        {
            _selectionBoxWidth = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Height of selection box.
    /// </summary>
    public double SelectionBoxHeight
    {
        get => _selectionBoxHeight;
        private set
        {
            _selectionBoxHeight = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a command invoked from UI to change stretch mode of image.
    /// </summary>
    public IRelayCommand<Stretch> StretchImageCommand { get; }

    /// <summary>
    /// Relays a command invoked from UI to copy displayed image to clipboard.
    /// </summary>
    public IRelayCommand CopyDisplayedImageCommand { get; }

    /// <summary>
    /// Relays a command invoked from UI to save displayed image to disk.
    /// </summary>
    public IRelayCommand SaveDisplayedImageCommand { get; }

    #endregion

    #region Constructors

    public ImageViewer()
    {
        // Instantiate commands.
        StretchImageCommand = new RelayCommand<Stretch>(StretchImage);
        CopyDisplayedImageCommand = new RelayCommand(() => Clipboard.SetImage(DisplayedImage), () => image != null || image.Source != null);
        SaveDisplayedImageCommand = new RelayCommand(SaveDisplayedImage, () => image != null || image.Source != null);

        InitializeComponent();
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Invokes the PropertyChanged event when a property value has been changed.
    /// </summary>
    /// <param name="propertyName">Name of property that has been changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region PropertyChangedCallbacks

    /// <summary>
    /// Eventhandler method to changes of <see cref="DisplayedImage"/>, updating scale factor.
    /// </summary>
    private static void OnDisplayedImageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var iv = sender as ImageViewer;

        if (e.NewValue is not BitmapSource bitmap)
        {
            // Reset stretch mode and scale when image is cleared.
            if (iv.ImageSource is null)
            {
                iv.Stretch = Stretch.Uniform;
                iv.image.Stretch = Stretch.Uniform;
                iv.Scale = null;

                // Reset other channels.
                iv.OnViewChanged();
            }

            // Do not show scale.
            iv.Scale = null;

            return;
        }

        // Update scale.
        if (iv.Stretch is Stretch.Uniform)
            iv.Scale = Math.Min(iv.scrollViewer.ActualWidth / bitmap.Width, iv.scrollViewer.ActualHeight / bitmap.Height);
        else iv.Scale = iv.ScaleTransform.ScaleX;
    }

    /// <summary>
    /// Eventhandler method to changes of <see cref="ImageSource"/>, updating image chunkdata and inspected pixel data.
    /// </summary>
    private static void OnImageSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var iv = sender as ImageViewer;
        if (e.NewValue is not GcBuffer newImage)
            return;

        // Add image timestamp to buffer.
        iv._timeStamps.Put(newImage.TimeStamp);

        // Update frame rate.
        iv.FPS = iv.CalcFPS();

        if (iv.ShowPixelInspector)
        {
            var mousePos = Mouse.GetPosition(iv.image);

            if (iv.IsOutOfBounds(mousePos))
                return;

            // Update pixel coordinate and values.
            iv.UpdatePixel(mousePos);
        }
    }

    /// <summary>
    /// Eventhandler method to changes of <see cref="Stretch"/>, updating stretch mode of <see cref="image"/> control.
    /// </summary>
    private static void OnStretchChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var iv = sender as ImageViewer;
        if (e.NewValue is Stretch stretch && stretch != iv.Stretch && (stretch is Stretch.Uniform || stretch is Stretch.None))
            iv.StretchImage(stretch);
    }

    #endregion

    #region Eventhandlers

    /// <summary>
    /// Eventhandler method to mouse pointer entering image, replacing mouse cursor with crosshair cursor.
    /// </summary>
    private void Image_MouseEnter(object sender, MouseEventArgs e)
    {
        if (ShowPixelInspector && e.MiddleButton != MouseButtonState.Pressed)
            Mouse.OverrideCursor = Cursors.Cross;
    }

    /// <summary>
    /// Eventhandler method to mouse pointer leaving image, restoring default mouse cursor.
    /// </summary>
    private void Image_MouseLeave(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = e.MiddleButton == MouseButtonState.Pressed ? Cursors.Pen : null;
    }

    /// <summary>
    /// Eventhandler to left mouse button clicking events over the image, where single clicks starts panning of the image or moving the double-clicked position to the viewport center of the <see cref="ScrollViewer"/>.
    /// </summary>
    private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (scrollViewer.ScrollableHeight == 0 && scrollViewer.ScrollableWidth == 0)
            return;

        // Retrieve clicked position.
        var mousePos = e.GetPosition(scrollViewer);

        // Center to double-clicked position.
        if (e.ClickCount == 2)
        {
            // Translate position to center of viewport.
            double Xmove = scrollViewer.ActualWidth / 2 - mousePos.X;
            double Ymove = scrollViewer.ActualHeight / 2 - mousePos.Y;

            // Scroll to new position.
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - Xmove);
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - Ymove);

            // Raise view changed event.
            OnViewChanged();

            e.Handled = true;
            return;
        }

        // Enter panning mode.
        if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
        {
            _lastDragPoint = mousePos;

            Mouse.OverrideCursor = Cursors.Hand;
            _ = Mouse.Capture(image);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Eventhandler to right mouse button clicking events over the image, cancelling panning and zooming if active.
    /// </summary>
    private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Cancel panning and zooming if right mouse button is pressed.
        if (e.ChangedButton == MouseButton.Right)
        {
            // Delete selection box.
            SelectionBoxVisibility = System.Windows.Visibility.Hidden;

            // Reset positions.
            _lastDragPoint = null;
            _lastMousePositionOnTarget = null;

            // Reset mouse settings.
            image.ReleaseMouseCapture();
            Mouse.OverrideCursor = Cursors.Cross;

            e.Handled = true;
        }
    }

    /// <summary>
    /// Eventhandler to mouse click events over image.
    /// </summary>
    private void Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Selection mode.
        if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed && SelectionBoxZoomingEnabled)
        {
            _lastMousePositionOnTarget = e.GetPosition(scrollViewer);

            // Set initial placement of selection box.
            SelectionBoxLeft = _lastMousePositionOnTarget.Value.X;
            SelectionBoxTop = _lastMousePositionOnTarget.Value.Y;
            SelectionBoxWidth = 0;
            SelectionBoxHeight = 0;
            SelectionBoxVisibility = System.Windows.Visibility.Visible;

            Mouse.OverrideCursor = Cursors.Pen;
            _ = Mouse.Capture(image);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Eventhandler method to mouse pointer movements over image.
    /// </summary>
    private void Image_MouseMove(object sender, MouseEventArgs e)
    {
        // Check out-of-bounds condition.
        var imagePos = e.GetPosition(image);

        if (ShowPixelInspector)
        {
            // Update pixel coordinate and values.
            UpdatePixel(imagePos);
        }

        // Panning mode.
        if (e.LeftButton == MouseButtonState.Pressed && _lastDragPoint.HasValue)
        {
            var currentDragPoint = e.GetPosition(scrollViewer);

            double dX = currentDragPoint.X - _lastDragPoint.Value.X;
            double dY = currentDragPoint.Y - _lastDragPoint.Value.Y;

            _lastDragPoint = currentDragPoint;

            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);

            return;
        }

        // Selection mode.
        if (e.MiddleButton == MouseButtonState.Pressed && _lastMousePositionOnTarget.HasValue && SelectionBoxZoomingEnabled)
        {
            // Retrieve current mouse position.
            var currentMousePositionOnTarget = e.GetPosition(scrollViewer);

            if (IsOutOfBounds(imagePos) || currentMousePositionOnTarget.X < 0 || currentMousePositionOnTarget.X > scrollViewer.ViewportWidth || currentMousePositionOnTarget.Y < 0 || currentMousePositionOnTarget.Y > scrollViewer.ViewportHeight)
                return;

            // Update drawing of selection box.
            SelectionBoxLeft = _lastMousePositionOnTarget.Value.X < currentMousePositionOnTarget.X ? _lastMousePositionOnTarget.Value.X : currentMousePositionOnTarget.X;
            SelectionBoxTop = _lastMousePositionOnTarget.Value.Y < currentMousePositionOnTarget.Y ? _lastMousePositionOnTarget.Value.Y : currentMousePositionOnTarget.Y;
            SelectionBoxWidth = Math.Abs(currentMousePositionOnTarget.X - _lastMousePositionOnTarget.Value.X);
            SelectionBoxHeight = Math.Abs(currentMousePositionOnTarget.Y - _lastMousePositionOnTarget.Value.Y);
        }
    }

    /// <summary>
    /// Eventhandler method to mouse button release events over image.
    /// </summary>
    private void Image_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (image.IsMouseCaptured == false)
            return;

        // Panning mode.
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Released && e.Handled == false)
        {
            // Reset dragging position.
            _lastDragPoint = null;

            // Reset mouse settings.
            image.ReleaseMouseCapture();
            Mouse.OverrideCursor = Cursors.Cross;

            // Raise view changed event.
            OnViewChanged();

            return;
        }

        // Selection mode.
        if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Released && e.Handled == false && SelectionBoxZoomingEnabled)
        {
            // Retrieve width and height of selection box (in Canvas/ScrollViewer coordinates).
            var width = SelectionBoxWidth;
            var height = SelectionBoxHeight;

            if (height > 0 && width > 0)
            {
                // Get center position of selection box in Image control coordinates.
                var centerPos = GetImagePosition(selectionBox, new Point(width / 2, height / 2));

                // Calculate new scale.
                var deltaZoom = Math.Min(scrollViewer.ViewportHeight / height, scrollViewer.ViewportWidth / width);

                // Stretch image to new scale.
                Stretch = Stretch.None;
                image.Stretch = Stretch.None;
                Scale *= deltaZoom;

                // Update ScrollViewer.
                scrollViewer.UpdateLayout();

                // Calculate offsets needed to scroll content to the center of viewport.
                var verticalOffset = centerPos.Y * deltaZoom - scrollViewer.ViewportHeight / 2;
                var horizontalOffset = centerPos.X * deltaZoom - scrollViewer.ViewportWidth / 2;

                // Scroll to new position.
                scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
                scrollViewer.ScrollToVerticalOffset(verticalOffset);
            }

            // Reset selection box.            
            SelectionBoxLeft = 0;
            SelectionBoxTop = 0;
            SelectionBoxWidth = 0;
            SelectionBoxHeight = 0;

            // Hide selection box.
            SelectionBoxVisibility = System.Windows.Visibility.Hidden;

            // Reset selection position.
            _lastMousePositionOnTarget = null;

            // Reset mouse settings.
            image.ReleaseMouseCapture();
            Mouse.OverrideCursor = Cursors.Cross;

            // Raise view changed event.
            OnViewChanged();

            e.Handled = true;
        }
    }

    /// <summary>
    /// Eventhandler method to mouse wheel rotation over image, changing scale by zooming in or out.
    /// </summary>
    private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (MouseWheelZoomingEnabled == false || image.Source == null)
            return;

        _lastMousePositionOnTarget = Mouse.GetPosition(image);

        if (Stretch == Stretch.Uniform)
        {
            Stretch = Stretch.None;
            image.Stretch = Stretch.None;
            Scale = Math.Min(scrollViewer.ExtentHeight / image.Source.Height, scrollViewer.ExtentWidth / image.Source.Width);
        }

        // Determine upscaling or downscaling of image.
        double zoom = e.Delta > 0 ? 1.1 : 1 / 1.1;

        // Update scale.
        Scale *= zoom;

        Point? targetBefore = _lastMousePositionOnTarget;
        image.UpdateLayout();
        Point? targetNow = Mouse.GetPosition(image);
        _lastMousePositionOnTarget = null;

        if (targetBefore.HasValue && image.Source != null)
        {
            double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
            double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;
            double newOffsetX = scrollViewer.HorizontalOffset - (dXInTargetPixels * (double)Scale);
            double newOffsetY = scrollViewer.VerticalOffset - (dYInTargetPixels * (double)Scale);

            if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                return;

            scrollViewer.ScrollToHorizontalOffset(newOffsetX);
            scrollViewer.ScrollToVerticalOffset(newOffsetY);
        }

        // Raise view changed event.
        OnViewChanged();

        e.Handled = true;
    }

    /// <summary>
    /// Eventhandler method to scrollbar scrolling.
    /// </summary>
    private void ScrollViewer_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
    {
        // Raise view changed event when scrolling is finished.
        if (e.ScrollEventType == System.Windows.Controls.Primitives.ScrollEventType.EndScroll)
            OnViewChanged();
    }

    /// <summary>
    /// Eventhandler method to <see cref="ZoomInButton"/> being clicked, upscaling image by a fixed amount.
    /// </summary>
    private void OnZoomInButton(object sender, RoutedEventArgs e)
    {
        if (image?.Source == null)
            return;

        if (image.Stretch == Stretch.Uniform)
        {
            Stretch = Stretch.None;
            image.Stretch = Stretch.None;
            Scale = Math.Min(scrollViewer.ExtentHeight / image.Source.Height, scrollViewer.ExtentWidth / image.Source.Width);
        }

        var scrollViewerCenter = new Point(scrollViewer.ActualWidth / 2, scrollViewer.ActualHeight / 2);
        var oldCenter = scrollViewer.TranslatePoint(scrollViewerCenter, image);

        // Update scale.
        Scale *= 1.1;

        scrollViewer.UpdateLayout();

        var newCenter = scrollViewer.TranslatePoint(scrollViewerCenter, image);

        // Calculate center shift.
        double dXInTargetPixels = newCenter.X - oldCenter.X;
        double dYInTargetPixels = newCenter.Y - oldCenter.Y;
        double newOffsetX = scrollViewer.HorizontalOffset - (dXInTargetPixels * (double)Scale);
        double newOffsetY = scrollViewer.VerticalOffset - (dYInTargetPixels * (double)Scale);

        scrollViewer.ScrollToHorizontalOffset(newOffsetX);
        scrollViewer.ScrollToVerticalOffset(newOffsetY);

        // Raise view changed event.
        OnViewChanged();
    }

    /// <summary>
    /// Eventhandler method to <see cref="ZoomOutButton"/> being clicked, downscaling image by a fixed amount.
    /// </summary>
    private void OnZoomOutButton(object sender, RoutedEventArgs e)
    {
        if (image?.Source == null)
            return;

        if (image.Stretch == Stretch.Uniform)
        {
            Stretch = Stretch.None;
            image.Stretch = Stretch.None;
            Scale = Math.Min(scrollViewer.ExtentHeight / image.Source.Height, scrollViewer.ExtentWidth / image.Source.Width);
        }

        var scrollViewerCenter = new Point(scrollViewer.ActualWidth / 2, scrollViewer.ActualHeight / 2);
        var oldCenter = scrollViewer.TranslatePoint(scrollViewerCenter, image);

        // Update scale.
        Scale /= 1.1;

        scrollViewer.UpdateLayout();

        var newCenter = scrollViewer.TranslatePoint(scrollViewerCenter, image);

        // Calculate center shift.
        double dXInTargetPixels = newCenter.X - oldCenter.X;
        double dYInTargetPixels = newCenter.Y - oldCenter.Y;
        double newOffsetX = scrollViewer.HorizontalOffset - (dXInTargetPixels * (double)Scale);
        double newOffsetY = scrollViewer.VerticalOffset - (dYInTargetPixels * (double)Scale);

        scrollViewer.ScrollToHorizontalOffset(newOffsetX);
        scrollViewer.ScrollToVerticalOffset(newOffsetY);

        // Raise view changed event.
        OnViewChanged();
    }

    /// <summary>
    /// Eventhandler to size changes of control.
    /// </summary>
    private void ImageViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Re-inforce stretch mode.
        if (Stretch == Stretch.Uniform)
            StretchImage(Stretch.Uniform);
        else if (image.Source is not null && scrollViewer.ExtentWidth > 0)
            // Re-calculate scale.
            Scale = scrollViewer.ExtentWidth / image.Source.Width;
    }

    /// <summary>
    /// Invokes the <see cref="ViewChanged"/> event, when the view of the image has been changed.
    /// </summary>
    private void OnViewChanged()
    {
        scrollViewer.UpdateLayout();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Retrieves position of a <see cref="UIElement"/> in <see cref="Image"/> control coordinates.
    /// </summary>
    /// <param name="element">UI element.</param>
    /// <param name="position">Position in element.</param>
    /// <returns>Position in <see cref="Image"/> control.</returns>
    private Point GetImagePosition(UIElement element, Point position)
    {
        // Translate point relative to Image control.
        var imagePos = element.TranslatePoint(position, image);

        // If Image has been transformed, ActualWidth and ActualHeight will be incorrect and scaling must done using DesiredSize.
        if (image.ActualWidth != image.DesiredSize.Width || image.ActualHeight != image.DesiredSize.Height)
        {
            imagePos.X *= image.DesiredSize.Width / image.ActualWidth;
            imagePos.Y *= image.DesiredSize.Height / image.ActualHeight;
        }

        return imagePos;
    }

    /// <summary>
    /// Updates coordinates and value for pixel at specified position over <see cref="Image"/> control.
    /// </summary>
    /// <param name="position">Position relative to <see cref="Image"/> control.</param>
    private void UpdatePixel(Point position)
    {
        if (image.Source is null)
            return;

        // Get current scale.
        double scaleX = image.ActualWidth / image.Source.Width;
        double scaleY = image.ActualHeight / image.Source.Height;

        // Update pixel coordinates.
        PixelCoordinateX = (uint)Math.Floor(position.X / scaleX);
        PixelCoordinateY = (uint)Math.Floor(position.Y / scaleY);

        if (PixelCoordinateX >= 0 && PixelCoordinateX < ImageSource.Width && PixelCoordinateY >= 0 && PixelCoordinateY < ImageSource.Height)
        {
            // Update displayed pixel value from coordinates.
            if (ImageSource.PixelFormat.ToString()[..3].Equals("RGB", StringComparison.CurrentCultureIgnoreCase)) // RGB
                PixelValue = [ImageSource[PixelCoordinateY, PixelCoordinateX][2], ImageSource[PixelCoordinateY, PixelCoordinateX][1], ImageSource[PixelCoordinateY, PixelCoordinateX][0]];
            else PixelValue = ImageSource[PixelCoordinateY, PixelCoordinateX]; // Grayscale or BGR
        }
    }

    /// <summary>
    /// Checks if position is outside bounds of Image control.
    /// </summary>
    /// <param name="pos">Position.</param>
    /// <returns>True if position is outside control, false if it is within.</returns>
    private bool IsOutOfBounds(Point pos)
    {
        return pos.X < 0 || pos.Y < 0 || pos.X >= image.ActualWidth || pos.Y >= image.ActualHeight;
    }

    /// <summary>
    /// Opens a file dialog and saves currently displayed image to specified png file.
    /// </summary>
    private void SaveDisplayedImage()
    {
        // Copy image to clipboard.
        Clipboard.SetImage(DisplayedImage);

        var saveFileDialog = new SaveFileDialog
        {
            Filter = "PNG image|*.png",
            DefaultExt = "png",
            Title = "Save image",
            FileName = $"Image_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            // Encode image to Png.
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));

            // Save Png to file.
            using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
            encoder.Save(fileStream);
        }

        // Clear data from clipboard.
        Clipboard.Clear();
    }

    /// <summary>
    /// Changes stretch mode of image.
    /// </summary>
    /// <param name="stretch">Stretch mode to apply.</param>
    private void StretchImage(Stretch stretch)
    {
        // Do nothing if no image is shown.
        if (image?.Source is null)
            return;

        // Change stretch mode.
        Stretch = stretch;
        image.Stretch = stretch;

        // Scale image.
        if (stretch == Stretch.Uniform)
            Scale = Math.Min(scrollViewer.ActualWidth / image.Source.Width, scrollViewer.ActualHeight / image.Source.Height);
        else if (stretch == Stretch.None)
            Scale = 1.0;

        // Raise view changed event.
        OnViewChanged();
    }

    /// <summary>
    /// Calculates effective frame rate (Hz) from received timestamps.
    /// </summary>
    /// <returns>Estimated number of frames per second.</returns>
    private double CalcFPS()
    {
        if (_timeStamps.Size > 1)
            return TimeSpan.TicksPerSecond / (_timeStamps.Max() - (double)_timeStamps.Min()) * (_timeStamps.Size - 1);
        else return 0.0;
    }

    #endregion
}