using System.Windows.Forms;
using GcLib;

namespace WinFormsDemoApp;

public partial class WinFormsDemoApp : Form
{
    /// <summary>
    /// Event-handling method delegating a buffer display task.
    /// </summary>
    private void OnBufferDisplay(object sender, GcBuffer buffer)
    {
        if (DisplayControl.InvokeRequired)
        {
            if (_stopInvoking == false)
            {
                _invokeInProgress = true;

                // Invoke display on UI thread.
                _ = DisplayControl.BeginInvoke((MethodInvoker)delegate { DisplayControl.DisplayImage(buffer); });

                _invokeInProgress = false;
            }
        }
        else
        {
            DisplayControl.DisplayImage(buffer);
        }
        ;
    }
}