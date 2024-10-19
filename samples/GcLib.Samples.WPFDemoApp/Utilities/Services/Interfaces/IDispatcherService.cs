using System;
using System.Threading.Tasks;

namespace ImagerViewer.Utilities.Services;

/// <summary>
/// Interface for a service providing dispatching and execution of action delegates onto the UI thread.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Executes an action on the UI thread.
    /// </summary>
    /// <param name="action">Delegate to invoke.</param>
    void Invoke(Action action);

    /// <summary>
    /// Executes the specified <see cref="Task"/> on the UI thread.
    /// </summary>
    /// <param name="callback">Delegate to invoke.</param>
    /// <returns>(awaitable) <see cref="Task"/> returned by the callback.</returns>
    Task Invoke(Func<Task> callback);

    /// <summary>
    ///  Executes the specified callback returning <see cref="Task{TResult}"/> synchronously on the UI thread.
    /// </summary>
    /// <typeparam name="TResult">The return value type of the task.</typeparam>
    /// <param name="callback">Delegate to invoke.</param>
    /// <returns>(awaitable) <see cref="Task{TResult}"/> returned by the callback.</returns>
    Task<TResult> Invoke<TResult>(Func<Task<TResult>> callback);
}