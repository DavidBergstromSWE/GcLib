using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Emgu.CV;
using ImagerViewer.Models;
using ImagerViewer.Utilities.IO;
using ImagerViewer.Utilities.Logging;
using ImagerViewer.Utilities.Services;
using ImagerViewer.Utilities.Themes;
using ImagerViewer.ViewModels;
using GcLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ImagerViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Enables and disables logging of application events. 
    /// </summary>
    public static bool IsLoggingEnabled { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create storage of logging data.
        var logModel = new LogModel();

        // Configure logging.
        Log.Logger = new LoggerConfiguration()
            .Filter.ByExcluding(_ => !IsLoggingEnabled)
            .MinimumLevel.Verbose()
            .WriteTo.LogModelSink(logModel: logModel, minimumLevel: Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.StatusBarSink(messenger: WeakReferenceMessenger.Default, minimumLevel: Serilog.Events.LogEventLevel.Information)
            .WriteTo.File(path: @"log.txt",
                          rollingInterval: RollingInterval.Day,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
#if DEBUG
            .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
#endif
            .CreateLogger();

        // Enable logging of unhandled exceptions.
        SetupUnhandledExceptionLogging();

        // Start logging.
        IsLoggingEnabled = true;

        Log.Information("{App} started (v{version})", MainWindowViewModel.Title, MainWindowViewModel.MajorMinorVersion); 

        // Configure services for dependency injection.
        Ioc.Default.ConfigureServices(
            new ServiceCollection()          
            .AddTransient<IDispatcherService, DispatcherService>()
            .AddTransient<IThemeService, ThemeService>()
            .AddScoped<IMetroWindowService, MetroWindowService>()
            .AddSingleton<IConfigurationService, ConfigurationService>()
            .AddSingleton<ISettingsService, ApplicationSettingsService>()
            .AddScoped<MainWindowViewModel>()
            .AddScoped<ImageProcessingViewModel>()
            .AddScoped<ImageDisplayViewModel>()
            .AddScoped<DeviceViewModel>()
            .AddScoped<PlayBackViewModel>()
            .AddScoped<AcquisitionViewModel>()
            .AddScoped<HistogramViewModel>()
            .AddTransient<OptionsWindowViewModel>()
            .AddTransient<LogWindowViewModel>()
            .AddTransient<ShortcutWindowViewModel>()
            .AddSingleton(logModel)
            .AddSingleton(new ImageModel())
            .AddSingleton(new DeviceModel())
            .AddSingleton<IDeviceProvider, GcSystem>()
            .AddLogging(loggingBuilder => loggingBuilder.AddSerilog())
            .BuildServiceProvider());

        InitializeLibraries();

        Log.Debug("Services configured");

        // Restore user settings to UI.
        Ioc.Default.GetRequiredService<ISettingsService>().RestoreSettings();
        Log.Debug("Application settings restored");

        // Shut down all child windows on main window closing.
        Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    /// <summary>
    /// Setup logging of unhandled exceptions raised in application.
    /// </summary>
    private void SetupUnhandledExceptionLogging()
    {
        // Logs all main UI thread related exceptions.
        DispatcherUnhandledException += (s, e) => { LogUnhandledException(e.Exception, "DispatcherUnhandledException", s.ToString()); e.Handled = true; Current.Shutdown(-1); };

        // Logs all other exceptions in background threads.
        AppDomain.CurrentDomain.UnhandledException += (s, e) => { LogUnhandledException(e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException", s?.ToString()); Environment.Exit(1); };

        // Logs exceptions from uses of a task scheduler for async operations.
        TaskScheduler.UnobservedTaskException += (s, e) => { LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException", s.ToString()); e.SetObserved(); };
    }

    /// <summary>
    /// Logs an unhandled exception.
    /// </summary>
    /// <param name="exceptionObject">Exception object.</param>
    /// <param name="type">Exception type.</param>
    /// <param name="source">Exception source.</param>
    private static void LogUnhandledException(object exceptionObject, string type, string source)
    {
        // Check if object is null.
        if (exceptionObject is not Exception ex)
            ex = new NotSupportedException("Unhandled exception: " + exceptionObject.ToString());

        Log.Fatal(ex, "Unhandled exception of type {Type} raised by {Source}!", type, source);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Persist user settings.
        Ioc.Default.GetRequiredService<ISettingsService>().StoreSettings();
        Log.Debug("Application settings stored");

        // Close libraries.
        CloseLibraries();

        Log.Information("{App} closed", MainWindowViewModel.Title);
        Log.CloseAndFlush();

        base.OnExit(e);
    }

    /// <summary>
    /// Initializes libraries used in the application.
    /// </summary>
    private static void InitializeLibraries()
    {
        // Initialize EmguCV.
        if (CvInvoke.Init() == false)
            throw new InvalidOperationException("Emgu CV could not be initialized!");

        // Initialize GcLib with logger.
        GcLibrary.Init(logger: Ioc.Default.GetService<ILogger<App>>());
    }

    /// <summary>
    /// Close libraries used in the application.
    /// </summary>
    private static void CloseLibraries()
    {
        // Dispose system level in GcLib.
        var system = Ioc.Default.GetRequiredService<IDeviceProvider>() as GcSystem;
        system?.Dispose();

        // Close GcLib.
        GcLibrary.Close();
    }
}