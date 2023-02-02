namespace Persistence.Tests;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;

public class DatabaseTest : IAsyncLifetime
{
    protected MapDbContext DbContext { get; private set; } = null!;

    private readonly TestcontainerDatabase postgresqlContainer = new ContainerBuilder<PostgreSqlTestcontainer>()
        .WithDatabase(new PostgreSqlTestcontainerConfiguration
        {
            Database = "db",
            Username = "postgres",
            Password = "postgres",
        })
        .Build();

    public async Task InitializeAsync()
    {
        await postgresqlContainer.StartAsync();
        DbContext = new(new DbContextOptionsBuilder().UseNpgsql(postgresqlContainer.ConnectionString).Options);
    }

    public Task DisposeAsync() => postgresqlContainer.DisposeAsync().AsTask();
}
