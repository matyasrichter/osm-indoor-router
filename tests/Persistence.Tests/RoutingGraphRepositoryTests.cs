namespace Persistence.Tests;

using GraphBuilding;
using GraphBuilding.Ports;
using Repositories;
using TestUtils;

[Collection("DB")]
[Trait("Category", "DB")]
public sealed class RoutingGraphRepositoryTests : DbTestClass
{
    [Fact]
    public async Task CanSaveAndRetrievePoints()
    {
        var repo = new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0, DateTimeKind.Utc) }
        );

        var version = await repo.AddVersion();
        var toInsert = new HashSet<InsertedNode>()
        {
            new(version, new(1, 2), 0m, null),
            new(version, new(2, 3), 0m, null),
            new(version, new(3, 4), 0m, null)
        };
        var points = new List<Node>();

        (await repo.GetNodes()).Should().BeEmpty();

        foreach (var insertedNode in toInsert)
        {
            points.Add(await repo.SaveNode(insertedNode));
        }

        (await repo.GetNodes()).Should().HaveCount(3).And.BeEquivalentTo(points);
    }

    [Fact]
    public async Task FailsForEdgesWithoutPoints()
    {
        var repo = new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0, DateTimeKind.Utc) }
        );

        var version = await repo.AddVersion();
        var node = await repo.SaveNode(new(version, new(1, 2), 0m, null));
        var toInsert = new HashSet<InsertedEdge>()
        {
            new(version, 100, node.Id, 2, 1, null),
            new(version, node.Id, 100, 1, 3, null)
        };

        var saving = async () => await repo.SaveEdges(toInsert);

        await saving.Should().ThrowAsync<Exception>();
        (await repo.GetEdges()).Should().BeEmpty();
    }

    [Fact]
    public async Task CanSaveEdges()
    {
        var repo = new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0, DateTimeKind.Utc) }
        );

        var version = await repo.AddVersion();
        var nodeA = await repo.SaveNode(new(version, new(1, 2), 0m, null));
        var nodeB = await repo.SaveNode(new(version, new(3, 4), 0m, null));
        var nodeC = await repo.SaveNode(new(version, new(5, 8), 0m, null));
        var toInsert = new HashSet<InsertedEdge>()
        {
            new(version, nodeA.Id, nodeB.Id, 1, 3, null),
            new(version, nodeA.Id, nodeC.Id, 1, 3, null)
        };

        await repo.SaveEdges(toInsert);

        var edges = await repo.GetEdges();
        edges.Should().HaveCount(2);
    }

    public RoutingGraphRepositoryTests(DatabaseFixture dbFixture)
        : base(dbFixture) { }
}
