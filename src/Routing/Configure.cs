namespace Routing;

using Microsoft.Extensions.DependencyInjection;

public static class Configure
{
    public static IServiceCollection ConfigureRoutingServices(this IServiceCollection services) =>
        services.AddTransient<RoutingService>();
}
