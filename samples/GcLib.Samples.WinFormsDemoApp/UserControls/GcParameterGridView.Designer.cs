using WinFormsDemoApp.Controls;
using System.Windows.Forms;

namespace WinFormsDemoApp.UserControls;

partial class GcParameterGridView : UserControl
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        CategoryFilterComboBox = new ComboBox();
        RefreshGridViewButton = new Button();
        VisibilityFilterComboBox = new ComboBox();
        ParameterDataGridView = new CustomDataGridView();
        Parameter = new DataGridViewTextBoxColumn();
        Value = new DataGridViewTextBoxColumn();
        PropertyGrid = new PropertyGrid();
        ((System.ComponentModel.ISupportInitialize)ParameterDataGridView).BeginInit();
        SuspendLayout();
        // 
        // CategoryFilterComboBox
        // 
        CategoryFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        CategoryFilterComboBox.FormattingEnabled = true;
        CategoryFilterComboBox.Location = new System.Drawing.Point(5, 2);
        CategoryFilterComboBox.Name = "CategoryFilterComboBox";
        CategoryFilterComboBox.Size = new System.Drawing.Size(150, 23);
        CategoryFilterComboBox.TabIndex = 27;
        // 
        // RefreshGridViewButton
        // 
        RefreshGridViewButton.Location = new System.Drawing.Point(180, 2);
        RefreshGridViewButton.Name = "RefreshGridViewButton";
        RefreshGridViewButton.Size = new System.Drawing.Size(60, 23);
        RefreshGridViewButton.TabIndex = 29;
        RefreshGridViewButton.Text = "Refresh";
        RefreshGridViewButton.UseVisualStyleBackColor = true;
        RefreshGridViewButton.Click += RefreshGridViewButton_Click;
        // 
        // VisibilityFilterComboBox
        // 
        VisibilityFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        VisibilityFilterComboBox.FormattingEnabled = true;
        VisibilityFilterComboBox.Location = new System.Drawing.Point(265, 2);
        VisibilityFilterComboBox.Name = "VisibilityFilterComboBox";
        VisibilityFilterComboBox.Size = new System.Drawing.Size(90, 23);
        VisibilityFilterComboBox.TabIndex = 30;
        // 
        // ParameterDataGridView
        // 
        ParameterDataGridView.AllowUserToAddRows = false;
        ParameterDataGridView.AllowUserToDeleteRows = false;
        ParameterDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
        dataGridViewCellStyle1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
        dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        ParameterDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        ParameterDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        ParameterDataGridView.Columns.AddRange(new DataGridViewColumn[] { Parameter, Value });
        ParameterDataGridView.Location = new System.Drawing.Point(5, 28);
        ParameterDataGridView.Margin = new Padding(4, 3, 4, 3);
        ParameterDataGridView.MultiSelect = false;
        ParameterDataGridView.Name = "ParameterDataGridView";
        ParameterDataGridView.RowHeadersVisible = false;
        ParameterDataGridView.Size = new System.Drawing.Size(350, 456);
        ParameterDataGridView.TabIndex = 31;
        ParameterDataGridView.CellClick += ParameterDataGridView_CellClick;
        ParameterDataGridView.CellEndEdit += ParameterDataGridView_CellEndEdit;
        ParameterDataGridView.CurrentCellDirtyStateChanged += ParameterDataGridView_CurrentCellDirtyStateChanged;
        ParameterDataGridView.MouseEnter += ParameterDataGridView_MouseEnter;
        ParameterDataGridView.MouseLeave += ParameterDataGridView_MouseLeave;
        // 
        // Parameter
        // 
        Parameter.HeaderText = "Parameter";
        Parameter.Name = "Parameter";
        Parameter.ReadOnly = true;
        Parameter.Resizable = DataGridViewTriState.True;
        // 
        // Value
        // 
        Value.HeaderText = "Value";
        Value.Name = "Value";
        Value.Resizable = DataGridViewTriState.True;
        Value.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // PropertyGrid
        // 
        PropertyGrid.CommandsVisibleIfAvailable = false;
        PropertyGrid.DisabledItemForeColor = System.Drawing.SystemColors.ControlText;
        PropertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5F);
        PropertyGrid.HelpVisible = false;
        PropertyGrid.Location = new System.Drawing.Point(6, 490);
        PropertyGrid.Margin = new Padding(4, 3, 4, 3);
        PropertyGrid.Name = "PropertyGrid";
        PropertyGrid.PropertySort = PropertySort.NoSort;
        PropertyGrid.Size = new System.Drawing.Size(350, 115);
        PropertyGrid.TabIndex = 32;
        PropertyGrid.ToolbarVisible = false;
        PropertyGrid.Visible = false;
        // 
        // GcParameterGridView
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(PropertyGrid);
        Controls.Add(ParameterDataGridView);
        Controls.Add(VisibilityFilterComboBox);
        Controls.Add(RefreshGridViewButton);
        Controls.Add(CategoryFilterComboBox);
        Name = "GcParameterGridView";
        Size = new System.Drawing.Size(360, 611);
        ((System.ComponentModel.ISupportInitialize)ParameterDataGridView).EndInit();
        ResumeLayout(false);
    }

    #endregion

    // Constituent controls

    /// <summary>
    /// Combobox for filtering which parameter categories to display in DataGridView.
    /// </summary>
    private ComboBox CategoryFilterComboBox;

    /// <summary>
    /// Button to refresh DataGridView with most current parameter values.
    /// </summary>
    private Button RefreshGridViewButton;

    /// <summary>
    /// Combobox for filtering under which user visibility the parameters should be shown.
    /// </summary>
    private ComboBox VisibilityFilterComboBox;

    /// <summary>
    /// DataGridView containing parameter names and values.
    /// </summary>
    private CustomDataGridView ParameterDataGridView;

    /// <summary>
    /// PropertyGrid showing parameter properties.
    /// </summary>
    private PropertyGrid PropertyGrid;
    private DataGridViewTextBoxColumn Parameter;
    private DataGridViewTextBoxColumn Value;
}
