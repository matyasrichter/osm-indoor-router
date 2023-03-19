using Core;
using GraphBuilding;
using GraphUpdater;
using Persistence;
using Settings;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (context, services) =>
            services
                .ConfigureSettingsServices()
                .AddHttpClient()
                .ConfigureGraphBuildingServices()
                .ConfigurePersistenceServices(context.Configuration)
                .ConfigureCoreServices()
                .AddHostedService<GraphUpdatingBackgroundService>()
    )
    .Build();

await host.RunAsync();
