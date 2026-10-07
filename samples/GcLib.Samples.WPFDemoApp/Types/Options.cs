using CommandLine;
using System.Collections.Generic;

namespace WPFDemoApp;

/// <summary>
/// Class representing command-line options for the application.
/// </summary>
public class Options
{
    /// <summary>
    /// Optional flag that specifies the path to a system configuration file.
    /// </summary>
    [Option(shortName: 'c', longName: "config", Required = false, HelpText = "Path to system configuration file (xml).")]
    public string ConfigurationFilePath { get; set; }

    /// <summary>
    /// Optional flag that specifies one or more device classes (of type <see cref="GcLib.GcDevice"/>) to register for use. If none is provided, all device classes available will be registered. 
    /// If multiple device classes are specified, they should be enumerated as a whitespace-separated list of class names.
    /// </summary>

    [Option(shortName: 'd', longName: "device", Required = false, HelpText = "Specifies one or more device classes to register. If none is provided, all available device classes will be used.")]
    public IEnumerable<string> DeviceClasses { get; set; }
}
