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
        System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
        this.CategoryFilterComboBox = new System.Windows.Forms.ComboBox();
        this.RefreshGridViewButton = new System.Windows.Forms.Button();
        this.VisibilityFilterComboBox = new System.Windows.Forms.ComboBox();
        this.ParameterDataGridView = new CustomDataGridView();
        this.Parameter = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.PropertyGrid = new System.Windows.Forms.PropertyGrid();
        ((System.ComponentModel.ISupportInitialize)(this.ParameterDataGridView)).BeginInit();
        this.SuspendLayout();
        // 
        // CategoryFilterComboBox
        // 
        this.CategoryFilterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.CategoryFilterComboBox.FormattingEnabled = true;
        this.CategoryFilterComboBox.Location = new System.Drawing.Point(5, 2);
        this.CategoryFilterComboBox.Name = "CategoryFilterComboBox";
        this.CategoryFilterComboBox.Size = new System.Drawing.Size(150, 23);
        this.CategoryFilterComboBox.TabIndex = 27;
        // 
        // RefreshCameraParameterGridViewButton
        // 
        this.RefreshGridViewButton.Location = new System.Drawing.Point(180, 2);
        this.RefreshGridViewButton.Name = "RefreshCameraParameterGridViewButton";
        this.RefreshGridViewButton.Size = new System.Drawing.Size(60, 23);
        this.RefreshGridViewButton.TabIndex = 29;
        this.RefreshGridViewButton.Text = "Refresh";
        this.RefreshGridViewButton.UseVisualStyleBackColor = true;
        this.RefreshGridViewButton.Click += new System.EventHandler(this.RefreshGridViewButton_Click);
        // 
        // VisibilityFilterComboBox
        // 
        this.VisibilityFilterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.VisibilityFilterComboBox.FormattingEnabled = true;
        this.VisibilityFilterComboBox.Location = new System.Drawing.Point(265, 2);
        this.VisibilityFilterComboBox.Name = "VisibilityFilterComboBox";
        this.VisibilityFilterComboBox.Size = new System.Drawing.Size(90, 23);
        this.VisibilityFilterComboBox.TabIndex = 30;         
        // 
        // CameraParameterGrid
        // 
        this.ParameterDataGridView.AllowUserToAddRows = false;
        this.ParameterDataGridView.AllowUserToDeleteRows = false;
        this.ParameterDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
        dataGridViewCellStyle1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
        this.ParameterDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        this.ParameterDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.ParameterDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
        this.Parameter,
        this.Value});
        this.ParameterDataGridView.Location = new System.Drawing.Point(5, 28);
        this.ParameterDataGridView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        this.ParameterDataGridView.MultiSelect = false;
        this.ParameterDataGridView.Name = "CameraParameterGrid";
        this.ParameterDataGridView.RowHeadersVisible = false;
        this.ParameterDataGridView.Size = new System.Drawing.Size(350, 600);
        this.ParameterDataGridView.TabIndex = 31;
        this.ParameterDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ParameterDataGridView_CellClick);
        this.ParameterDataGridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.ParameterDataGridView_CellEndEdit);
        this.ParameterDataGridView.CurrentCellDirtyStateChanged += new System.EventHandler(this.ParameterDataGridView_CurrentCellDirtyStateChanged);
        this.ParameterDataGridView.MouseEnter += new System.EventHandler(this.ParameterDataGridView_MouseEnter);
        this.ParameterDataGridView.MouseLeave += new System.EventHandler(this.ParameterDataGridView_MouseLeave);
        // 
        // Parameter
        // 
        this.Parameter.HeaderText = "Parameter";
        this.Parameter.Name = "Parameter";
        this.Parameter.ReadOnly = true;
        this.Parameter.Resizable = System.Windows.Forms.DataGridViewTriState.True;
        // 
        // Value
        // 
        this.Value.HeaderText = "Value";
        this.Value.Name = "Value";
        this.Value.Resizable = System.Windows.Forms.DataGridViewTriState.True;
        this.Value.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
        // 
        // PropertyGrid
        // 
        this.PropertyGrid.CommandsVisibleIfAvailable = false;
        this.PropertyGrid.DisabledItemForeColor = System.Drawing.SystemColors.ControlText;
        this.PropertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.PropertyGrid.HelpVisible = false;
        this.PropertyGrid.Location = new System.Drawing.Point(5, 634);
        this.PropertyGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        this.PropertyGrid.Name = "PropertyGrid";
        this.PropertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
        this.PropertyGrid.Size = new System.Drawing.Size(350, 115);
        this.PropertyGrid.TabIndex = 32;
        this.PropertyGrid.ToolbarVisible = false;
        this.PropertyGrid.Visible = false;
        // 
        // ParameterGridView
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.PropertyGrid);
        this.Controls.Add(this.ParameterDataGridView);
        this.Controls.Add(this.VisibilityFilterComboBox);
        this.Controls.Add(this.RefreshGridViewButton);
        this.Controls.Add(this.CategoryFilterComboBox);
        this.Name = "ParameterGridView";
        this.Size = new System.Drawing.Size(360, 755);
        ((System.ComponentModel.ISupportInitialize)(this.ParameterDataGridView)).EndInit();
        this.ResumeLayout(false);

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
