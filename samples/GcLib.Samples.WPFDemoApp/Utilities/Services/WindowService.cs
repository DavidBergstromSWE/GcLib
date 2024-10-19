using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using ImagerViewer.Views;
using Serilog;

namespace ImagerViewer.Utilities.Services;

/// <summary>
/// Service providing windows.
/// </summary>
internal class WindowService : IWindowService
{
    #region Public methods

    /// <inheritdoc/>
    /// <exception cref="TypeLoadException"/>
    /// <exception cref="InvalidOperationException"/>
    public void ShowWindow(object viewModel)
    {
        // Return if window is already open.
        if (IsOpen(viewModel))
            return;

        // Resolve window from object.
        Window window = CreateWindow(viewModel);

        // Hook eventhandlers.
        window.Loaded += Window_Loaded;
        window.Closed += Window_Closed;

        // Show window.
        window.Show();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Remarks: The method requires viewmodel to have a parameterless constructor and will throw a <see cref="MissingMethodException"/> if not found.
    /// </remarks>
    /// <exception cref="TypeLoadException"></exception>
    /// <exception cref="MissingMethodException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void ShowWindow<T>()
    {
        // Return if window is already open.
        if (IsOpen<T>())
            return;

        // Resolve window from type.
        Window window = CreateWindow<T>();

        // Hook eventhandlers.
        window.Loaded += Window_Loaded;
        window.Closed += Window_Closed;

        // Show window.
        window.Show();
    }

    /// <inheritdoc/>
    public void CloseWindow<T>()
    {
        IEnumerable<Window> windows = Application.Current.MainWindow.OwnedWindows.Cast<Window>().Where(w => w.DataContext != null && w.DataContext.GetType() == typeof(T));
        foreach (Window window in windows)
            window.Close();
    }

    /// <inheritdoc/>
    public void CloseAllWindows()
    {
        WindowCollection childWindows = Application.Current.MainWindow.OwnedWindows;
        foreach (Window window in childWindows)
        {
            if (window != Application.Current.MainWindow)
                window.Close();
        }
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Resolves and creates a new <see cref="Window"/> with <paramref name="viewModel"/> set as DataContext.
    /// </summary>
    /// <remarks>
    /// Remarks: Windows can only be resolved if they are named using standard MVVM naming convention (eg. MainWindow is paired with MainWindowViewModel and AboutDialogWindow is paried with AboutDialogWindowViewModel etc.) 
    /// and located in the same namespace as <see cref="MainWindow"/>.
    /// </remarks>
    /// <param name="viewModel">Instantiated viewmodel.</param>
    /// <returns><see cref="Window"/> instantiated with <paramref name="viewModel"/> as DataContext.</returns>
    /// <exception cref="TypeLoadException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    protected static Window CreateWindow(object viewModel)
    {
        // Retrieve assembly containing MainWindow.
        Assembly assembly = typeof(MainWindow).Assembly;

        try
        {
            // Resolve window type by naming convention.
            Type viewType = assembly.GetType(name: typeof(MainWindow).Namespace + "." + viewModel.GetType().Name.Replace("ViewModel", string.Empty), throwOnError: true);

            // Instantiate as window.
            if (viewType.GetConstructor(Type.EmptyTypes).Invoke([]) is Window window)
            {
                // Set viewmodel as datacontext and MainWindow as owner.
                window.DataContext = viewModel;
                window.Owner = Application.Current.MainWindow;
                return window;
            }
            else throw new InvalidOperationException("Associated view is not a window!");
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Resolves and creates a new <see cref="Window"/> with a viewmodel of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Viewmodel type.</typeparam>
    /// <remarks>
    /// Remarks: Windows can only be resolved if they are named using standard MVVM naming convention (eg. MainWindow is paired with MainWindowViewModel and AboutDialogWindow is paried with AboutDialogWindowViewModel etc.) 
    /// and located in the same namespace as <see cref="MainWindow"/>.
    /// </remarks>
    /// <returns><see cref="Window"/> instantiated with a new viewmodel instance of type <typeparamref name="T"/> as DataContext.</returns>
    /// <exception cref="TypeLoadException"></exception>
    /// <exception cref="MissingMethodException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    protected static Window CreateWindow<T>()
    {
        // Retrieve assembly containing MainWindow.
        Assembly assembly = typeof(MainWindow).Assembly;

        try
        {
            // Resolve window type by naming convention.
            Type viewType = assembly.GetType(name: typeof(MainWindow).Namespace + "." + typeof(T).Name.Replace("ViewModel", string.Empty), throwOnError: true);

            // Instantiate as window.
            if (viewType.GetConstructor(Type.EmptyTypes).Invoke([]) is Window window)
            {
                // If DataContext is not already set, invoke constructor of viewmodel.
                if (window.DataContext == null || window.DataContext.GetType() != typeof(T))
                {
                    // Instantiate viewmodel and set it as datacontext.
                    var ctorInfo = typeof(T).GetConstructor(Type.EmptyTypes);
                    if (ctorInfo != null)
                        window.DataContext = typeof(T).GetConstructor(Type.EmptyTypes).Invoke([]);
                    else throw new MissingMethodException($"Could not instantiate viewmodel as it does not have an accessible parameterless constructor!");
                }

                // Set MainWindow to window owner.
                window.Owner = Application.Current.MainWindow;
                return window;
            }
            else throw new InvalidOperationException("Associated view is not a window!");
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Checks if <see cref="Window"/> with viewmodel of type <typeparamref name="T"/> as DataContext is open.
    /// </summary>
    /// <typeparam name="T">Viewmodel type.</typeparam>
    /// <returns>True if window is open.</returns>
    protected static bool IsOpen<T>()
    {
        IEnumerable<Window> windows = Application.Current.MainWindow.OwnedWindows.Cast<Window>().Where(w => w.DataContext != null && w.DataContext.GetType() == typeof(T));
        return windows.Any();
    }

    /// <summary>
    /// Checks if <see cref="Window"/> with <paramref name="viewModel"/> as DataContext is open.
    /// </summary>
    /// <param name="viewModel">Viewmodel instance.</param>
    /// <returns>True if window is open.</returns>
    protected static bool IsOpen(object viewModel)
    {
        IEnumerable<Window> windows = Application.Current.MainWindow.OwnedWindows.Cast<Window>().Where(w => w.DataContext != null && w.DataContext.GetType() == viewModel.GetType());
        return windows.Any();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Eventhandler executed when a window has been closed.
    /// </summary>
    protected void Window_Closed(object sender, EventArgs e)
    {
        var window = (Window)sender;
        Log.Verbose("{Title} window closed", window.Title);

        // Unhook eventhandlers.
        window.Loaded -= Window_Loaded;
        window.Closed -= Window_Closed;
    }

    /// <summary>
    /// Eventhandler executed when a window has been loaded.
    /// </summary>
    protected void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var window = (Window)sender;
        Log.Verbose("{Title} window opened", window.Title);
    }

    #endregion
}