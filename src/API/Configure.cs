namespace API;

using Core;
using Persistence;
using Routing;
using Settings;

public static class Configure
{
    public static void AddServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) =>
        _ = services
            .ConfigureSettingsServices()
            .ConfigurePersistenceServices(configuration)
            .ConfigureCoreServices()
            .ConfigureRoutingServices();
}
