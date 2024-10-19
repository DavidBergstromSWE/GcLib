using System.Windows;
using MahApps.Metro.Controls;

namespace ImagerViewer.Views;

/// <summary>
/// Interaction logic for LogWindow.xaml
/// </summary>
public partial class LogWindow : MetroWindow
{
    public LogWindow()
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
    }
}