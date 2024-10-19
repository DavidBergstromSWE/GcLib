using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ImagerViewerApp.Utilities.Services;

/// <summary>
/// Interface for a controller of an opened progress dialog.
/// </summary>
public interface IProgressController
{
    /// <summary>
    /// Returns true if dialog was cancelled.
    /// </summary>
    bool IsCanceled { get; }

    /// <summary>
    /// Returns true if dialog is open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Minimum value of the progress.
    /// </summary>
    double Minimum { get; set; }

    /// <summary>
    /// Maximum value of the progress.
    /// </summary>
    double Maximum { get; set; }

    /// <summary>
    /// Event raised when dialog was closed.
    /// </summary>
    event EventHandler Closed;

    /// <summary>
    /// Event raised when dialog was cancelled.
    /// </summary>
    event EventHandler Canceled;

    /// <summary>
    /// Set the progress bar to be indeterminate.
    /// </summary>
    void SetIndeterminate();

    /// <summary>
    ///  Changes if the cancel button should be visible or not.
    /// </summary>
    /// <param name="value">True or false.</param>
    void SetCancelable(bool value);

    /// <summary>
    /// Changes the progress bar value.
    /// </summary>
    /// <param name="value">The percentage to set as value.</param>
    void SetProgress(double value);

    /// <summary>
    /// Changes the message content of the dialog.
    /// </summary>
    /// <param name="message">The message to be displayed.</param>
    void SetMessage(string message);

    /// <summary>
    /// Changes the title of the dialog.
    /// </summary>
    /// <param name="title"></param>
    void SetTitle(string title);

    /// <summary>
    /// Changes the foreground brush of the progress bar.
    /// </summary>
    /// <param name="brush">Brush.</param>
    void SetProgressBarForegroundBrush(Brush brush);

    /// <summary>
    /// Begins an operation to close the dialog.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task CloseAsync();
}