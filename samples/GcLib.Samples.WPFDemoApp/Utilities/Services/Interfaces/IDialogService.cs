using System.Windows;
using Microsoft.Win32;

namespace ImagerViewerApp.Utilities.Services;

/// <summary>
/// Interface for a service providing dialogs (eg. modal windows).
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Opens a new dialog window of type <see cref="Window"/>, with specified <paramref name="viewModel"/> object set as <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    /// <param name="viewModel">Viewmodel (datacontext).</param>
    /// <returns>True if dialog is accepted.</returns>
    bool? ShowDialog(object viewModel);

    /// <summary>
    /// Opens a new dialog window of type <see cref="Window"/>, with viewmodel object of type <typeparamref name="T"/> set as <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    /// <typeparam name="T">Viewmodel type.</typeparam>
    /// <returns>True if dialog is accepted.</returns>
    bool? ShowDialog<T>();

    /// <summary>
    /// Closes all existing and already opened dialog windows of type <see cref="Window"/>, having a viewmodel of type <typeparamref name="T"/> set as <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    /// <typeparam name="T">Viewmodel type.</typeparam>
    void CloseDialog<T>();

    /// <summary>
    /// Displays a standard <see cref="OpenFileDialog"/> that prompts the user to select one or multiple files to open.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="multiSelect">Indicates whether multiple files can be selected.</param>
    /// <param name="filter">File name filter string.</param>
    /// <param name="defaultExtension">Default file name extension.</param>
    /// <param name="defaultPath">Initial directory displayed by dialog.</param>
    /// <returns>Selected file path.</returns>
    string ShowOpenFileDialog(string title, bool multiSelect, string filter, string defaultExtension, string defaultPath = "");

    /// <summary>
    /// Displays a standard <see cref="SaveFileDialog"/> that prompts the user to specify a filename to save a file as.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="fileName">Filename.</param>
    /// <param name="filter">File name filter string.</param>
    /// <param name="defaultExtension">Default file name extension.</param>
    /// <param name="defaultPath">Initial directory displayed by dialog.</param>
    /// <returns>Selected file path.</returns>
    string ShowSaveFileDialog(string title, string fileName, string filter, string defaultExtension, string defaultPath = "");

    /// <summary>
    /// Displays a standard <see cref="OpenFolderDialog"/> that prompts the user to select a folder.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="defaultPath">Initial directory displayed by dialog.</param>
    /// <returns>Selected folder path.</returns>
    string ShowOpenFolderDialog(string title, string defaultPath = "");
}