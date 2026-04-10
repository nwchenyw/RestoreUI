using Microsoft.Extensions.Hosting.WindowsServices;
using RestoreSystem.Service;

Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "RestoreSystemService";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
