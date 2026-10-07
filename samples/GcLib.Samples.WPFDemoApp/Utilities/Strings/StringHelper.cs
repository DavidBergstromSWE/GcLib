using System.Collections.Generic;
using System.Linq;

namespace WPFDemoApp.Utilities.Strings;

/// <summary>
/// Helper class for string operations.
/// </summary>
internal static class StringHelper
{
    /// <summary>
    /// Joins a list of strings with commas and "and" before the last item.
    /// </summary>
    /// <param name="strings">Strings to join.</param>
    /// <returns>A string with the items joined by commas and "and" before the last item.</returns>
    public static string JoinWithAnd(IEnumerable<string> strings)
    {
        if (strings == null || !strings.Any()) return string.Empty;

        // Convert to array for easier indexing.
        var array = strings.ToArray();

        if (array.Length == 1) return array[0];
        if (array.Length == 2) return $"{array[0]} and {array[1]}";

        // Join all but the last item with commas, and add "and" before the last item.
        return string.Join(", ", array.Take(array.Length - 1)) + " and " + array.Last();
    }
}
