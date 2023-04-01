using System.Runtime.InteropServices;
using API;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    _ = app.UseSwagger().UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<RoutingDbContext>();
    Migrate.MigrateUp(context);
}

app.Run();

[ComVisible(true)]
public partial class Program { } // so you can reference it from tests
