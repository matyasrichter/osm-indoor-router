namespace Persistence.Tests;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;

public sealed class DatabaseFixture : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlTestcontainerConfiguration conf = new()
    {
        Database = "db",
        Username = "postgres",
        Password = "postgres"
    };

    private TestcontainerDatabase postgresqlContainer = null!;
    public MapDbContext DbContext { get; private set; } = null!;


    public async Task InitializeAsync()
    {
        // https://github.com/testcontainers/testcontainers-dotnet/issues/750
        postgresqlContainer = new ContainerBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(conf)
            .Build();
        await postgresqlContainer.StartAsync();
        DbContext = new(new DbContextOptionsBuilder().UseNpgsql(postgresqlContainer.ConnectionString).Options);
    }

    public Task DisposeAsync() => postgresqlContainer.DisposeAsync().AsTask();

    public void Dispose() => conf.Dispose();
}

[CollectionDefinition("DB")]
public class DatabaseCollectionFixture : ICollectionFixture<DatabaseFixture>
{
}
