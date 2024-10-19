using GcLib;

namespace ImagerViewer;

/// <summary>
/// Device index.
/// </summary>
public enum DeviceIndex
{
    /// <summary>
    /// First device.
    /// </summary>
    Device1 = 1,
    /// <summary>
    /// Second device.
    /// </summary>
    Device2
}

/// <summary>
/// Image channel for display.
/// </summary>
public enum DisplayChannel
{
    /// <summary>
    /// Input channel 1.
    /// </summary>
    Channel1 = 1,
    /// <summary>
    /// Input channel 2.
    /// </summary>
    Channel2,
    /// <summary>
    /// Fused output channel.
    /// </summary>
    FusedChannel,
    /// <summary>
    /// All channels.
    /// </summary>
    All
}

/// <summary>
/// Device parameter visibility.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Features that should be visible for all users via the GUI and API (default).
    /// </summary>
    Beginner = GcVisibility.Beginner,
    /// <summary>
    /// Features that require a more in-depth knowledge of the camera functionality.
    /// </summary>
    Expert = GcVisibility.Expert,
    /// <summary>
    /// Advanced features that might bring the cameras into a state where it will not work properly anymore if it is set incorrectly for the cameras current mode of operation.
    /// </summary>
    Guru = GcVisibility.Guru
}

/// <summary>
/// Image resizing direction.
/// </summary>
public enum ScalingDirection
{
    /// <summary>
    /// Upscaling of image size.
    /// </summary>
    UpScale = 0,
    /// <summary>
    /// Downscaling of image size.
    /// </summary>
    DownScale
}

/// <summary>
/// Base color for themes.
/// </summary>
public enum ThemeBaseColor
{
    /// <summary>
    /// Light background with dark text.
    /// </summary>
    Light = 0,
    /// <summary>
    /// Dark background with light text.
    /// </summary>
    Dark = 1
}