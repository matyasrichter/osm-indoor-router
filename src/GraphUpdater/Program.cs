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

// Apply migrations
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<RoutingDbContext>();
    Migrate.MigrateUp(context);
}

await host.RunAsync();
