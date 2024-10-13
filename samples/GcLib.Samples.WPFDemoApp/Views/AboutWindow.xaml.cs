using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using MahApps.Metro.Controls;

namespace FusionViewer.Views;

/// <summary>
/// Dialog for an About window, displaying general application and author information.
/// </summary>
public partial class AboutWindow : MetroWindow
{
    public AboutWindow()
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
    }

    /// <summary>
    /// Event-handling method to navigation request events from hyperlinks in view.
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            _ = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch (Exception)
        {
            // ignore
        }
    }
}