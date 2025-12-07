using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ImagerViewer.Models;
using ImagerViewer.UserControls;
using GcLib;
using ScottPlot;
using Serilog.Events;

namespace ImagerViewer.ViewModels;

/// <summary>
/// Provides view models in design time, being populated by dummy properties and commands.
/// </summary>
internal sealed class MockViewModelLocator
{
    /// <summary>
    /// View model for connection/disconnection of devices and loading/saving device settings. 
    /// </summary>
    public static MockDeviceViewModel DeviceViewModel => new();

    /// <summary>
    /// View model for processing of images.
    /// </summary>
    public static MockImageProcessingViewModel ImageProcessingViewModel => new();

    /// <summary>
    /// View model for displaying image histograms.
    /// </summary>
    public static MockHistogramViewModel HistogramViewModel => new();

    /// <summary>
    /// View model for the selection of a device to open from a list of available devices on the system.
    /// </summary>
    public static MockOpenDeviceDialogWindowViewModel OpenDeviceDialogWindowViewModel => new();

    /// <summary>
    /// View model for displaying logging information about current application session.
    /// </summary>
    public static MockLogWindowViewModel LogWindowViewModel => new();

    // Add mock for ImageDisplayViewModel?
}

/// <summary>
/// Mockup for <see cref="DeviceViewModel"/>, to be used in design mode.
/// </summary>
internal sealed class MockDeviceViewModel
{
    public static DeviceModel Device { get; set; } = new("VirtualCam", "MySimLabs", "VirtualCam1") { IsConnected = true };
    public static bool IsEnabled => true;
    public static ICommand ConnectCameraFromDialogCommand { get; }
    public static ICommand OpenParameterDialogWindowCommand { get; }
}

/// <summary>
/// Mockup for <see cref="ImageProcessingViewModel"/>, to be used in design mode.
/// </summary>
internal sealed class MockImageProcessingViewModel
{
    public static ImageModel SelectedImageChannel { get; set; } = new() { Brightness = 100.0 };
    public static ICommand OpenParameterDialogWindowCommand { get; }
}

/// <summary>
/// Mockup for <see cref="OpenDeviceDialogWindowViewModel"/>, to be used in design mode.
/// </summary>
internal sealed class MockOpenDeviceDialogWindowViewModel
{
    public static GcDeviceInfo SelectedDevice { get; set; } = new GcDeviceInfo(vendorName: "MySimLabs",
                                                                               modelName: "VirtualCam",
                                                                               serialNumber: "1.0",
                                                                               uniqueID: "VirtualCam1",
                                                                               deviceClass: _classInfo,
                                                                               userDefinedName: "MyCameraSimulator");
    public static List<GcDeviceInfo> DeviceList => [SelectedDevice];

    private static readonly GcDeviceClassInfo _classInfo = new(SelectedDevice.ModelName, SelectedDevice.SerialNumber, typeof(VirtualCam));
}

/// <summary>
/// Mockup for <see cref="OptionsDeviceViewModel"/>, to be used in design mode.
/// </summary>
internal sealed class MockOptionsDeviceViewModel
{
    public static string Name => "Devices";

    public static Visibility SelectedVisibility => Visibility.Guru;

    public static uint DeviceParameterUpdateTimeDelay { get; set; } = 500;
}

/// <summary>
/// Mockup for <see cref="HistogramViewModel"/>, to be used in design mode.
/// </summary>
internal sealed class MockHistogramViewModel
{
    public static bool IsEnabled => true;

    public static ImageHistogram Histogram => new(SampleData.MonaLisa(), 255, 0);

    public static bool ShowGrid => true;

    public static bool ShowLiveHistogram => true;

    public static bool ShowProcessed => true;

    public static HistogramPlotType SelectedPlotType => HistogramPlotType.Fill;

    public static int SelectedHistogramSize => 64;
}

/// <summary>
/// Mockup for <see cref="LogWindowViewModel"/>, to be used in design mode.
/// </summary>
internal sealed class MockLogWindowViewModel
{
    /// <summary>
    /// Fake collection of logging events (usable as design time data).
    /// </summary>
    public static List<LogEvent> LogEvents => [
        new LogEvent(new DateTime(2023, 1, 4, 12, 25, 37, 32), LogEventLevel.Information, "ImageViewer started (v1.0.0)"),
        new LogEvent(new DateTime(2023, 1, 4, 12, 25, 37, 532), LogEventLevel.Debug, "VirtualCam added (VirtualCam v1.0.0.0)"),
        new LogEvent(new DateTime(2023, 1, 4, 12, 25, 37, 759), LogEventLevel.Warning, "Unable to add device class of type PvCam"),
        new LogEvent(new DateTime(2023, 1, 4, 12, 25, 39, 120), LogEventLevel.Error, "Failed to open device!"),
        new LogEvent(new DateTime(2023, 1, 4, 12, 25, 40, 717), LogEventLevel.Verbose, "A really verbose message saying absolutely nothing")
    ];

    public static ObservableCollection<LogEventLevel> LogLevels => LogWindowViewModel.LogLevels;

    public static LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Debug;
}