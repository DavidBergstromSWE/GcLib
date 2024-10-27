using CommunityToolkit.Mvvm.DependencyInjection;

namespace ImagerViewer.ViewModels;

/// <summary>
/// Provides view models in runtime, by resolving instances from an IoC container.
/// </summary>
internal sealed class ViewModelLocator
{
    /// <summary>
    /// View model for the main window.
    /// </summary>
    public static MainWindowViewModel MainWindowViewModel => Ioc.Default.GetRequiredService<MainWindowViewModel>();

    /// <summary>
    /// View model for connection/disconnection of devices and loading/saving device settings. 
    /// </summary>
    public static DeviceViewModel DeviceViewModel => Ioc.Default.GetRequiredService<DeviceViewModel>();

    /// <summary>
    /// View model for the acquisition and recording of image data from an input (camera) channel.
    /// </summary>
    public static AcquisitionViewModel AcquisitionViewModel => Ioc.Default.GetRequiredService<AcquisitionViewModel>();

    /// <summary>
    /// View model for processing of images.
    /// </summary>
    public static ImageProcessingViewModel ImageProcessingViewModel => Ioc.Default.GetRequiredService<ImageProcessingViewModel>();

    /// <summary>
    /// View model for displaying images.
    /// </summary>
    public static ImageDisplayViewModel ImageDisplayViewModel => Ioc.Default.GetRequiredService<ImageDisplayViewModel>();

    /// <summary>
    /// View model for displaying image histograms.
    /// </summary>
    public static HistogramViewModel HistogramViewModel => Ioc.Default.GetRequiredService<HistogramViewModel>();

    /// <summary>
    /// View model for playback of previously recorded image data.
    /// </summary>
    public static PlayBackViewModel PlayBackViewModel => Ioc.Default.GetRequiredService<PlayBackViewModel>();

    /// <summary>
    /// View model for handling UI options.
    /// </summary>
    public static OptionsWindowViewModel OptionsWindowViewModel => Ioc.Default.GetRequiredService<OptionsWindowViewModel>();

    /// <summary>
    /// View model for viewing log file.
    /// </summary>
    public static LogWindowViewModel LogWindowViewModel => Ioc.Default.GetRequiredService<LogWindowViewModel>();

    /// <summary>
    /// View model for viewing shortcuts.
    /// </summary>
    public static ShortcutWindowViewModel ShortcutWindowViewModel => Ioc.Default.GetRequiredService<ShortcutWindowViewModel>();
}