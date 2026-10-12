using System;
using System.Collections.Generic;
using System.Linq;
using GcLib;

namespace WPFDemoApp.Utilities.Helpers;

/// <summary>
/// Provides helper methods for working with <see cref="GcLibrary"/>.
/// </summary>
internal static class GcLibraryHelper
{
    /// <summary>
    /// Returns a list of all valid device classes in the current application (including <see cref="GcLibrary"/>) that implement <see cref="GcDevice"/>, <see cref="IDeviceEnumerator"/> and <see cref="IDeviceClassDescriptor"/>.
    /// </summary>
    /// <returns>A list of valid device classes.</returns>
    public static List<Type> GetValidDeviceClasses()
    {
        // Get all types in the current application domain that implement GcDevice, IDeviceEnumerator, and IDeviceClassDescriptor.
        return [.. AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(assembly => assembly.GetTypes())
                                          .Where(type => typeof(GcDevice).IsAssignableFrom(type) && typeof(IDeviceEnumerator).IsAssignableFrom(type) && typeof(IDeviceClassDescriptor).IsAssignableFrom(type))];
    }
}
