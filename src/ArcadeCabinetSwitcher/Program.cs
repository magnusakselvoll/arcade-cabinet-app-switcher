using ArcadeCabinetSwitcher;
using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using ArcadeCabinetSwitcher.ProcessManagement;
using Serilog;
using Serilog.Settings.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ArcadeCabinetSwitcher";
});
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
        typeof(Serilog.LoggerConfigurationEventLogExtensions).Assembly)));

var host = builder.Build();
host.Run();
