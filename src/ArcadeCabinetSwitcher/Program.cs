using ArcadeCabinetSwitcher;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ArcadeCabinetSwitcher";
});
builder.Services.AddHostedService<Worker>();

builder.Services.AddSerilog((_, lc) =>
    lc.ReadFrom.Configuration(builder.Configuration));

var host = builder.Build();
host.Run();
