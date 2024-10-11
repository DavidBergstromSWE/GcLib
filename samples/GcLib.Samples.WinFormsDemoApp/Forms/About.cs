using System;
using System.Reflection;
using System.Windows.Forms;

namespace WinFormsDemoApp;

/// <summary>
/// About window for displaying application and author information.
/// </summary>
public partial class About : Form
{
    /// <summary>
    /// Open window, displaying application and author information.
    /// </summary>
    public About()
    {
        InitializeComponent();

        FormBorderStyle = FormBorderStyle.FixedSingle;

        // Add title with version info.
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        Text = $"{assemblyName}";

        // Text to be shown in window.
        TitleLabel.Text = $"{assemblyName} v{version.Major}.{version.Minor}.{version.Build}";
        DescriptionLabel.Text = "Simple WinForms demo app for GcLib library.";
        CopyrightLabel.Text = "Copyright " + Convert.ToChar(169) + " 2024";
        CompanyLabel.Text = "MySimLabs";
        AuthorLabel.Text = "David Bergström";
        LinkLabel.Text = "david.bergstrom@mysimlabs.com";
    }

    /// <summary>
    /// Close window.
    /// </summary>
    private void OKbutton_Click(object sender, EventArgs e) => Close();
}