namespace Persistence.Tests;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DatabaseFixture : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlTestcontainerConfiguration conf = new()
    {
        Database = "db",
        Username = "postgres",
        Password = "postgres"
    };

    public TestcontainerDatabase PostgresContainer { get; }

    public DatabaseFixture() =>
        // https://github.com/testcontainers/testcontainers-dotnet/issues/750
#pragma warning disable CS0618
        PostgresContainer = new ContainerBuilder<PostgreSqlTestcontainer>()
#pragma warning restore CS0618
            .WithDatabase(conf)
            .WithImage(Environment.GetEnvironmentVariable("TEST_DB_IMAGE_NAME") ?? "postgis/postgis:15-3.3-alpine")
            .Build();

    public async Task InitializeAsync()
    {
        await PostgresContainer.StartAsync();
        var dbContext = new MapDbContext(
            new DbContextOptionsBuilder()
                .UseNpgsql(PostgresContainer.ConnectionString + ";Include Error Detail=true")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options
        );
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await PostgresContainer.StopAsync();

    public void Dispose() => conf.Dispose();
}

[CollectionDefinition("DB", DisableParallelization = true)]
public class DatabaseCollectionFixture : ICollectionFixture<DatabaseFixture>
{
}

public class DbTestClass : IAsyncLifetime, IDisposable
{
    private readonly string connectionString;
    protected MapDbContext DbContext { get; private set; } = null!;

    protected DbTestClass(DatabaseFixture dbFixture) => connectionString = dbFixture.PostgresContainer.ConnectionString;

    public Task InitializeAsync() =>
        Task.FromResult(DbContext = new(
            new DbContextOptionsBuilder()
                .UseNpgsql(connectionString + ";Include Error Detail=true")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options
        ));

    async Task IAsyncLifetime.DisposeAsync()
    {
        // delete everything in the database before the next test
        await DbContext.MapNodes.ExecuteDeleteAsync();
        await DbContext.MapEdges.ExecuteDeleteAsync();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
