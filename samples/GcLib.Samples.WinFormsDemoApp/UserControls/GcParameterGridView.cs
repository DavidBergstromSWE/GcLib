using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GcLib;

namespace WinFormsDemoApp.UserControls;

/// <summary>
/// Composite user control for displaying and interacting with camera device parameters and features with category and visibility filtering capabilities. 
/// </summary>
public partial class GcParameterGridView : UserControl
{
    #region Fields

    /// <summary>
    /// Camera device.
    /// </summary>
    private GcDevice _camera;

    /// <summary>
    /// Visibility setting.
    /// </summary>
    private GcVisibility _visibility;

    /// <summary>
    /// Contains a (cached) list of parameters implemented by camera.
    /// </summary>
    private IReadOnlyList<GcParameter> _parameterList;

    /// <summary>
    /// Contains a list of parameter categories implemented by camera.
    /// </summary>
    private List<string> _categoryList;

    #endregion

    /// <summary>
    /// Creates an empty ParameterGridView control. Use Init method to initialize control with camera device parameters and a default visibility setting.
    /// </summary>
    public GcParameterGridView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes ParameterGridView control with parameters from camera at a specified initial visibility setting.
    /// </summary>
    /// <param name="camera">Camera device.</param>
    /// <param name="visibility">Initial visibility.</param>
    public void Init(GcDevice camera, GcVisibility visibility = GcVisibility.Beginner)
    {
        _camera = camera;

        // Retrieve list of all visible parameters.
        _parameterList = camera.Parameters.ToList(GcVisibility.Guru);

        // Initialize filtering controls.
        _visibility = visibility;
        InitializeVisibilityComboBox(_visibility);
        _categoryList = GetParameterCategories(_parameterList);
        InitializeCategoryComboBox(_categoryList);

        // Create a new list of parameters in DataGridView.
        CreateGridView(_parameterList);

        // Register to control events.
        CategoryFilterComboBox.SelectedIndexChanged += CategoryFilterComboBox_SelectedIndexChanged;
        VisibilityFilterComboBox.SelectedIndexChanged += VisibilityFilterComboBox_SelectedIndexChanged;

        // Register to camera events.
        _camera.ParameterInvalidate += OnParameterInvalidate;
    }

    /// <summary>
    /// Retrieve a unique set of categories from a list of parameters.
    /// </summary>
    /// <returns></returns>
    private static List<string> GetParameterCategories(IReadOnlyList<GcParameter> parameterList)
    {
        return [.. parameterList.Select(o => o.Category).Distinct()];
    }

    /// <summary>
    /// Clear and reset control to an empty state.
    /// </summary>
    public void Clear()
    {
        ClearGridView();
        VisibilityFilterComboBox.DataSource = null;
        VisibilityFilterComboBox.Items.Clear();
        CategoryFilterComboBox.Items.Clear();

        // Unregister from control events.
        CategoryFilterComboBox.SelectedIndexChanged -= CategoryFilterComboBox_SelectedIndexChanged;
        VisibilityFilterComboBox.SelectedIndexChanged -= VisibilityFilterComboBox_SelectedIndexChanged;

        // Unregister from camera events.
        if (_camera != null)
            _camera.ParameterInvalidate -= OnParameterInvalidate;

        _camera = null;
        _parameterList = null;
    }

    /// <summary>
    /// Updates current DataGridView with an updated list of parameters.
    /// </summary>
    /// <param name="parameterList">Updated list of parameters.</param>
    private void UpdateGridView(IReadOnlyList<GcParameter> parameterList)
    {
        foreach (DataGridViewRow row in ParameterDataGridView.Rows)
        {
            string parameterName = (string)row.Cells["Parameter"].Value;
            string oldValue = row.Cells["Value"].Value.ToString();
            string newValue = parameterList.FirstOrDefault(p => p.Name == parameterName).ToString();
            if (oldValue != newValue)
                row.Cells["Value"].Value = newValue;
        }
    }

