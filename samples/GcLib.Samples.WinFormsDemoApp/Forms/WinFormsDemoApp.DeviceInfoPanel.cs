using System.Windows.Forms;
using GcLib;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp : Form
{
    /// <summary>
    /// Display device info.
    /// </summary>
    /// <param name="deviceInfo">Device info.</param>
    private void DisplayDeviceInfo(GcDeviceInfo deviceInfo)
    {
        VendorNameLabel.Text = deviceInfo.VendorName;
        ModelLabel.Text = deviceInfo.ModelName;
        SerialLabel.Text = deviceInfo.SerialNumber;
        UniqueIDLabel.Text = deviceInfo.UniqueID;
        ClassLabel.Text = deviceInfo.DeviceClassInfo.DeviceType.Name;
    }

    /// <summary>
    /// Clear device info.
    /// </summary>
    private void ClearDeviceInfo()
    {
        VendorNameLabel.Text = null;
        ModelLabel.Text = null;
        SerialLabel.Text = null;
        UniqueIDLabel.Text = null;
        ClassLabel.Text = null;
    }
}