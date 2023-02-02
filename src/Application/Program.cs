using Application;
using Microsoft.Extensions.Hosting;

var builder = Host
    .CreateDefaultBuilder()
    .ConfigureServices((context, services) => services.AddServices(context.Configuration));
var host = builder.Build();
await host.StartAsync();
