namespace Persistence.Tests;

using System.Data;
using System.Diagnostics.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Respawn;

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
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS osm_points (node_id BIGINT PRIMARY KEY, tags JSONB,"
                + " geom GEOMETRY(Point, 4326), updated_at TIMESTAMP WITH TIME ZONE);"
        );
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS osm_lines (way_id BIGINT PRIMARY KEY, tags JSONB,"
                + " geom GEOMETRY(LineString, 4326), nodes bigint[], updated_at TIMESTAMP WITH TIME ZONE);"
        );
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS osm_polygons (area_id BIGINT PRIMARY KEY, tags JSONB,"
                + " geom GEOMETRY(Geometry, 4326), nodes bigint[], updated_at TIMESTAMP WITH TIME ZONE);"
        );
    }

    public async Task DisposeAsync() => await PostgresContainer.StopAsync();
}

[CollectionDefinition("DB", DisableParallelization = true)]
public class DatabaseCollectionFixture : ICollectionFixture<DatabaseFixture> { }

public class DbTestClass : IAsyncLifetime
{
    private readonly string connectionString;
    protected RoutingDbContext DbContext { get; private set; } = null!;
    protected IDbConnection Connection { get; private set; } = null!;
    private Respawner respawner = null!;

    protected DbTestClass(DatabaseFixture dbFixture)
    {
        Contract.Requires(dbFixture != null);
        connectionString = dbFixture!.PostgresContainer.GetConnectionString();
    }

    public async Task InitializeAsync()
    {
        DbContext = new(
            new DbContextOptionsBuilder()
                .UseNpgsql(connectionString + ";Include Error Detail=true")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options
        );
        await DbContext.Database.OpenConnectionAsync();
        Connection = DbContext.Database.GetDbConnection();
        await ((NpgsqlConnection)Connection).ReloadTypesAsync();
        respawner = await Respawner.CreateAsync(
            DbContext.Database.GetDbConnection(),
            new() { DbAdapter = DbAdapter.Postgres }
        );
    }

    public async Task DisposeAsync()
    {
        await respawner.ResetAsync(DbContext.Database.GetDbConnection());
        Connection.Dispose();
    }
}
