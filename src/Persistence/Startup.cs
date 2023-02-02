namespace Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class Startup
{
    public static IServiceCollection ConfigurePersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) => services.AddDbContext<MapDbContext>(
        options => options.UseNpgsql(configuration.GetConnectionString("postgres"))
    );
}
