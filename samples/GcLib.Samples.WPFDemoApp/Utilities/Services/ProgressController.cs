using System;
using System.Threading.Tasks;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;

namespace ImagerViewer.Utilities.Services;

/// <summary>
/// Controller of an opened progress dialog. The class if a wrapper for the <see cref="ProgressDialogController"/> in MahApps.Metro framework.
/// </summary>
internal sealed class ProgressController : IProgressController
{
    /// <summary>
    /// Manipulates opened <see cref="ProgressDialog"/> instances.
    /// </summary>
    private readonly ProgressDialogController _controller;

    /// <summary>
    /// Creates a new controller of an opened progress dialog.
    /// </summary>
    /// <param name="controller">Manipulates opened <see cref="ProgressDialog"/> instances.</param>
    public ProgressController(ProgressDialogController controller)
    {
        _controller = controller;

        // Hook eventhandler for programmically closed dialogs.
        _controller.Closed += Controller_Closed;

        // Hook eventhandler for user-canceled dialogs.
        _controller.Canceled += Controller_Canceled;
    }

    /// <summary>
    /// Eventhandler for user-canceled dialogs, invoking <see cref="Canceled"/> event.
    /// </summary>
    private void Controller_Canceled(object sender, EventArgs e)
    {
        Canceled?.Invoke(this, e);
    }

    /// <summary>
    /// Eventhandler for programmically closed dialogs, invoking <see cref="Closed"/> event.
    /// </summary>
    private void Controller_Closed(object sender, EventArgs e)
    {
        Closed?.Invoke(this, e);
    }

    /// <summary>
    /// True if cancellation has been requested by the user.
    /// </summary>
    public bool IsCanceled => _controller.IsCanceled;

    /// <summary>
    /// True if progress dialog is open.
    /// </summary>
    public bool IsOpen => _controller.IsOpen;

    /// <summary>
    /// Minimum restriction for progress value.
    /// </summary>
    public double Minimum { get => _controller.Minimum; set => _controller.Minimum = value; }

    /// <summary>
    /// Maximum restriction for progress value.
    /// </summary>
    public double Maximum { get => _controller.Maximum; set => _controller.Maximum = value; }

    /// <summary>
    /// Event raised when progress dialog has been closed.
    /// </summary>
    public event EventHandler Closed;

    /// <summary>
    /// Event raised when progress dialog has been canceled.
    /// </summary>
    public event EventHandler Canceled;

    /// <summary>
    /// Begins the operation to close an opened progress dialog.
    /// </summary>
    /// <returns></returns>
    public Task CloseAsync()
    {
        return _controller.CloseAsync();
    }

    /// <summary>
    /// Controls if a cancellation button should be visible.
    /// </summary>
    /// <param name="value">True if cancellation button should be visible.</param>
    public void SetCancelable(bool value)
    {
        _controller.SetCancelable(value);
    }

    /// <summary>
    /// Controls if progress should be indeterminate.
    /// </summary>
    public void SetIndeterminate()
    {
        _controller.SetIndeterminate();
    }

    /// <summary>
    /// Controls the message displayed in the progress dialog.
    /// </summary>
    /// <param name="message">Message to be displayed.</param>
    public void SetMessage(string message)
    {
        _controller.SetMessage(message);
    }

    /// <summary>
    /// Controls the progress value of the progress bar.
    /// </summary>
    /// <param name="value">Progress value.</param>
    public void SetProgress(double value)
    {
        _controller.SetProgress(value);
    }

    /// <summary>
    /// Controls the foreground brush of the progress bar.
    /// </summary>
    /// <param name="brush">Brush.</param>
    public void SetProgressBarForegroundBrush(Brush brush)
    {
        _controller.SetProgressBarForegroundBrush(brush);
    }

    /// <summary>
    /// Controls the title of progress dialog.
    /// </summary>
    /// <param name="title">Title.</param>
    public void SetTitle(string title)
    {
        _controller.SetTitle(title);
    }
}