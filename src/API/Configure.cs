namespace API;

using Core;
using Microsoft.AspNetCore.HttpOverrides;
using Persistence;
using Routing;
using Settings;
using Microsoft.Extensions.Configuration;

public static class Configure
{
    public static void AddServices(
        this IServiceCollection services,
        IConfiguration configuration
    ) =>
        _ = services
            .ConfigureSettingsServices()
            .ConfigurePersistenceServices(configuration)
            .ConfigureCoreServices()
            .ConfigureRoutingServices()
            .Configure<ForwardedHeadersOptions>(
                options =>
                    options.ForwardedHeaders =
                        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            )
            .AddCors(
                (options) =>
                    options.AddDefaultPolicy(
                        policy =>
                            policy
                                .WithOrigins(
                                    configuration
                                        .GetSection("Settings:CorsAllowedOrigins")
                                        .Get<string[]>()
                                        ?? throw new InvalidOperationException(
                                            "Missing CorsAllowedOrigins"
                                        )
                                )
                                .WithMethods("GET")
                    )
            )
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(setup =>
            {
                setup.SupportNonNullableReferenceTypes();
                setup.UseAllOfToExtendReferenceSchemas();
                setup.SchemaFilter<NotNullableRequiredSchemaFilter>();
            })
            .AddControllers()
            .AddJsonOptions(
                options =>
                    options.JsonSerializerOptions.Converters.Add(
                        new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()
                    )
            );
}
