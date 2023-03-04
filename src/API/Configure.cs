namespace API;

using Persistence;
using Settings;

public static class Configure
{
    public static void AddServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) => _ = services.ConfigureSettingsServices().ConfigurePersistenceServices(configuration);
}
