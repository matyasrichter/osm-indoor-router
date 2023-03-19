namespace Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class MapDbContextFactory : IDesignTimeDbContextFactory<RoutingDbContext>
{
    public RoutingDbContext CreateDbContext(string[] args)
    {
        var env =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? throw new InvalidOperationException("DOTNET_ENVIRONMENT is not set");
        var conf = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{env}.json", false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<RoutingDbContext>().UseNpgsql(
            conf.GetConnectionString("postgres")
        );
        return new(optionsBuilder.Options);
    }
}
