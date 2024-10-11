using System;
using System.Windows.Forms;
using GcLib;
using GcLib.Utilities.Collections;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp : Form
{
    /// <summary>
    /// Setup PropertyPanel with camera parameters.
    /// </summary>
    private void InitializePropertyPanel()
    {
        // Width
        GcInteger Width = _camera.Parameters.GetInteger("Width");
        WidthTextBox.Tag = Width;
        WidthTextBox.Text = Width?.ToString();
        WidthTextBox.Enabled = Width != null;
        WidthTextBox.ReadOnly = WidthTextBox.Enabled && !Width.IsWritable;
        WidthUnit.Text = WidthTextBox.Enabled ? Width.Unit : string.Empty;

        // Height
        GcInteger Height = _camera.Parameters.GetInteger("Height");
        HeightTextBox.Tag = Height;
        HeightTextBox.Text = Height?.ToString();
        HeightTextBox.Enabled = Height != null;
        HeightTextBox.ReadOnly = HeightTextBox.Enabled && !Height.IsWritable;
        HeightUnit.Text = HeightTextBox.Enabled ? Height.Unit : string.Empty;

        // Frame rate
        GcFloat AcquisitionFrameRate = null;
        if (_camera.Parameters.IsImplemented("AcquisitionFrameRate"))
            AcquisitionFrameRate = _camera.Parameters.GetFloat("AcquisitionFrameRate");
        else if (_camera.Parameters.IsImplemented("FrameRate"))
            AcquisitionFrameRate = _camera.Parameters.GetFloat("FrameRate");
        AcquisitionFrameRateTextBox.Tag = AcquisitionFrameRate;
        AcquisitionFrameRateTextBox.Text = AcquisitionFrameRate?.ToString();
        AcquisitionFrameRateTextBox.Enabled = AcquisitionFrameRate != null;
        AcquisitionFrameRateTextBox.ReadOnly = AcquisitionFrameRateTextBox.Enabled && !AcquisitionFrameRate.IsWritable;
        AcquisitionFrameRateUnit.Text = AcquisitionFrameRateTextBox.Enabled ? AcquisitionFrameRate.Unit : string.Empty;

        // Pixel format
        GcEnumeration PixelFormat = _camera.Parameters.GetEnumeration("PixelFormat");
        PixelFormatComboBox.Tag = PixelFormat;
        PixelFormatComboBox.DataSource = PixelFormat?.GetSymbolics();
        PixelFormatComboBox.SelectedItem = PixelFormat?.StringValue;
        PixelFormatComboBox.Enabled = PixelFormat != null && PixelFormat.IsWritable;

        // Test pattern
        GcEnumeration TestPattern = _camera.Parameters.GetEnumeration("TestPattern");
        TestPatternComboBox.Tag = TestPattern;
        TestPatternComboBox.DataSource = TestPattern?.GetSymbolics();
        TestPatternComboBox.SelectedItem = TestPattern?.StringValue;
        TestPatternComboBox.Enabled = TestPattern != null && TestPattern.IsWritable;
    }

    /// <summary>
    /// Parses user textbox input and changes corresponding camera parameter in camera.
    /// </summary>
    private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
    {
        // Trap Enter/Return key
        if (e.KeyChar == (char)Keys.Return)
        {
            var textBox = (TextBox)sender;

            string parameterName = ((GcParameter)textBox.Tag).Name;

            try
            {
                _camera.Parameters.SetParameterValue(parameterName, textBox.Text);
            }
            catch (FormatException ex) // not a number
            {
                string message = ex.Message;
                _ = MessageBox.Show(message, "Input Error!", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _ = MessageBox.Show(message, "Input Error!", MessageBoxButtons.OK);
            }

            // Update GUI with changes
            ParameterGridView.UpdateParameter(parameterName);
            UpdateControls(PropertyPanel);
            UpdateControls(AcquisitionPanel);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Parses combobox user input and changes corresponding camera parameter in camera.
    /// </summary>
    private void ComboBox_SelectionChangeCommitted(object sender, EventArgs e)
    {
        var comboBox = (ComboBox)sender;

        string parameterName = ((GcParameter)comboBox.Tag).Name;

        try
        {
            _camera.Parameters.SetParameterValue(parameterName, comboBox.SelectedItem.ToString());
        }
        catch (Exception ex)
        {
            string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            _ = MessageBox.Show(message, "Input Error!", MessageBoxButtons.OK);
        }

        // Update GUI with changes
        ParameterGridView.UpdateParameter(parameterName);
        UpdateControls(PropertyPanel);
        UpdateControls(AcquisitionPanel);
    }

    /// <summary>
    /// Parses checkbox user input and changes corresponding camera parameter in camera.
    /// </summary>
    private void CheckBox_Clicked(object sender, EventArgs e)
    {
        var checkBox = (CheckBox)sender;

        string parameterName = ((GcParameter)checkBox.Tag).Name;

        try
        {
            _camera.Parameters.SetParameterValue(parameterName, checkBox.Checked.ToString());
        }
        catch (Exception ex)
        {
            string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            _ = MessageBox.Show(message, "Input Error!", MessageBoxButtons.OK);
        }

        // Update GUI with changes
        ParameterGridView.UpdateParameter(parameterName);
        UpdateControls(PropertyPanel);
        UpdateControls(AcquisitionPanel);
    }

    /// <summary>
    /// Clears text labels, textboxes, comboboxes, etc. in PropertyPanel.
    /// </summary>
    private void ClearPropertyPanel()
    {
        WidthTextBox.Text = null;
        WidthUnit.Text = null;

        HeightTextBox.Text = null;
        HeightUnit.Text = null;

        AcquisitionFrameRateTextBox.Text = null;
        AcquisitionFrameRateUnit.Text = null;

        PixelFormatComboBox.DataSource = null;
        PixelFormatComboBox.Items.Clear();

        TestPatternComboBox.DataSource = null;
        TestPatternComboBox.Items.Clear();
    }
}