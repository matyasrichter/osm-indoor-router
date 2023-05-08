namespace Persistence.Tests;

using Entities.Processed;
using GraphBuilding;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Repositories;
using Routing.Entities;
using TestUtils;

[Collection("DB")]
[Trait("Category", "DB")]
public sealed class RoutingGraphRepositoryTests : DbTestClass
{
    private static readonly GeometryFactory GF = new(new(), 4326);

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
            new(100, nodeId, new(Array.Empty<Coordinate>()), 2, 1, null, 1),
            new(nodeId, 100, new(Array.Empty<Coordinate>()), 1, 3, null, 1)
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
            new(
                FromId: nodeIds[0],
                ToId: nodeIds[1],
                Geometry: new(Array.Empty<Coordinate>()),
                Cost: 1,
                ReverseCost: 3,
                Source: null,
                Distance: 1,
                IsElevator: false,
                IsStairs: true,
                IsEscalator: false
            ),
            new(
                FromId: nodeIds[0],
                ToId: nodeIds[2],
                Geometry: new(Array.Empty<Coordinate>()),
                Cost: 3,
                ReverseCost: 1,
                Source: new(SourceType.Line, 654),
                Distance: 2,
                IsElevator: true,
                IsStairs: false,
                IsEscalator: true
            )
        };

        await repo.SaveEdges(toInsert, version);

        (await DbContext.RoutingEdges.ToListAsync())
            .Should()
            .BeEquivalentTo(
                new RoutingEdge[]
                {
                    new()
                    {
                        Version = version,
                        FromId = nodeIds[0],
                        ToId = nodeIds[1],
                        Cost = 1,
                        ReverseCost = 3,
                        SourceId = null,
                        Distance = 1,
                        IsElevator = false,
                        IsStairs = true,
                        IsEscalator = false
                    },
                    new()
                    {
                        Version = version,
                        FromId = nodeIds[0],
                        ToId = nodeIds[2],
                        Cost = 3,
                        ReverseCost = 1,
                        SourceId = 654,
                        SourceType = SourceType.Line,
                        Distance = 2,
                        IsElevator = true,
                        IsStairs = false,
                        IsEscalator = true
                    }
                },
                o => o.Excluding(x => x.Id).Excluding(x => x.From).Excluding(x => x.To)
            );
    }

    [Fact]
    public async Task CanRemoveDesertedNodes()
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
                    new(new(5, 8), 0m, null),
                    new(new(10, 20), 0m, null),
                },
                version
            )
        ).ToList();
        var toInsert = new HashSet<InMemoryEdge>()
        {
            new(nodeIds[0], nodeIds[1], new(Array.Empty<Coordinate>()), 1, 3, null, 1),
            new(
                nodeIds[0],
                nodeIds[2],
                new(Array.Empty<Coordinate>()),
                3,
                1,
                new(SourceType.Line, 654),
                2
            )
        };

        await repo.SaveEdges(toInsert, version);

        (await DbContext.RoutingNodes.CountAsync()).Should().Be(4);
        (await DbContext.RoutingEdges.CountAsync()).Should().Be(2);

        await repo.RemoveNodesWithoutEdges();

        (await DbContext.RoutingNodes.ToListAsync())
            .Select(x => x.Id)
            .Should()
            .BeEquivalentTo(nodeIds.Take(3));
        (await DbContext.RoutingEdges.CountAsync()).Should().Be(2);
    }

    public static TheoryData<
        string,
        RoutingNode[],
        double,
        double,
        decimal,
        long,
        Node
    > FindsClosestNodeData() =>
        new()
        {
            {
                "it is the closest and all nodes are on level 3",
                new RoutingNode[]
                {
                    new(
                        1,
                        1,
                        GF.CreatePoint(new Coordinate(10, 10)),
                        3,
                        new(SourceType.Point, 123),
                        false
                    ),
                    new(
                        2,
                        1,
                        GF.CreatePoint(new Coordinate(20, 10)),
                        3,
                        new(SourceType.Point, 123),
                        false
                    ),
                    new(
                        3,
                        1,
                        GF.CreatePoint(new Coordinate(16, 16)),
                        3,
                        new(SourceType.Point, 123),
                        false
                    )
                },
                15,
                15,
                3,
                1,
                new(3, GF.CreatePoint(new Coordinate(16, 16)), 3, false)
            },
            {
                "it is the closest on that level (not globally)",
                new RoutingNode[]
                {
                    new(
                        1,
                        1,
                        GF.CreatePoint(new Coordinate(10, 10)),
                        2,
                        new(SourceType.Point, 123),
                        false
                    ),
                    new(
                        2,
                        1,
                        GF.CreatePoint(new Coordinate(16, 10)),
                        2,
                        new(SourceType.Point, 123),
                        false
                    ),
                    new(
                        3,
                        1,
                        GF.CreatePoint(new Coordinate(16, 16)),
                        1,
                        new(SourceType.Point, 123),
                        false
                    )
                },
                15,
                15,
                1,
                1,
                new(3, GF.CreatePoint(new Coordinate(16, 16)), 1, false)
            },
            {
                "it is the closest in that graph version (not globally)",
                new RoutingNode[]
                {
                    new(
                        1,
                        2,
                        GF.CreatePoint(new Coordinate(10, 10)),
                        2,
                        new(SourceType.Point, 123),
                        false
                    ),
                    new(
                        2,
                        2,
                        GF.CreatePoint(new Coordinate(16, 10)),
                        2,
                        new(SourceType.Point, 123),
                        false
                    ),
                    new(
                        3,
                        1,
                        GF.CreatePoint(new Coordinate(16, 16)),
                        2,
                        new(SourceType.Point, 123),
                        false
                    )
                },
                15,
                15,
                2,
                2,
                new(2, GF.CreatePoint(new Coordinate(16, 10)), 2, false)
            }
        };

    [Theory]
    [MemberData(nameof(FindsClosestNodeData))]
    public async Task FindsClosestNode(
        string because,
        RoutingNode[] nodes,
        double lat,
        double lon,
        decimal level,
        long version,
        Node expected
    )
    {
        await DbContext.RoutingNodes.AddRangeAsync(nodes);
        await DbContext.SaveChangesAsync();
        var result = await new RoutingGraphRepository(
            DbContext,
            new TestingTimeMachine()
        ).FindClosestNode(lat, lon, level, version);
        result.Should().Be(expected, because);
    }

    public RoutingGraphRepositoryTests(DatabaseFixture dbFixture)
        : base(dbFixture) { }
}
