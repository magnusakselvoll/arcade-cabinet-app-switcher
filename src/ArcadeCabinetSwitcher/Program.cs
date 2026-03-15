using ArcadeCabinetSwitcher;
using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using ArcadeCabinetSwitcher.ProcessManagement;
using Serilog;
using Serilog.Events;
using Serilog.Settings.Configuration;

var programData = Environment.GetEnvironmentVariable("ProgramData")
    ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var logPath = Path.Combine(programData, "ArcadeCabinetSwitcher", "logs", "arcade-cabinet-switcher.log");

var builder = Host.CreateApplicationBuilder(args);
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

try
{
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
