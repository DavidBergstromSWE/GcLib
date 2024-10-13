using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FusionViewer.Utilities.Services;

/// <summary>
/// Service providing dispatching and running of actions onto the UI thread.
/// </summary>
internal sealed class DispatcherService : IDispatcherService
{
    /// <inheritdoc/>
    public void Invoke(Action action)
    {
        Dispatcher dispatchObject = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (dispatchObject == null || dispatchObject.CheckAccess())
            action();
        else
            dispatchObject.Invoke(action);
    }

    /// <inheritdoc/>
    public Task Invoke(Func<Task> callback)
    {
        Dispatcher dispatchObject = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (dispatchObject == null || dispatchObject.CheckAccess())
            return callback();
        else
            return dispatchObject.Invoke(callback);
    }

    /// <inheritdoc/>
    public Task<T> Invoke<T>(Func<Task<T>> callback)
    {
        Dispatcher dispatchObject = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (dispatchObject == null || dispatchObject.CheckAccess())
            return callback();
        else
            return dispatchObject.Invoke(callback);
    }
}