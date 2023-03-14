namespace Persistence.Tests;

using System.Diagnostics.Contracts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer PostgresContainer { get; }

    public DatabaseFixture() =>
        PostgresContainer = new PostgreSqlBuilder()
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithImage(
                Environment.GetEnvironmentVariable("TEST_DB_IMAGE_NAME")
                    ?? "gitlab.fit.cvut.cz:5000/richtm12/bp-code/postgis"
            )
            .Build();

    public async Task InitializeAsync()
    {
        await PostgresContainer.StartAsync();
        await using var dbContext = new RoutingDbContext(
            new DbContextOptionsBuilder()
                .UseNpgsql(PostgresContainer.GetConnectionString() + ";Include Error Detail=true")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options
        );
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await PostgresContainer.StopAsync();
}

[CollectionDefinition("DB", DisableParallelization = true)]
public class DatabaseCollectionFixture : ICollectionFixture<DatabaseFixture> { }

public class DbTestClass : IAsyncLifetime
{
    private readonly string connectionString;
    protected RoutingDbContext DbContext { get; private set; } = null!;

    protected DbTestClass(DatabaseFixture dbFixture)
    {
        Contract.Requires(dbFixture != null);
        connectionString = dbFixture!.PostgresContainer.GetConnectionString();
    }

    public Task InitializeAsync() =>
        Task.FromResult(
            DbContext = new(
                new DbContextOptionsBuilder()
                    .UseNpgsql(connectionString + ";Include Error Detail=true")
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    .Options
            )
        );

    public async Task DisposeAsync()
    {
        // delete everything in the database before the next test
        _ = await DbContext.RoutingNodes.ExecuteDeleteAsync();
        _ = await DbContext.RoutingEdges.ExecuteDeleteAsync();
        _ = await DbContext.RoutingEdges.ExecuteDeleteAsync();
    }
}
