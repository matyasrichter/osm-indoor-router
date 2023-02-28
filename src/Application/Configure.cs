namespace Application;

using GraphBuilding;
using Microsoft.Extensions.Options;
using Persistence;
using Settings;

public static class Configure
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services
            .AddOptions<AppSettings>()
            .BindConfiguration("Settings")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        _ = services
            .AddSingleton(x => x.GetRequiredService<IOptions<AppSettings>>().Value)
            .AddHttpClient()
            .ConfigurePersistenceServices(configuration)
            .ConfigureGraphBuildingServices();
    }
}
