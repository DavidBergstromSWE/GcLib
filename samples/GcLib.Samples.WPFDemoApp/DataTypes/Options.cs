using CommandLine;
using System.Collections.Generic;

namespace WPFDemoApp;

/// <summary>
/// Class representing command-line options for the application.
/// </summary>
public class Options
{
    /// <summary>
    /// Optional flag that specifies the path to a configuration file.
    /// </summary>
    [Option(shortName: 'c', longName: "config", Required = false, HelpText = "Path to the configuration file.")]
    public string ConfigurationFilePath { get; set; }

    /// <summary>
    /// Optional flag that specifies one or more device classes to register and use. If none is provided, all available device classes will be registered. The device classes should be specified as a whitespace-separated list of class names.
    /// </summary>

    [Option(shortName: 'd', longName: "device", Required = false, HelpText = "Register the specified device class. If none is provided, all available device classes will be used.")]
    public IEnumerable<string> RegisterDeviceClasses { get; set; }
}
