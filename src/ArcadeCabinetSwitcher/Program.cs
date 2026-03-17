using ArcadeCabinetSwitcher;
using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using ArcadeCabinetSwitcher.ProcessManagement;
using ArcadeCabinetSwitcher.UI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Serilog;
using Serilog.Events;
using Serilog.Settings.Configuration;

var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var logPath = Path.Combine(localAppData, "ArcadeCabinetSwitcher", "logs", "arcade-cabinet-switcher.log");

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IOverlayService, AvaloniaOverlayService>();
builder.Services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
builder.Services.AddSingleton<IProcessLauncher, SystemProcessLauncher>();
builder.Services.AddSingleton<IProcessManager, ProcessManager>();
builder.Services.AddSingleton<ISystemActionHandler, SystemActionHandler>();
builder.Services.AddSingleton<IJoystickReader, SdlJoystickReader>();
builder.Services.AddSingleton<IInputHandler, InputHandler>();
builder.Services.AddHostedService<Worker>();

builder.Services.AddSerilog((_, lc) =>
    lc.ReadFrom.Configuration(builder.Configuration, new ConfigurationReaderOptions(
        typeof(Serilog.ConsoleLoggerConfigurationExtensions).Assembly,
        typeof(Serilog.FileLoggerConfigurationExtensions).Assembly,
        typeof(Serilog.LoggerConfigurationEventLogExtensions).Assembly))
      .WriteTo.File(
          logPath,
          rollingInterval: RollingInterval.Day,
          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
          restrictedToMinimumLevel: LogEventLevel.Verbose));

var host = builder.Build();

var overlayService = (AvaloniaOverlayService)host.Services.GetRequiredService<IOverlayService>();
var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

// Exit via tray → stop host
overlayService.ExitRequested += (_, _) =>
{
    Log.Information(LogEvents.ExitRequestedFromTray.Name!, "Exit requested from tray icon");
    appLifetime.StopApplication();
};

// Start host (starts Worker as background service)
await host.StartAsync();

// Host stopping → shut down Avalonia
appLifetime.ApplicationStopping.Register(() =>
    Dispatcher.UIThread.Post(() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()));

// Pass overlay service to App before running Avalonia
App.OverlayService = overlayService;

try
{
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await host.StopAsync();
    Log.CloseAndFlush();
}
