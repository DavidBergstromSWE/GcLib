using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace ImagerViewerApp.Views
{
    /// <summary>
    /// Interaction logic for ShortcutWindow.xaml
    /// </summary>
    public partial class ShortcutWindow : MetroWindow
    {
        public ShortcutWindow()
        {
            InitializeComponent();

            Owner = Application.Current.MainWindow;

            // Add all input bindings from parent window.
            foreach (InputBinding binding in Owner.InputBindings)
                InputBindings.Add(binding);
        }
    }
}