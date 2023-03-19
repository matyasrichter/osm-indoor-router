using System.Runtime.InteropServices;
using API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddServices(builder.Configuration);
builder.Services
    .AddControllers()
    .AddJsonOptions(
        options =>
            options.JsonSerializerOptions.Converters.Add(
                new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()
            )
    );
builder.Services.AddCors(
    options =>
        options.AddDefaultPolicy(
            policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
        )
);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    setup.SupportNonNullableReferenceTypes();
    setup.UseAllOfToExtendReferenceSchemas();
    setup.SchemaFilter<NotNullableRequiredSchemaFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger().UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors();

app.Run();

[ComVisible(true)]
public partial class Program { } // so you can reference it from tests
