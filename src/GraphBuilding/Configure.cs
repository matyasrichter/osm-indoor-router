namespace GraphBuilding;

using Microsoft.Extensions.DependencyInjection;

public static class Configure
{
    public static IServiceCollection ConfigureGraphBuildingServices(
        this IServiceCollection services
    ) => services.AddTransient<GraphUpdater>().AddTransient<OverpassLoader>();
}
