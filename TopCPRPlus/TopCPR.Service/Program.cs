using Microsoft.Extensions.Hosting.WindowsServices;
using TopCPR.Service;

Host.CreateDefaultBuilder(args)
    .UseWindowsService(o => o.ServiceName = "TopCPRService")
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
