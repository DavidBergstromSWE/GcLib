using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using Serilog;

namespace FusionViewer.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        InitializeComponent();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("Main GUI window loaded");
    }

    private void MainWindow_Closed(object sender, System.EventArgs e)
    {
        Log.Debug("Main GUI window closed");
    }
}