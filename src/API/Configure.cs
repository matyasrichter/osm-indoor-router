namespace API;

using Core;
using Persistence;
using Routing;
using Settings;

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
            .AddCors(
                options =>
                    options.AddDefaultPolicy(
                        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
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
