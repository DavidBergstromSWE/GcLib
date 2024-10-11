using System.Windows.Forms;

namespace WinFormsDemoApp.Controls;

/// <summary>
/// Customized DataGridView control. The control suppresses a row change when pressing Enter or Return key.
/// </summary>
public partial class CustomDataGridView : DataGridView
{
    public CustomDataGridView()
    {
        InitializeComponent();
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        // suppress row change when pressing Enter/Return key
        if (keyData == Keys.Enter)
        {
            EndEdit();
            return true;
        }
        return base.ProcessDialogKey(keyData);
    }
}
