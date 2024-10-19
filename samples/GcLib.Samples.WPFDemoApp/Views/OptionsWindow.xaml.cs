using System.Windows;
using MahApps.Metro.Controls;

namespace ImagerViewer.Views;

/// <summary>
/// Window exposing user options for application.
/// </summary>
public partial class OptionsWindow : MetroWindow
{
    public OptionsWindow()
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
    }
}