namespace Persistence;

using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repositories;

public static class Configure
{
    public static IServiceCollection ConfigurePersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) =>
        services
            .AddDbContext<MapDbContext>(
                options => options.UseNpgsql(configuration.GetConnectionString("postgres"))
            )
            .AddTransient<IGraphSavingPort, MapRepository>();
}
