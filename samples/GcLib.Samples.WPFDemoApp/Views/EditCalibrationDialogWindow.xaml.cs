using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;

namespace ImagerViewerApp.Views;

/// <summary>
/// Dialog for a window allowing editing of an existing geometric calibration.
/// </summary>
public partial class EditCalibrationDialogWindow : MetroWindow
{
    /// <summary>
    /// Creates a dialog for a window allowing editing of an existing geometric calibration.
    /// </summary>
    public EditCalibrationDialogWindow()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Explicitly updates binding in <see cref="TextBox"/> when a key is pressed (while control is in focus).
    /// </summary>
    /// <param name="sender"><see cref="TextBox"/> control.</param>
    /// <param name="e">Event arguments passed.</param>
    private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
        {
            var textBox = (TextBox)sender;

            // Update binding source.
            BindingExpression exp = textBox.GetBindingExpression(TextBox.TextProperty);
            exp.UpdateSource();
        }
    }
}