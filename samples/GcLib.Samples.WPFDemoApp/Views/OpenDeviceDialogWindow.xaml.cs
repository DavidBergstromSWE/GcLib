using System.Windows;
using MahApps.Metro.Controls;

namespace ImagerViewerApp.Views;

/// <summary>
/// Dialog for a window allowing user selection of a device from a list of available devices on the system.
/// </summary>
public partial class OpenDeviceDialogWindow : MetroWindow
{
    public OpenDeviceDialogWindow()
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
    }
}