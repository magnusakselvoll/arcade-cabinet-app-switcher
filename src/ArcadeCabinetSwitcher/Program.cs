using ArcadeCabinetSwitcher;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ArcadeCabinetSwitcher";
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
