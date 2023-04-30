namespace Persistence.Tests;

using Entities.Processed;
using Repositories;
using Routing.Entities;
using Settings;

[Collection("DB")]
[Trait("Category", "DB")]
public class PgRoutingRepositoryTests : DbTestClass
{
    private async Task<(
        RoutingNode NodeA,
        RoutingNode MiddleNode,
        RoutingNode NodeB,
        RoutingEdge EdgeA,
        RoutingEdge EdgeB
    )> CreateTrivialGraph()
    {
        var nodeA = new RoutingNode(10, new(10, 10), 0, 1, false);
        var middleNode = new RoutingNode(10, new(10, 15), 0, 2, false);
        var nodeB = new RoutingNode(10, new(10, 20), 0, 3, false);
        await DbContext.RoutingNodes.AddRangeAsync(nodeA, nodeB, middleNode);
        await DbContext.SaveChangesAsync();
        var edgeA = new RoutingEdge()
        {
            Version = 10,
            FromId = nodeA.Id,
            ToId = middleNode.Id,
            Cost = 100,
            ReverseCost = 50
        };
        var edgeB = new RoutingEdge()
        {
            Version = 10,
            FromId = middleNode.Id,
            ToId = nodeB.Id,
            Cost = 100,
            ReverseCost = 50
        };
        await DbContext.RoutingEdges.AddRangeAsync(edgeA, edgeB);
        await DbContext.SaveChangesAsync();
        return (nodeA, middleNode, nodeB, edgeA, edgeB);
    }

    [Fact]
    public async Task CanRouteOnTrivialGraph()
    {
        var (nodeA, nodeM, nodeB, edgeA, edgeB) = await CreateTrivialGraph();
        var repo = new PgRoutingRepository(DbContext, settings);
        var route = await repo.FindRoute(nodeA.Id, nodeB.Id, 10);
        var expected = new List<RouteSegment>()
        {
            new(
                new(nodeA.Id, nodeA.Coordinates, nodeA.Level, false),
                new(edgeA.Id, edgeA.Cost, edgeA.Distance),
                0
            ),
            new(
                new(nodeM.Id, nodeM.Coordinates, nodeM.Level, false),
                new(edgeB.Id, edgeB.Cost, edgeB.Distance),
                100
            ),
            new(new(nodeB.Id, nodeB.Coordinates, nodeB.Level, false), null, 200)
        };
        route.Should().BeEquivalentTo(expected, o => o.WithStrictOrderingFor(x => x.Node.Id));
    }

    [Fact]
    public async Task CanRouteOnTrivialGraphReverse()
    {
        var (nodeA, nodeM, nodeB, edgeA, edgeB) = await CreateTrivialGraph();
        var repo = new PgRoutingRepository(DbContext, settings);
        var route = await repo.FindRoute(nodeB.Id, nodeA.Id, 10);
        var expected = new List<RouteSegment>()
        {
            new(
                new(nodeB.Id, nodeB.Coordinates, nodeB.Level, false),
                new(edgeB.Id, edgeB.ReverseCost, edgeB.Distance),
                0
            ),
            new(
                new(nodeM.Id, nodeM.Coordinates, nodeM.Level, false),
                new(edgeA.Id, edgeA.ReverseCost, edgeA.Distance),
                50
            ),
            new(new(nodeA.Id, nodeA.Coordinates, nodeA.Level, false), null, 100)
        };
        route.Should().BeEquivalentTo(expected, o => o.WithStrictOrderingFor(x => x.Node.Id));
    }

    [Fact]
    public async Task HandlesRouteNotFound()
    {
        var nodeA = new RoutingNode(10, new(10, 20), 0, 1, false);
        var nodeB = new RoutingNode(10, new(10, 20), 0, 1, false);
        await DbContext.RoutingNodes.AddRangeAsync(nodeA, nodeB);
        await DbContext.SaveChangesAsync();
        var repo = new PgRoutingRepository(DbContext, settings);
        var route = await repo.FindRoute(nodeA.Id, nodeB.Id, 10);
        route.Should().BeEmpty();
    }

    public PgRoutingRepositoryTests(DatabaseFixture dbFixture)
        : base(dbFixture) { }

    private readonly AppSettings settings =
        new()
        {
            Bbox = new()
            {
                SouthWest = new() { Latitude = 50.100700, Longitude = 14.386007 },
                NorthEast = new() { Latitude = 50.105917, Longitude = 14.395190 }
            },
            CorsAllowedOrigins = Array.Empty<string>()
        };
}
