using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommandLine;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Emgu.CV;
using GcLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using WPFDemoApp.Models;
using WPFDemoApp.Utilities.Strings;
using WPFDemoApp.Utilities.IO;
using WPFDemoApp.Utilities.Logging;
using WPFDemoApp.Utilities.Services;
using WPFDemoApp.Utilities.Themes;
using WPFDemoApp.ViewModels;

namespace WPFDemoApp;

/// <summary>
/// ImageViewer is a demo app for the <see cref="GcLib"/> library. The app demonstrates how to connect to devices, change device parameter settings and display live image streams. The app also provide some elementary recording and playback functionality.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Enables and disables logging of application events. 
    /// </summary>
    public static bool IsLoggingEnabled { get; set; }

    protected override async void OnStartup(StartupEventArgs e)
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
            .AddSingleton<ISettingsService, SettingsService>()
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

        Log.Debug("Services configured");

        InitializeLibraries();

        // Parse path to configuration file if specified in command line arguments.
        string filePath = Parser.Default.ParseArguments<Options>(e.Args)
            .MapResult(o => o.ConfigurationFilePath, _ => string.Empty);

        // Start GUI with configuration file loaded and acquisition started (if provided).
        if (!string.IsNullOrEmpty(filePath))
        {
            if (File.Exists(filePath))
            {
                try
                {
                    await Ioc.Default.GetRequiredService<IConfigurationService>().RestoreAsync(filePath, CancellationToken.None);
                    await Ioc.Default.GetRequiredService<AcquisitionViewModel>().PlayCommand.ExecuteAsync(null);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to restore configuration in '{filePath}'{ex.Message}");
                }
            }
            else Log.Warning("Configuration file '{FilePath}' not found.", filePath);
        }

        // Restore user settings to UI.
        Ioc.Default.GetRequiredService<ISettingsService>().Restore();
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
        Ioc.Default.GetRequiredService<ISettingsService>().Store();
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

        // Parse command line arguments to register device classes in GcLibrary.
        var result = Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
            .WithParsed(o =>
            {
                if (o.DeviceClasses == null || !o.DeviceClasses.Any())
                {
                    GcLibrary.Init(true, Ioc.Default.GetService<ILogger<App>>());
                }
                else
                {
                    GcLibrary.Init(false, Ioc.Default.GetService<ILogger<App>>());

                    foreach (var deviceClass in o.DeviceClasses)
                    {
                        try
                        {
                            // Get all valid device classes in GcLibrary, which are derived from GcDevice and implement IDeviceEnumerator and IDeviceClassDescriptor.
                            var validDeviceClasses = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(assembly => assembly.GetTypes())
                                .Where(type => typeof(GcDevice).IsAssignableFrom(type) &&
                                               typeof(IDeviceEnumerator).IsAssignableFrom(type) &&
                                               typeof(IDeviceClassDescriptor).IsAssignableFrom(type))
                                .ToList();

                            // Register device class if it is valid, otherwise log an error message.
                            var deviceType = validDeviceClasses.FirstOrDefault(type => type.Name == deviceClass);
                            if (deviceType != null)
                            {
                                var registerMethod = typeof(GcLibrary).GetMethod(nameof(GcLibrary.Register))?.MakeGenericMethod(deviceType);
                                registerMethod?.Invoke(null, null);
                            }
                            else
                            {
                                Log.Error($"Unable to register device class {deviceClass} (specified as command line argument)\nValid classes are: {StringHelper.JoinWithAnd(validDeviceClasses.Select(type => type.Name))}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex, $"Failed to register device class {deviceClass}");
                        }
                    }
                }
            });
            //.WithNotParsed(e => HandleParseError(e));
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