    /// <summary>
    /// Create a new DataGridView based on a list of camera parameters/features and applied category and visibility filters.
    /// </summary>
    /// <param name="parameterList">List of camera parameters.</param>
    private void CreateGridView(IReadOnlyList<GcParameter> parameterList)
    {
        if (parameterList == null)
            return;

        ClearGridView();

        // Add new row from each parameter list entry.
        foreach (GcParameter parameter in parameterList)
        {
            // Apply category filter.
            if (_categoryList.Contains(parameter.Category) == false)
                continue;

            // Apply visibility filter.
            if ((int)_visibility < (int)parameter.Visibility)
                continue;

            int rowIndex = ParameterDataGridView.Rows.Add([parameter.DisplayName]);

            // Check object type.
            switch (parameter)
            {
                case GcInteger gcInteger: // Implemented as textbox or combobox

                    if (gcInteger.IncrementMode == EIncMode.listIncrement) // implement as combobox
                    {
                        var integerComboBoxCell = new DataGridViewComboBoxCell { DataSource = gcInteger.ListOfValidValue.Select(i => i.ToString()).ToArray(), Value = gcInteger.Value.ToString() };
                        ParameterDataGridView.Rows[rowIndex].Cells["Value"] = integerComboBoxCell;
                        // Formatting
                        integerComboBoxCell.ReadOnly = gcInteger.IsWritable == false;
                        integerComboBoxCell.FlatStyle = gcInteger.IsWritable ? FlatStyle.Standard : FlatStyle.Flat;
                        integerComboBoxCell.Style.ForeColor = gcInteger.IsWritable ? Color.Black : Color.Gray;
                        integerComboBoxCell.ToolTipText = gcInteger.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcInteger.Description;
                    }
                    else // implement as textbox
                    {
                        var integerTextBoxCell = new DataGridViewTextBoxCell { Value = gcInteger.ToString() };
                        ParameterDataGridView.Rows[rowIndex].Cells["Value"] = integerTextBoxCell;
                        // Formatting
                        integerTextBoxCell.ReadOnly = gcInteger.IsWritable == false;
                        integerTextBoxCell.Style.ForeColor = gcInteger.IsWritable ? Color.Black : Color.Gray;
                        integerTextBoxCell.ToolTipText = gcInteger.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcInteger.Description;
                    }

                    break;

                case GcFloat gcFloat: // Implemented as textbox

                    var floatTextBoxCell = new DataGridViewTextBoxCell { Value = gcFloat.ToString() }; // textbox
                    ParameterDataGridView.Rows[rowIndex].Cells["Value"] = floatTextBoxCell;
                    // Formatting
                    floatTextBoxCell.ReadOnly = gcFloat.IsWritable == false;
                    floatTextBoxCell.Style.ForeColor = gcFloat.IsWritable ? Color.Black : Color.Gray;
                    floatTextBoxCell.ToolTipText = gcFloat.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcFloat.Description;

                    break;

                case GcString gcString: // Implemented as textbox

                    var stringTextBoxCell = new DataGridViewTextBoxCell { Value = gcString.ToString() };
                    ParameterDataGridView.Rows[rowIndex].Cells["Value"] = stringTextBoxCell;
                    // Formatting
                    stringTextBoxCell.ReadOnly = gcString.IsWritable == false;
                    stringTextBoxCell.Style.ForeColor = gcString.IsWritable ? Color.Black : Color.Gray;
                    stringTextBoxCell.ToolTipText = gcString.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcString.Description;

                    break;

                case GcEnumeration gcEnumeration: // Implemented as combobox

                    var comboBoxCell = new DataGridViewComboBoxCell { DataSource = gcEnumeration.GetSymbolics(), Value = gcEnumeration.StringValue };
                    ParameterDataGridView.Rows[rowIndex].Cells["Value"] = comboBoxCell;
                    // Formatting
                    comboBoxCell.ReadOnly = gcEnumeration.IsWritable == false;
                    comboBoxCell.FlatStyle = gcEnumeration.IsWritable ? FlatStyle.Standard : FlatStyle.Flat;
                    comboBoxCell.Style.ForeColor = gcEnumeration.IsWritable ? Color.Black : Color.Gray;
                    comboBoxCell.ToolTipText = gcEnumeration.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcEnumeration.Description;

                    break;

                case GcBoolean gcBoolean: // Implemented as checkbox

                    var checkBoxCell = new DataGridViewCheckBoxCell { Value = gcBoolean.Value };
                    ParameterDataGridView.Rows[rowIndex].Cells["Value"] = checkBoxCell;
                    // Formatting
                    checkBoxCell.ReadOnly = gcBoolean.IsWritable == false;
                    checkBoxCell.FlatStyle = gcBoolean.IsWritable ? FlatStyle.Standard : FlatStyle.Flat;
                    checkBoxCell.Style.ForeColor = gcBoolean.IsWritable ? Color.Black : Color.Gray;
                    checkBoxCell.ToolTipText = gcBoolean.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcBoolean.Description;

                    break;

                case GcCommand gcCommand: // Implemented as button

                    var buttonCell = new DataGridViewButtonCell { Value = parameter.Name };
                    ParameterDataGridView.Rows[rowIndex].Cells["Value"] = buttonCell;
                    // Formatting
                    buttonCell.ToolTipText = gcCommand.Description; ParameterDataGridView.Rows[rowIndex].Cells["Parameter"].ToolTipText = gcCommand.Description;

                    break;
            }
        }

        // Remove initial cell selection
        ParameterDataGridView.ClearSelection();
    }

