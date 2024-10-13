using System.Windows.Controls;
using System.Windows.Input;

namespace FusionViewer.Views
{
    /// <summary>
    /// Interaction logic for DeviceView.xaml
    /// </summary>
    public partial class DeviceView : UserControl
    {
        public DeviceView()
        {
            InitializeComponent();
        }

        private void ChannelNameTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Enter editing mode.
            ChangeEditingMode(textBox: sender as TextBox, enable: true);
        }

        private void ChannelNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                // Exit editing mode.
                ChangeEditingMode(textBox: sender as TextBox, enable: false);
            }
        }

        private void ChannelNameTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            // Exit editing mode.
            ChangeEditingMode(textBox: sender as TextBox, enable: false);
        }

        private static void ChangeEditingMode(TextBox textBox, bool enable)
        {
            textBox.IsReadOnly = !enable;
            textBox.Focusable = enable;
            textBox.BorderThickness = enable ? new System.Windows.Thickness(1) : new System.Windows.Thickness(0);
            textBox.Cursor = enable ? Cursors.IBeam : Cursors.Arrow;
        }
    }
}