namespace API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using GraphBuilding;
using Persistence.Repositories;
using Routing.Entities;
using TestUtils;

[Collection("Controller")]
[Trait("Category", "DB")]
public class RoutingControllerTests : ControllerTestBase
{
    [Fact]
    public async Task Returns404WhenNoRouteIsFound()
    {
        var response = await Client.GetAsync("route?from=1&to=2&graphVersion=1");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsRoute()
    {
        var timeMachine = new TestingTimeMachine
        {
            Now = new(2023, 03, 01, 1, 1, 1, DateTimeKind.Utc)
        };
        var repo = new RoutingGraphRepository(DbContext, timeMachine);

        // create a three node graph A <--> B <--> C
        var version = await repo.AddVersion();
        var nodes = new InMemoryNode[]
        {
            new(new(10, 20), 0, 1),
            new(new(20, 25), 0, 2),
            new(new(30, 31), 0, 3)
        };
        var nodeIds = (await repo.SaveNodes(nodes, version)).ToList();
        await repo.SaveEdges(
            new InMemoryEdge[]
            {
                new(nodeIds[0], nodeIds[1], 100, 200, 123456, 100),
                new(nodeIds[1], nodeIds[2], 100, 200, 123457, 100)
            },
            version
        );
        await repo.FinalizeVersion(version);

        var response = await Client.GetAsync(
            $"route?from={nodeIds[0]}&to={nodeIds[2]}&graphVersion={version}"
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var route = await response.Content.ReadFromJsonAsync<Route>();
        route.Should().NotBeNull();
        route!.Nodes
            .Should()
            .BeEquivalentTo(
                new List<RouteNode>()
                {
                    new(
                        nodeIds[0],
                        nodes[0].Coordinates.Y,
                        nodes[0].Coordinates.X,
                        nodes[0].Level,
                        nodes[0].IsLevelConnection
                    ),
                    new(
                        nodeIds[1],
                        nodes[1].Coordinates.Y,
                        nodes[1].Coordinates.X,
                        nodes[1].Level,
                        nodes[1].IsLevelConnection
                    ),
                    new(
                        nodeIds[2],
                        nodes[2].Coordinates.Y,
                        nodes[2].Coordinates.X,
                        nodes[2].Level,
                        nodes[2].IsLevelConnection
                    )
                },
                o => o.WithStrictOrdering()
            );
        route.TotalDistanceInMeters.Should().Be(200);
    }

    public RoutingControllerTests(IntegrationTestFactory factory)
        : base(factory) { }
}
