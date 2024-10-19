using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ImagerViewerApp.Views;

/// <summary>
/// Interaction logic for DisplayView.xaml
/// </summary>
public partial class ImageProcessingView : UserControl
{
    public ImageProcessingView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Eventhandler to KeyUp events in TextBox, updating current binding on 'Enter' key being pressed.
    /// </summary>
    private void TextBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BindingExpression binding = BindingOperations.GetBindingExpression((TextBox)sender, TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }

    /// <summary>
    /// Eventhandler to right mousebutton events in Slider, resetting slider value to 0.
    /// </summary>
    private void Slider_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var slider = (Slider)sender;
        slider.Value = 0;
        e.Handled = true;
    }

    /// <summary>
    /// Eventhandler to key events in ComboBox.
    /// </summary>
    private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Disable default behaviour of opening combobox when F4 key is pressed.
        if (e.Key == Key.F4)
        {
            // Execute keybinding command instead.
            var binding = App.Current.MainWindow.InputBindings.Cast<KeyBinding>().Where(k => k.Key == Key.F4);
            if (binding.Any())
                binding.First()?.Command.Execute((DisplayChannel)binding.First().CommandParameter);

            e.Handled = true;
        }
    }
}