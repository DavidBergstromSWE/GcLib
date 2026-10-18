using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Basler.Pylon;

namespace GcLib;

/// <summary>
/// Provides methods for initializing and terminating the Pylon runtime.
/// </summary>
/// <remarks>Initialization and termination are synchronized. Initialization has no effect if the runtime is
/// already initialized; termination has no effect if it is not initialized.</remarks>
internal static class PylonRuntime
{
    /// <summary>
    /// Provides an object for synchronizing access to shared resources.
    /// </summary>
    private static readonly Lock SyncRoot = new();

    /// <summary>
    /// Initializes the Pylon runtime.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if the assembly containing the global type is not found.</exception>
    /// <exception cref="TypeLoadException">Thrown if the global type cannot be loaded.</exception>
    private static readonly Action InitializeRuntime = GetGlobalMethod("PylonInitialize").CreateDelegate<Action>();

    /// <summary>
    /// Terminates the Pylon runtime when invoked.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if the assembly containing the global type is not found.</exception>
    /// <exception cref="TypeLoadException">Thrown if the global type cannot be loaded.</exception>
    private static readonly Action<bool> TerminateRuntime = GetGlobalMethod("PylonTerminate").CreateDelegate<Action<bool>>();

    /// <summary>
    /// Indicates whether initialization has completed.
    /// </summary>
    private static bool _isInitialized;

    /// <summary>
    /// Initializes the Pylon runtime once.
    /// </summary>
    /// <remarks>Subsequent calls have no effect.</remarks>
    /// <exception cref="FileNotFoundException">Thrown if the assembly containing the global type is not found.</exception>
    /// <exception cref="TypeLoadException">Thrown if the global type cannot be loaded.</exception>
    internal static void Initialize()
    {
        lock (SyncRoot)
        {
            if (_isInitialized)
                return;

            InitializeRuntime();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Terminates the runtime if it is initialized; otherwise, does nothing.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if the assembly containing the global type is not found.</exception>
    /// <exception cref="TypeLoadException">Thrown if the global type cannot be loaded.</exception>
    internal static void Terminate()
    {
        lock (SyncRoot)
        {
            if (_isInitialized == false)
                return;

            TerminateRuntime(true);
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Gets a public static method from the SDK's global type.
    /// </summary>
    /// <param name="methodName">The name of the method to retrieve.</param>
    /// <returns>The requested method.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the assembly containing the global type is not found.</exception>
    /// <exception cref="TypeLoadException">Thrown if the global type cannot be loaded.</exception>
    private static MethodInfo GetGlobalMethod(string methodName)
    {
        // This SDK exposes the required lifecycle methods on a public method of an internal type.
        var globalType = typeof(Camera).Assembly.GetType("Basler.Pylon.Internals.Native.Global", throwOnError: true)!;
        return globalType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(globalType.FullName, methodName);
    }
}
