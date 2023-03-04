namespace Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class Configure
{
    public static IServiceCollection ConfigureSettingsServices(this IServiceCollection services)
    {
        _ = services
            .AddOptions<AppSettings>()
            .BindConfiguration("Settings")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services.AddSingleton(x => x.GetRequiredService<IOptions<AppSettings>>().Value);
    }
}
