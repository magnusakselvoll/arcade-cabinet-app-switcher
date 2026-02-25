using ArcadeCabinetSwitcher;
using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.ProcessManagement;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ArcadeCabinetSwitcher";
});
builder.Services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
builder.Services.AddSingleton<IProcessLauncher, SystemProcessLauncher>();
builder.Services.AddSingleton<IProcessManager, ProcessManager>();
builder.Services.AddHostedService<Worker>();

builder.Services.AddSerilog((_, lc) =>
    lc.ReadFrom.Configuration(builder.Configuration));

var host = builder.Build();
host.Run();
