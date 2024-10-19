using System.Threading;
using System.Threading.Tasks;

namespace ImagerViewerApp.Utilities.IO;

/// <summary>
/// Interface for a service managing the storing and restoring of a configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Restores configuration from a file.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>(awaitable) Task.</returns>
    Task RestoreAsync(string filePath, CancellationToken token);

    /// <summary>
    /// Stores configuration to a file.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    Task StoreAsync(string filePath);
}