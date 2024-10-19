using System.Windows.Controls;
using System.Windows.Media;
using ImagerViewer.UserControls;

namespace ImagerViewer.Views;

/// <summary>
/// Interaction logic for ImageDisplayView.xaml
/// </summary>
public partial class ImageDisplayView : UserControl
{
    /// <summary>
    /// Stores settings for an <see cref="ImagerViewer"/> control.
    /// </summary>
    /// <remarks>
    /// Creates a stored setting for an <see cref="ImagerViewer"/> control.
    /// </remarks>
    /// <param name="stretch">Stretch mode for displayed image.</param>
    /// <param name="scale">Scale of displayed image.</param>
    /// <param name="verticalOffset">Scrolling vertical offset of displayed image.</param>
    /// <param name="horizontalOffset">Scrolling horizontal offset of displayed image.</param>
    private struct ViewerSettings(Stretch stretch, double? scale, double verticalOffset, double horizontalOffset)
    {
        /// <summary>
        /// Stretch mode for displayed image.
        /// </summary>
        public Stretch Stretch { get; set; } = stretch;

        /// <summary>
        /// Scale of displayed image.
        /// </summary>
        public double? Scale { get; set; } = scale;

        /// <summary>
        /// Scrolling vertical offset of displayed image.
        /// </summary>
        public double VerticalOffset { get; set; } = verticalOffset;

        /// <summary>
        /// Scrolling horizontal offset of displayed image.
        /// </summary>
        public double HorizontalOffset { get; set; } = horizontalOffset;
    }

    public ImageDisplayView()
    {
        InitializeComponent();
    }
}