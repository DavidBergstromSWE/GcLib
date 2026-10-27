using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GcLib.Utilities.FileIO;

public static class FileHelper
{
    /// <summary>
    /// Retrieves file version of assembly (dll) as a string.
    /// </summary>
    /// <param name="filepath">Path to dll.</param>
    /// <returns>File version string.</returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static string GetAssemblyFileVersion(string filepath)
    {
        // Create version string with dots (not commas) and without whitespaces.
        return string.Concat(FileVersionInfo.GetVersionInfo(filepath).FileVersion
                                            .Replace(",", ".")
                                            .Where(c => !char.IsWhiteSpace(c)));
    }

    /// <summary>
    /// Check if assembly is available.
    /// </summary>
    /// <param name="assemblyString">The assembly name or display name.</param>
    /// <returns>True if the assembly is available; otherwise, false.</returns>
    internal static bool IsAssemblyAvailable(string assemblyString)
    {
        try
        {
            Assembly assembly = Assembly.Load(assemblyString);
            return assembly != null; // If the assembly is loaded successfully, it is available.
        }
        catch (FileNotFoundException) // The assembly is not found.
        {
            return false;
        }
        catch (FileLoadException) // The assembly is found but cannot be loaded (e.g., due to version mismatch or other issues).
        {
            return false;
        }
        catch (BadImageFormatException) // The assembly is not a valid assembly (e.g., not a .NET assembly or corrupted).
        {
            return false;
        }
    }
}