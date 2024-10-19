using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace ImagerViewer.Utilities.Services;

/// <summary>
/// Service providing windows and dialogs adapted to an application using the <see cref="MahApps.Metro"/> framework.
/// </summary>
internal sealed class MetroWindowService : WindowService, IMetroWindowService
{
    #region Fields

    /// <summary>
    /// Dialog coordinator provided by <see cref="MahApps.Metro"/>.
    /// </summary>
    private readonly IDialogCoordinator _dialogCoordinator;

    #endregion

    /// <summary>
    /// Creates a new service providing windows and dialogs adapted to an application using the <see cref="MahApps.Metro"/> framework.
    /// </summary>
    /// <param name="dialogCoordinator">Dialog coordinator provided by <see cref="MahApps.Metro"/>.</param>
    public MetroWindowService(IDialogCoordinator dialogCoordinator = null)
    {
        if (dialogCoordinator == null)
            _dialogCoordinator = DialogCoordinator.Instance;
    }

    /// <inheritdoc/>
    public Task<TDialog> GetCurrentDialogAsync<TDialog>(object viewModel) where TDialog : BaseMetroDialog
    {
        return _dialogCoordinator.GetCurrentDialogAsync<TDialog>(viewModel);
    }

    /// <inheritdoc/>
    public MessageDialogResult ShowMessageDialog(object viewModel, string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
    {
        return _dialogCoordinator.ShowModalMessageExternal(viewModel, title, message, style, settings);
    }

    /// <inheritdoc/>
    public Task<MessageDialogResult> ShowMessageAsync(object viewModel, string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
    {
        return _dialogCoordinator.ShowMessageAsync(viewModel, title, message, style, settings);
    }

    /// <inheritdoc/>
    public async Task<IProgressController> ShowProgressDialogAsync(object viewModel, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
    {
        var controller = await _dialogCoordinator.ShowProgressAsync(viewModel, title, message, isCancelable, settings);
        return new ProgressController(controller);
    }

    /// <inheritdoc/>
    public async Task ShowProgressDialogAsync(object viewModel, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null, Progress<double> progress = null, CancellationTokenSource tokenSource = null, int delay = 1000)
    {
        try
        {
            // Add time delay before showing dialog.
            await Task.Delay(delay, settings.CancellationToken);

            // Open progress dialog.
            var controller = await _dialogCoordinator.ShowProgressAsync(context: viewModel,
                                                                        title: title,
                                                                        message: message,
                                                                        isCancelable: isCancelable,
                                                                        settings: settings);
            // Add eventhandler to user cancellations of dialog.
            controller.Canceled += async (s, e) =>
            {
                // Close dialog.
                if (controller.IsOpen)
                    await controller.CloseAsync();

                // Signal task cancellation.
                tokenSource?.Cancel();
            };

            // Configure progress setting of dialog.
            if (progress == null)
            {
                // Show indeterminate progress ring.
                controller.SetIndeterminate();
            }
            else
            {
                // Set progress bar restrictions.
                controller.Minimum = 0.0;
                controller.Maximum = 1.0;

                // Show progress bar.
                controller.SetProgress(controller.Minimum);

                // Add eventhandler to progress changes.
                progress.ProgressChanged += (s, p) =>
                {
                    // Update progress in dialog.
                    controller.SetProgress(p);
                };
            }

            // Add eventhandler to closing of dialog.
            controller.Closed += (s, e) =>
            {
                // Unsubscribe from progress changes.
                if (progress != null)
                    progress.ProgressChanged -= (s, p) => { controller.SetProgress(p); };
            };

            return;
        }
        catch (OperationCanceledException)
        {
            // Need to clean up anything? Otherwise, just catch and continue.
        }
    }

    #region IDialogService

    /// <inheritdoc/>
    /// <exception cref="TypeLoadException"/>
    /// <exception cref="InvalidOperationException"/>
    public bool? ShowDialog(object viewModel)
    {
        Window window = CreateWindow(viewModel);

        // Hook eventhandlers.
        window.Loaded += Window_Loaded;
        window.Closed += Window_Closed;

        return window.ShowDialog();
    }

    /// <inheritdoc/>
    /// <exception cref="TypeLoadException"></exception>
    /// <exception cref="MissingMethodException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public bool? ShowDialog<T>()
    {
        Window window = CreateWindow<T>();

        // Hook eventhandlers.
        window.Loaded += Window_Loaded;
        window.Closed += Window_Closed;

        return window.ShowDialog();
    }

    /// <inheritdoc/>
    public void CloseDialog<T>()
    {
        // Find windows of viewtype.
        IEnumerable<Window> windows = Application.Current.MainWindow.OwnedWindows.Cast<Window>().Where(p => p.DataContext != null && p.DataContext.GetType() == typeof(T));
        if (windows.Any())
        {
            foreach (var window in windows)
                window?.Close();
        }
    }

    /// <inheritdoc/>
    public string ShowOpenFileDialog(string title, bool multiSelect, string filter, string defaultExtension, string defaultPath = "")
    {
        var openFileDialog = new OpenFileDialog
        {
            InitialDirectory = defaultPath,
            Title = title,
            Multiselect = multiSelect,
            Filter = filter,
            DefaultExt = defaultExtension,
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
    }

    /// <inheritdoc/>
    public string ShowSaveFileDialog(string title, string fileName, string filter, string defaultExtension, string defaultPath = "")
    {
        var saveFileDialog = new SaveFileDialog
        {
            InitialDirectory = defaultPath,
            Title = title,
            FileName = fileName,
            Filter = filter,
            DefaultExt = defaultExtension
        };

        return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : string.Empty;
    }

    /// <inheritdoc/>
    public string ShowOpenFolderDialog(string title, string defaultPath = "")
    {
        var folderDialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = defaultPath,
            Multiselect = false,
        };
    
        return folderDialog.ShowDialog() == true ? folderDialog.FolderName : string.Empty;
    }

    /// <inheritdoc/>
    public string ShowInputDialog(object viewModel, string title, string query, string defaultInput)
    {
        var input = _dialogCoordinator.ShowModalInputExternal(viewModel, title, query, new MetroDialogSettings() { DefaultText = defaultInput });
        return string.IsNullOrEmpty(input) ? defaultInput : input;
    }

    #endregion
}