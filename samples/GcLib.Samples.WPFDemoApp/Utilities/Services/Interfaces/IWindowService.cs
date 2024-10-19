using System.Windows;

namespace ImagerViewerApp.Utilities.Services;

/// <summary>
/// Interface for a service providing windows of type <see cref="Window"/>.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Opens a new <see cref="Window"/> with instantiated <paramref name="viewModel"/> object set as <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    /// <param name="viewModel">Instantiated viewmodel.</param>
    void ShowWindow(object viewModel);

    /// <summary>
    /// Opens a new <see cref="Window"/> with viewmodel of type <typeparamref name="T"/> set as <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    /// <typeparam name="T">Viewmodel type.</typeparam>
    void ShowWindow<T>();

    /// <summary>
    /// Closes an existing and already opened <see cref="Window"/> having a viewmodel of type <typeparamref name="T"/> set as <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    /// <typeparam name="T">Type of window datacontext (viewmodel).</typeparam>
    void CloseWindow<T>();

    /// <summary>
    /// Closes all existing and already opened windows of type <see cref="Window"/>, being a child of the <see cref="Application.MainWindow"/> (main window of the application).
    /// </summary>
    void CloseAllWindows();
}