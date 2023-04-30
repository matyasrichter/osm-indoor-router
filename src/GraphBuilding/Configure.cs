namespace GraphBuilding;

using Microsoft.Extensions.DependencyInjection;
using Parsers;

public static class Configure
{
    public static IServiceCollection ConfigureGraphBuildingServices(
        this IServiceCollection services
    ) =>
        services
            .AddTransient<MapProcessor>()
            .AddTransient<OverpassLoader>()
            .AddTransient<IGraphBuilder, GraphBuilder>()
            .AddTransient<LevelParser>();
}