    /// <summary>
    /// Clear DataGridView.
    /// </summary>
    private void ClearGridView()
    {
        ParameterDataGridView.Rows.Clear();
    }

    /// <summary>
    /// Refresh DataGridView with an updated set of parameters from camera.
    /// </summary>
    public void RefreshGridView()
    {
        if (_camera == null || Enabled == false)
            return;

        // Store DataGridView scrolling position and cell selection.
        int currentFirstDisplayedScrollingRowIndex = ParameterDataGridView.FirstDisplayedScrollingRowIndex; // scrolling position
        int currentCellRowIndex = ParameterDataGridView.CurrentCell.RowIndex; int currentCellColumnIndex = ParameterDataGridView.CurrentCell.ColumnIndex; // cell selection

        // Retrieve updated version of parameter list from camera.
        _parameterList = _camera.Parameters.ToList(GcVisibility.Guru);

        // Update DataGridGrid with new parameter list.
        UpdateGridView(_parameterList);

        // Restore DataGridView scrolling position and cell selection.
        ParameterDataGridView.FirstDisplayedScrollingRowIndex = currentFirstDisplayedScrollingRowIndex;  // scrolling position
        ParameterDataGridView.CurrentCell = ParameterDataGridView.Rows[currentCellRowIndex].Cells[currentCellColumnIndex]; // cell selection
    }

    /// <summary>
    /// Update parameter cell in ParameterGridView control with new value retrieved from camera.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    public void UpdateParameter(string parameterName)
    {
        if (_camera == null)
            throw new NullReferenceException("No camera was found!");

        // Retrieve parameter value from camera.
        string parameterValue = _camera.Parameters.GetParameterValue(parameterName);
        if (parameterValue != null && parameterValue != GetCellValue(parameterName))
            SetCellValue(parameterName, parameterValue);

        RefreshGridView();
    }


    /// <summary>
    /// Set cell in DataGridView to a new value.
    /// </summary>
    /// <param name="parameterName">Parameter name of cell.</param>
    /// <param name="parameterValue">Parameter value of cell.</param>
    private void SetCellValue(string parameterName, string parameterValue)
    {
        // Find row representing parameter
        foreach (DataGridViewRow row in ParameterDataGridView.Rows)
        {
            if (row.Cells["Parameter"].Value.ToString() == parameterName)
            {
                row.Cells["Value"].Value = parameterValue;
                return;
            }
        }
    }

    /// <summary>
    /// Returns current value of cell in DataGridView.
    /// </summary>
    /// <param name="parameterName">Parameter name of cell.</param>
    /// <returns>Cell value for parameter (or null if not found).</returns>
    private string GetCellValue(string parameterName)
    {
        // Find row representing parameter
        foreach (DataGridViewRow row in ParameterDataGridView.Rows)
        {
            if (row.Cells["Parameter"].Value.ToString() == parameterName)
                return row.Cells["Value"].Value.ToString();
        }
        return null;
    }

    /// <summary>
    /// Fill category filter with categories and select "All categories" as the initial setting.
    /// </summary>
    private void InitializeCategoryComboBox(List<string> categoryList)
    {
        CategoryFilterComboBox.Items.Clear();
        _ = CategoryFilterComboBox.Items.Add("All categories");
        CategoryFilterComboBox.Items.AddRange([.. categoryList]);
        CategoryFilterComboBox.SelectedIndex = 0; // "All categories"
    }

