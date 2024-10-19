using System;
using System.Threading;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace ImagerViewerApp.Utilities.Services;

/// <summary>
/// Interface for a service providing dialogs and windows to the application. The interface extends <see cref="IDialogService"/> and <see cref="IWindowService"/> with functionality specific to the <see cref="MahApps.Metro"/> framework.
/// </summary>
public interface IMetroWindowService : IWindowService, IDialogService
{
    /// <summary>
    /// Shows a message dialog outside the current window.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message contained in the dialog.</param>
    /// <param name="style">Button styles for the dialog.</param>
    /// <param name="settings">Optional Settings that override the global metro dialog settings.</param>
    /// <returns>The result of which button was pressed.</returns>
    MessageDialogResult ShowMessageDialog(object viewModel, string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null);

    /// <summary>
    /// Shows a progress dialog inside the current window.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message contained in the dialog.</param>
    /// <param name="isCancelable">Determines if the cancel button is visible.</param>
    /// <param name="settings">Optional Settings that override the global metro dialog settings.</param>
    /// <returns>A task promising the instance of an object implementing <see cref="IProgressController"/>.</returns>
    Task<IProgressController> ShowProgressDialogAsync(object viewModel, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null);

    /// <summary>
    /// Shows a progress dialog inside the current window, with an optional time delay before displaying it to the user.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message contained in the dialog.</param>
    /// <param name="isCancelable">Determines if the cancel button is visible.</param>
    /// <param name="settings">Optional Settings that override the global metro dialog settings.</param>
    /// <param name="progress">Optional provider of progress report.</param>
    /// <param name="tokenSource">Optional token source for signaling a cancelled operation.</param>
    /// <param name="delay">Optional time delay before showing the dialog.</param>
    /// <returns>A task promising the instance of an object implementing <see cref="IProgressController"/>.</returns>
    Task ShowProgressDialogAsync(object viewModel, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null, Progress<double> progress = null, CancellationTokenSource tokenSource = null, int delay = 1000);

    /// <summary>
    /// Shows a message dialog inside the current window.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message contained in the dialog.</param>
    /// <param name="style">Button styles for the dialog.</param>
    /// <param name="settings">Optional Settings that override the global metro dialog settings.</param>
    /// <returns>A task promising the result of which button was pressed.</returns>
    Task<MessageDialogResult> ShowMessageAsync(object viewModel, string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null);

    /// <summary>
    /// Get the dialog currently shown.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <returns>Task promising the dialog instance.</returns>
    Task<TDialog> GetCurrentDialogAsync<TDialog>(object viewModel) where TDialog : BaseMetroDialog;

    /// <summary>
    /// Shows an input dialog, with a user query and a default input response.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="query">User query.</param>
    /// <param name="defaultInput">Default input response.</param>
    /// <returns>Input from user.</returns>
    string ShowInputDialog(object viewModel, string title, string query, string defaultInput);
}