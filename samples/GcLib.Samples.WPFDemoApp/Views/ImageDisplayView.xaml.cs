using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FusionViewer.UserControls;
using FusionViewer.ViewModels;

namespace FusionViewer.Views;

/// <summary>
/// Interaction logic for ImageDisplayView.xaml
/// </summary>
public partial class ImageDisplayView : UserControl
{
    /// <summary>
    /// Stores settings for an <see cref="ImageViewer"/> control.
    /// </summary>
    /// <remarks>
    /// Creates a stored setting for an <see cref="ImageViewer"/> control.
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

    /// <summary>
    /// True if views are currently being synchronized.
    /// </summary>
    private bool isSynchronizingViews;

    /// <summary>
    /// Cached settings for viewer of channel 1.
    /// </summary>
    private ViewerSettings _channel1ViewerSettings;

    /// <summary>
    /// Cached settings for viewer of channel 2.
    /// </summary>
    private ViewerSettings _channel2ViewerSettings;

    /// <summary>
    /// Cached settings for viewer of fused channel.
    /// </summary>
    private ViewerSettings _fusedChannelViewerSettings;

    public ImageDisplayView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Synchronizes views based on settings from the <see cref="ImageViewer"/> raising the <see cref="ImageViewer.ViewChanged"/> event.
    /// </summary>
    /// <param name="sender"><see cref="ImageViewer"/> object.</param>
    /// <param name="e">Event arguments (empty).</param>
    private void SynchronizeViews(object sender, EventArgs e)
    {
        if (DataContext is null)
            return;

        // No synchronization if all channels are to be displayed.
        var viewModel = DataContext as ImageDisplayViewModel;
        if (viewModel.SelectedDisplayChannel == DisplayChannel.All || viewModel.SynchronizeViews == false)
            return;

        var viewer = sender as ImageViewer;

        if (isSynchronizingViews == false) 
        {
            isSynchronizingViews = true;

            // Settings to be synchronized.
            var stretch = viewer.Stretch; // stretching mode
            var scale = viewer.Scale; // image scaling
            var verticalOffset = viewer.scrollViewer.VerticalOffset; // vertical offset of scrolled content
            var horizontalOffset = viewer.scrollViewer.HorizontalOffset; // horizontal offset of scrolled content

            if (viewer != channel1Viewer && scale != null)
            {
                channel1Viewer.Stretch = stretch;
                channel1Viewer.image.Stretch = stretch;
                channel1Viewer.Scale = scale;
                channel1Viewer.scrollViewer.ScrollToVerticalOffset(verticalOffset);
                channel1Viewer.scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            }

            if (viewer != channel2Viewer && scale != null)
            {
                channel2Viewer.Stretch = stretch;
                channel2Viewer.image.Stretch = stretch;
                channel2Viewer.Scale = scale;
                channel2Viewer.scrollViewer.ScrollToVerticalOffset(verticalOffset);
                channel2Viewer.scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            }

            if (viewer != fusedChannelViewer && scale != null)
            {
                fusedChannelViewer.Stretch = stretch;
                fusedChannelViewer.image.Stretch = stretch;
                fusedChannelViewer.Scale = scale;
                fusedChannelViewer.scrollViewer.ScrollToVerticalOffset(verticalOffset);
                fusedChannelViewer.scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            }

            isSynchronizingViews = false;
        }
    }

    /// <summary>
    /// Handler to <see cref="DisplayChannel.All"/> mode being selected.
    /// </summary>
    private void AllChannels_Checked(object sender, RoutedEventArgs e)
    {
        // Cache settings.
        _channel1ViewerSettings = new ViewerSettings(channel1Viewer.Stretch, channel1Viewer.Scale, channel1Viewer.scrollViewer.VerticalOffset, channel1Viewer.scrollViewer.HorizontalOffset);
        _channel2ViewerSettings = new ViewerSettings(channel2Viewer.Stretch, channel2Viewer.Scale, channel2Viewer.scrollViewer.VerticalOffset, channel2Viewer.scrollViewer.HorizontalOffset);
        _fusedChannelViewerSettings = new ViewerSettings(fusedChannelViewer.Stretch, fusedChannelViewer.Scale, fusedChannelViewer.scrollViewer.VerticalOffset, fusedChannelViewer.scrollViewer.HorizontalOffset);

        // Use uniform stretch when all channels are to be shown.
        channel1Viewer.Stretch = Stretch.Uniform;
        channel1Viewer.image.Stretch = Stretch.Uniform;
        channel2Viewer.Stretch = Stretch.Uniform;
        channel2Viewer.image.Stretch = Stretch.Uniform;
        fusedChannelViewer.Stretch = Stretch.Uniform;
        fusedChannelViewer.image.Stretch = Stretch.Uniform;

        e.Handled = true;
    }

    /// <summary>
    /// Handler to <see cref="DisplayChannel.All"/> mode being de-selected.
    /// </summary>
    private void AllChannels_Unchecked(object sender, RoutedEventArgs e)
    {
        // Turn off synchronization while restoring settings.
        var viewModel = DataContext as ImageDisplayViewModel;
        var synchronizeViews = viewModel.SynchronizeViews; // cache synchronization setting
        viewModel.SynchronizeViews = false;

        // Restore cached settings.
        if (channel1Viewer.Scale != null)
        {
            channel1Viewer.Stretch = _channel1ViewerSettings.Stretch;
            channel1Viewer.image.Stretch = _channel1ViewerSettings.Stretch;
            channel1Viewer.Scale = _channel1ViewerSettings.Scale;
            channel1Viewer.UpdateLayout();
            channel1Viewer.scrollViewer.ScrollToVerticalOffset(_channel1ViewerSettings.VerticalOffset);
            channel1Viewer.scrollViewer.ScrollToHorizontalOffset(_channel1ViewerSettings.HorizontalOffset);
        }

        if (channel2Viewer.Scale != null)
        {
            channel2Viewer.Stretch = _channel2ViewerSettings.Stretch;
            channel2Viewer.image.Stretch = _channel2ViewerSettings.Stretch;
            channel2Viewer.Scale = _channel2ViewerSettings.Scale;
            channel2Viewer.UpdateLayout();
            channel2Viewer.scrollViewer.ScrollToVerticalOffset(_channel2ViewerSettings.VerticalOffset);
            channel2Viewer.scrollViewer.ScrollToHorizontalOffset(_channel2ViewerSettings.HorizontalOffset);
        }

        if (fusedChannelViewer.Scale != null)
        {
            fusedChannelViewer.Stretch = _fusedChannelViewerSettings.Stretch;
            fusedChannelViewer.image.Stretch = _fusedChannelViewerSettings.Stretch;
            fusedChannelViewer.Scale = _fusedChannelViewerSettings.Scale;
            fusedChannelViewer.UpdateLayout();
            fusedChannelViewer.scrollViewer.ScrollToVerticalOffset(_fusedChannelViewerSettings.VerticalOffset);
            fusedChannelViewer.scrollViewer.ScrollToHorizontalOffset(_fusedChannelViewerSettings.HorizontalOffset);
        }

        // Restore cached synchronization setting.
        viewModel.SynchronizeViews = synchronizeViews;

        e.Handled = true;
    }
}