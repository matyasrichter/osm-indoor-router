namespace Persistence;

using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repositories;
using Routing.Ports;

public static class Configure
{
    public static IServiceCollection ConfigurePersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) =>
        services
            .AddDbContext<RoutingDbContext>(
                options => options.UseNpgsql(configuration.GetConnectionString("postgres"))
            )
            .AddTransient<IGraphSavingPort, RoutingGraphRepository>()
            .AddTransient<IRoutingPort, PgRoutingRepository>();
}