    /// <summary>
    /// Fill visibility filter and select an initial visibility setting.
    /// </summary>
    /// <param name="visibility">Initial visibility setting.</param>
    private void InitializeVisibilityComboBox(GcVisibility visibility)
    {
        VisibilityFilterComboBox.DataSource = new Enum[] { GcVisibility.Beginner, GcVisibility.Expert, GcVisibility.Guru }; // Invisible is filtered out
        if (VisibilityFilterComboBox.Items.Contains(visibility))
            VisibilityFilterComboBox.SelectedItem = visibility;
    }

    /// <summary>
    /// Handles user selection in category filter combobox.
    /// </summary>
    private void CategoryFilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (CategoryFilterComboBox.SelectedIndex == 0) // "All categories"
        {
            var categoryList = CategoryFilterComboBox.Items.Cast<object>().Select(item => item.ToString()).ToList();
            categoryList.RemoveAt(0);
            _categoryList = categoryList;
        }
        else // specific category
        {
            var categoryList = new List<string> { CategoryFilterComboBox.GetItemText(CategoryFilterComboBox.SelectedItem) };
            _categoryList = categoryList;
        }

        CreateGridView(_parameterList);
    }

    /// <summary>
    /// Handles user selection in visibility filter combobox.
    /// </summary>
    private void VisibilityFilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (VisibilityFilterComboBox.Items.Count > 0)
        {
            _visibility = (GcVisibility)VisibilityFilterComboBox.SelectedIndex;
            CreateGridView(_parameterList);
        }
    }

    /// <summary>
    /// Handles user input into textboxes or button clicks in DataGridView.
    /// </summary>
    private void ParameterDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        // Make sure the clicked row/column is valid
        bool validClick = e.RowIndex != -1 && e.ColumnIndex != -1;

        if (validClick)
        {
            DataGridViewCell currentCell = ParameterDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if ((currentCell is DataGridViewTextBoxCell) || (currentCell is DataGridViewButtonCell))
            {
                // Parse input and propagate change to camera.
                ParseCellDataToCamera(ParameterDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]);

                // Update GUI controls.
                OnParameterValueChanged(this, EventArgs.Empty);

                // Display parameter info in PropertyGrid.
                DisplayParameterInPropertyGrid(ParameterDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]);
            }
        }
    }

    /// <summary>
    /// Handles user input into comboboxes and checkboxes in DataGridView.
    /// </summary>
    private void ParameterDataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
        int rowIndex = ParameterDataGridView.CurrentCell.RowIndex;
        int columnIndex = ParameterDataGridView.CurrentCell.ColumnIndex;
        DataGridViewCell currentCell = ParameterDataGridView.CurrentCell;

        if (ParameterDataGridView.IsCurrentCellDirty && (currentCell is DataGridViewComboBoxCell || currentCell is DataGridViewCheckBoxCell))
        {
            // Commit changes to ComboBox and CheckBox immediately.
            _ = ParameterDataGridView.EndEdit(DataGridViewDataErrorContexts.Commit);

            // Parse input and propagate change to camera.
            ParseCellDataToCamera(currentCell);

            // Update GUI controls.               
            OnParameterValueChanged(this, EventArgs.Empty);

            // Display parameter info in PropertyGrid.
            DisplayParameterInPropertyGrid(ParameterDataGridView.Rows[rowIndex].Cells[columnIndex]);
        }
    }

    /// <summary>
    /// Parse new data input in DataGridView cell and update/execute corresponding parameter in camera.
    /// </summary>
    /// <param name="dataGridViewCell">Cell which has been changed.</param>
    private void ParseCellDataToCamera(DataGridViewCell dataGridViewCell)
    {
        string parameterName = dataGridViewCell.OwningRow.Cells["Parameter"].Value.ToString();

        try
        {
            if (dataGridViewCell is DataGridViewTextBoxCell || dataGridViewCell is DataGridViewComboBoxCell || dataGridViewCell is DataGridViewCheckBoxCell)
                _camera.Parameters.SetParameterValue(parameterName, dataGridViewCell.Value.ToString());
            else if (dataGridViewCell is DataGridViewButtonCell)
                _camera.Parameters.ExecuteParameterCommand(parameterName);

            // Update cell with new parameter value (move outside try/catch?).
            UpdateParameter(parameterName);
        }

        //* Exception handling *//

        // Parameter not found in camera
        catch (MissingMemberException ex)
        {
            string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            DialogResult result = MessageBox.Show(message, "Input Error!", MessageBoxButtons.OK);
            if (result == DialogResult.OK)
            { /* Do something? */ }
        }

        // Wrong format of input
        catch (FormatException ex)
        {
            string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            DialogResult result = MessageBox.Show($"{message} '{parameterName}' expects {_camera.Parameters.GetParameter(parameterName).Type.ToString().ToLower()} input.", "Input Error! ", MessageBoxButtons.OK);
            if (result == DialogResult.OK)
            {
                if (dataGridViewCell is DataGridViewTextBoxCell || dataGridViewCell is DataGridViewComboBoxCell || dataGridViewCell is DataGridViewCheckBoxCell)
                    dataGridViewCell.Value = _camera.Parameters.GetParameterValue(parameterName); // reset to old value
            }
        }

        // All others
        catch (Exception ex)
        {
            string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            DialogResult result = MessageBox.Show(message, "Input Error!", MessageBoxButtons.OK);
            if (result == DialogResult.OK)
            {
                if (dataGridViewCell is DataGridViewTextBoxCell || dataGridViewCell is DataGridViewComboBoxCell || dataGridViewCell is DataGridViewCheckBoxCell)
                    dataGridViewCell.Value = _camera.Parameters.GetParameterValue(parameterName); // reset to old value
            }
        }
    }

    /// <summary>
    /// Helping event handler, making combobox dropdown menus in DataGridView appear at first click.
    /// </summary>
    private void ParameterDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        bool validClick = e.RowIndex != -1 && e.ColumnIndex != -1; //Make sure the clicked row/column is valid.

        if (validClick)
        {
            if (ParameterDataGridView.CurrentCell.ReadOnly == false)
            {
                if (ParameterDataGridView.CurrentCell is DataGridViewComboBoxCell)
                {
                    _ = ParameterDataGridView.BeginEdit(true);
                    ((ComboBox)ParameterDataGridView.EditingControl).DroppedDown = true; // open dropdown menu at first click (no focus click necessary)
                }

                if (ParameterDataGridView.CurrentCell is DataGridViewButtonCell)
                    ParameterDataGridView_CellEndEdit(sender, e); // re-directs to CellEndEdit event handler
            }

            // Display parameter info in PropertyGrid.
            DisplayParameterInPropertyGrid(ParameterDataGridView.CurrentCell);
        }
    }

    /// <summary>
    /// Display parameter properties of selected DataGridView cell in PropertyGrid.
    /// </summary>
    private void DisplayParameterInPropertyGrid(DataGridViewCell dataGridViewCell)
    {
        string parameterName = dataGridViewCell.OwningRow.Cells["Parameter"].Value.ToString();

        try
        {
            GcParameter parameter = _camera.Parameters.GetParameter(parameterName);

            PropertyGrid.SelectedObject = parameter;
            PropertyGrid.Refresh();
            PropertyGrid.Visible = true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Displays PropertyGrid of selected cell when mouse is entering DataGridView.
    /// </summary>
    private void ParameterDataGridView_MouseEnter(object sender, EventArgs e)
    {
        if (ParameterDataGridView.SelectedCells.Count != 0)
            PropertyGrid.Visible = true; // show propertygrid if cell is selected
    }

    /// <summary>
    /// Hides PropertyGrid of selected cell when mouse is leaving DataGridView.
    /// </summary>
    private void ParameterDataGridView_MouseLeave(object sender, EventArgs e)
    {
        PropertyGrid.Visible = false;
    }

    /// <summary>
    /// Refresh DataGridView with an updated parameter list.
    /// </summary>
    private void RefreshGridViewButton_Click(object sender, EventArgs e)
    {
        _camera?.Parameters.Update();
        RefreshGridView();
    }

    /// <summary>
    /// Responds to <see cref="GcDevice.ParameterInvalidate"/> events raised in a device when a parameter has changed (and dependent parameters may have been updated).
    /// </summary>
    private void OnParameterInvalidate(object sender, EventArgs e)
    {
        _camera?.Parameters.Update();
        RefreshGridView();
    }

    /// <summary>
    /// Event raised when a parameter value has changed in the DataGridView.
    /// </summary>
    public event EventHandler ParameterValueChanged;

    /// <summary>
    /// Signals that a parameter value has changed in the DataGridView and may need invalidation/updating of other UI controls.
    /// </summary>
    protected virtual void OnParameterValueChanged(object sender, EventArgs e)
    {
        ParameterValueChanged?.Invoke(this, e);
    }
}