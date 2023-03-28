namespace Persistence.Tests;

using GraphBuilding;
using Microsoft.EntityFrameworkCore;
using Repositories;
using TestUtils;

[Collection("DB")]
[Trait("Category", "DB")]
public sealed class RoutingGraphRepositoryTests : DbTestClass
{
    [Fact]
    public async Task CanSavePoints()
    {
        var repo = new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0, DateTimeKind.Utc) }
        );

        var version = await repo.AddVersion();
        var toInsert = new HashSet<InMemoryNode>()
        {
            new(new(1, 2), 0m, null),
            new(new(2, 3), 0m, null),
            new(new(3, 4), 0m, null)
        };

        (await DbContext.RoutingNodes.CountAsync()).Should().Be(0);

        var ids = (await repo.SaveNodes(toInsert, version)).ToList();
        ids.Should().HaveCount(toInsert.Count);

        var inDb = await DbContext.RoutingNodes.ToListAsync();
        inDb.Should().HaveCount(toInsert.Count);
        inDb.Select(x => x.Id).Should().BeEquivalentTo(ids);
    }

    [Fact]
    public async Task FailsForEdgesWithoutPoints()
    {
        var repo = new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0, DateTimeKind.Utc) }
        );

        var version = await repo.AddVersion();
        var nodeId = (
            await repo.SaveNodes(new InMemoryNode[] { new(new(1, 2), 0m, null) }, version)
        ).First();
        var toInsert = new HashSet<InMemoryEdge>()
        {
            new(100, nodeId, 2, 1, null),
            new(nodeId, 100, 1, 3, null)
        };

        var saving = async () => await repo.SaveEdges(toInsert, version);

        await saving.Should().ThrowAsync<Exception>();
        (await DbContext.RoutingEdges.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CanSaveEdges()
    {
        var repo = new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0, DateTimeKind.Utc) }
        );

        var version = await repo.AddVersion();
        var nodeIds = (
            await repo.SaveNodes(
                new InMemoryNode[]
                {
                    new(new(1, 2), 0m, null),
                    new(new(3, 4), 0m, null),
                    new(new(5, 8), 0m, null)
                },
                version
            )
        ).ToList();
        var toInsert = new HashSet<InMemoryEdge>()
        {
            new(nodeIds[0], nodeIds[1], 1, 3, null),
            new(nodeIds[0], nodeIds[2], 1, 3, null)
        };

        await repo.SaveEdges(toInsert, version);

        (await DbContext.RoutingEdges.CountAsync()).Should().Be(2);
    }

    public RoutingGraphRepositoryTests(DatabaseFixture dbFixture)
        : base(dbFixture) { }
}
