namespace Core;

using Microsoft.Extensions.DependencyInjection;

public static class Configure
{
    public static IServiceCollection ConfigureCoreServices(this IServiceCollection services) =>
        services.AddTransient<ITimeMachine, TimeMachine>();
}
