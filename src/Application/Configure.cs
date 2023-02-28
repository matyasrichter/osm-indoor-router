namespace Application;

using GraphBuilding;
using Microsoft.Extensions.Options;
using Persistence;
using Settings;

public static class Configure
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<Settings>()
            .BindConfiguration("Settings")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddSingleton(x => x.GetRequiredService<IOptions<Settings>>().Value)
            .AddHttpClient()
            .ConfigurePersistenceServices(configuration)
            .ConfigureGraphBuildingServices();
    }
}
