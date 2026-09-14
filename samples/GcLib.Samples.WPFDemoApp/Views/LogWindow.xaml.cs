using System.Windows;
using MahApps.Metro.Controls;

namespace WPFDemoApp.Views;

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