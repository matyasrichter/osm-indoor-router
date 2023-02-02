namespace Application;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Settings;

public static class Configure
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSingleton(r =>
            {
                var settings = new Settings();
                r.GetRequiredService<IConfiguration>().GetSection("Settings").Bind(settings);
                return settings;
            })
            .ConfigurePersistenceServices(configuration);
}
