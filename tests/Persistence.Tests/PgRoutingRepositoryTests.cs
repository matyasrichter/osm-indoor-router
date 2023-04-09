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
        RoutingNode NodeB,
        RoutingEdge Edge
    )> CreateTrivialGraph()
    {
        var nodeA = new RoutingNode(10, new(10, 20), 0, 1);
        var nodeB = new RoutingNode(10, new(10, 20), 0, 1);
        await DbContext.RoutingNodes.AddRangeAsync(nodeA, nodeB);
        await DbContext.SaveChangesAsync();
        var edge = new RoutingEdge()
        {
            Version = 10,
            FromId = nodeA.Id,
            ToId = nodeB.Id,
            Cost = 100,
            ReverseCost = 50
        };
        await DbContext.RoutingEdges.AddAsync(edge);
        await DbContext.SaveChangesAsync();
        return (nodeA, nodeB, edge);
    }

    [Fact]
    public async Task CanRouteOnTrivialGraph()
    {
        var (nodeA, nodeB, edge) = await CreateTrivialGraph();
        var repo = new PgRoutingRepository(DbContext, settings);
        var route = await repo.FindRoute(nodeA.Id, nodeB.Id, 10);
        var expected = new List<RouteSegment>()
        {
            new(new(nodeA.Id, nodeA.Coordinates, nodeA.Level), new(edge.Id, edge.Cost), 0),
            new(new(nodeB.Id, nodeB.Coordinates, nodeB.Level), null, 100)
        };
        route.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CanRouteOnTrivialGraphReverse()
    {
        var (nodeA, nodeB, edge) = await CreateTrivialGraph();
        var repo = new PgRoutingRepository(DbContext, settings);
        var route = await repo.FindRoute(nodeB.Id, nodeA.Id, 10);
        var expected = new List<RouteSegment>()
        {
            new(new(nodeB.Id, nodeB.Coordinates, nodeB.Level), new(edge.Id, edge.ReverseCost), 0),
            new(new(nodeA.Id, nodeA.Coordinates, nodeA.Level), null, 50)
        };
        route.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task HandlesRouteNotFound()
    {
        var nodeA = new RoutingNode(10, new(10, 20), 0, 1);
        var nodeB = new RoutingNode(10, new(10, 20), 0, 1);
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
