using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace ImagerViewer.Views;

/// <summary>
/// Window for showing images in full screen mode.
/// </summary>
public partial class FullScreenImageWindow : MetroWindow
{
    public FullScreenImageWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Eventhandler to KeyDown events.
    /// </summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Close window on Escape key presses.
        if (e.Key == Key.Escape || e.Key == Key.F11)
        {
            Close();
        }
    }

    /// <summary>
    /// Eventhandler to click events on Exit menu item.
    /// </summary>
    private void OnExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Eventhandler to click events in context menu, where a image processing view is requested.
    /// </summary>
    private void OnOpenProcessingSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Only allow a single instance.
        if (OwnedWindows.Count > 0)
            return;

        // Create view as content.
        var view = new ImageProcessingView
        {
            Padding = new Thickness(left: 5, top: 5, right: 5, bottom: 5),

            // Re-use datacontext from original view.
            DataContext = Application.Current.MainWindow.FindChild<ImageProcessingView>().DataContext,
        };

        // Create new window with content.
        var window = new MetroWindow
        {
            Title = "Image Processing Settings",
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = this.Left + 20,
            Top = this.Top + 20,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = view,
            Owner = this,
            WindowTransitionsEnabled = false,
        };

        // Add all input bindings from parent window.
        foreach (InputBinding binding in InputBindings)
            window.InputBindings.Add(binding);

        // Show window.
        window.Show();
    }

    /// <summary>
    /// Eventhandler to click events over <see cref="CheckBox"/>-styled menu items in context menu, toggling the IsChecked property.
    /// </summary>
    private void OnCheckableMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ((sender as MenuItem).Icon as CheckBox).IsChecked = !((sender as MenuItem).Icon as CheckBox).IsChecked;
    }

    /// <summary>
    /// Eventhandler to click events where user has requested to close the context menu.
    /// </summary>
    private void OnContextMenuExit(object sender, RoutedEventArgs e)
    {
        contextMenu.IsOpen = false;
    }
}