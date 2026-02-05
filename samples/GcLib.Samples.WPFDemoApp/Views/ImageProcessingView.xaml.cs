using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ImagerViewer.Views;

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
        slider.Value = 50;
        e.Handled = true;
    }
}