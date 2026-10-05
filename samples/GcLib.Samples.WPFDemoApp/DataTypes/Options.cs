using CommandLine;
using System.Collections.Generic;

namespace WPFDemoApp;

/// <summary>
/// Class representing command-line options for the application.
/// </summary>
public class Options
{
    /// <summary>
    /// Optional flag that specifies the path to the configuration file. If not provided, the application will use the default configuration.
    /// </summary>
    [Option(shortName: 'c', longName: "config", Required = false, HelpText = "Path to the configuration file.")]
    public string InputFile { get; set; }

    /// <summary>
    /// Optional flag that specifies the device class to use. If not provided, all available device classes will be used.
    /// </summary>

    [Option(shortName: 'd', longName: "device", Required = false, HelpText = "Use the specified device class. If not provided, all available device classes will be used.")]
    public IEnumerable<string> RegisterDeviceClasses { get; set; }
}
