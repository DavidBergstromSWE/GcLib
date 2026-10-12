using Microsoft.Extensions.DependencyInjection;

namespace WPFDemoApp.ViewModels;

/// <summary>
/// Provides view models in runtime, by resolving instances from the service provider of the application.
/// </summary>
internal sealed class ViewModelLocator
{
    /// <summary>
    /// View model for the main window.
    /// </summary>
    public static MainWindowViewModel MainWindowViewModel => App.Current.Services.GetRequiredService<MainWindowViewModel>();

    /// <summary>
    /// View model for connection/disconnection of devices and loading/saving device settings. 
    /// </summary>
    public static DeviceViewModel DeviceViewModel => App.Current.Services.GetRequiredService<DeviceViewModel>();

    /// <summary>
    /// View model for the acquisition and recording of image data from an input (camera) channel.
    /// </summary>
    public static AcquisitionViewModel AcquisitionViewModel => App.Current.Services.GetRequiredService<AcquisitionViewModel>();

    /// <summary>
    /// View model for processing of images.
    /// </summary>
    public static ImageProcessingViewModel ImageProcessingViewModel => App.Current.Services.GetRequiredService<ImageProcessingViewModel>();

    /// <summary>
    /// View model for displaying images.
    /// </summary>
    public static ImageDisplayViewModel ImageDisplayViewModel => App.Current.Services.GetRequiredService<ImageDisplayViewModel>();

    /// <summary>
    /// View model for displaying image histograms.
    /// </summary>
    public static HistogramViewModel HistogramViewModel => App.Current.Services.GetRequiredService<HistogramViewModel>();

    /// <summary>
    /// View model for playback of previously recorded image data.
    /// </summary>
    public static PlayBackViewModel PlayBackViewModel => App.Current.Services.GetRequiredService<PlayBackViewModel>();

    /// <summary>
    /// View model for handling UI options.
    /// </summary>
    public static OptionsWindowViewModel OptionsWindowViewModel => App.Current.Services.GetRequiredService<OptionsWindowViewModel>();

    /// <summary>
    /// View model for viewing log file.
    /// </summary>
    public static LogWindowViewModel LogWindowViewModel => App.Current.Services.GetRequiredService<LogWindowViewModel>();

    /// <summary>
    /// View model for viewing shortcuts.
    /// </summary>
    public static ShortcutWindowViewModel ShortcutWindowViewModel => App.Current.Services.GetRequiredService<ShortcutWindowViewModel>();
}