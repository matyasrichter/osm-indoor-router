namespace API.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence;
using Respawn;
using Testcontainers.PostgreSql;

[CollectionDefinition("Controller")]
public class ControllerCollectionDefinition : ICollectionFixture<IntegrationTestFactory> { }

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public PostgreSqlContainer PostgresContainer { get; }

    public RoutingDbContext DbContext { get; set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public IntegrationTestFactory() =>
        PostgresContainer = new PostgreSqlBuilder()
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithImage(
                Environment.GetEnvironmentVariable("TEST_DB_IMAGE_NAME")
                    ?? "gitlab.fit.cvut.cz:5000/richtm12/bp-code/postgis"
            )
            .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(
            services => services.Replace(new(typeof(RoutingDbContext), DbContext))
        );

    public async Task InitializeAsync()
    {
        await PostgresContainer.StartAsync();
        DbContext = new(
            new DbContextOptionsBuilder()
                .UseNpgsql(PostgresContainer.GetConnectionString() + ";Include Error Detail=true")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options
        );
        await DbContext.Database.MigrateAsync();
        Client = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        Client.Dispose();
    }
}

public abstract class ControllerTestBase : IAsyncLifetime
{
    protected HttpClient Client { get; }
    protected RoutingDbContext DbContext { get; }

    private Respawner respawner = null!;

    protected ControllerTestBase(IntegrationTestFactory factory)
    {
        Client = factory.Client;
        DbContext = factory.DbContext;
    }

    public async Task InitializeAsync()
    {
        await DbContext.Database.OpenConnectionAsync();
        respawner = await Respawner.CreateAsync(
            DbContext.Database.GetDbConnection(),
            new() { DbAdapter = DbAdapter.Postgres }
        );
    }

    public async Task DisposeAsync() =>
        await respawner.ResetAsync(DbContext.Database.GetDbConnection());
}
