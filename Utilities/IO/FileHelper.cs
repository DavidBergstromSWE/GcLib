using System.Diagnostics;
using System.Linq;
using System.IO;

namespace GcLib.Utilities.IO;

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
}