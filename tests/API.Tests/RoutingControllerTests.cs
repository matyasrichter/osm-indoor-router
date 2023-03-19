namespace API.Tests;

using System.Net;
using System.Net.Http.Json;
using GraphBuilding.Ports;
using Persistence.Repositories;
using Routing.Entities;
using TestUtils;

[Collection("Controller")]
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
        var nodeA = await repo.SaveNode(new(version, new(10, 20), 0, 1), version);
        var nodeB = await repo.SaveNode(new(version, new(20, 25), 0, 2), version);
        var nodeC = await repo.SaveNode(new(version, new(30, 31), 0, 3), version);
        await repo.SaveEdges(
            new InsertedEdge[]
            {
                new(version, nodeA.Id, nodeB.Id, 100, 200, 123456),
                new(version, nodeB.Id, nodeC.Id, 100, 200, 123457)
            },
            version
        );
        await repo.FinalizeVersion(version);

        var response = await Client.GetAsync(
            $"route?from={nodeA.Id}&to={nodeC.Id}&graphVersion={version}"
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var route = await response.Content.ReadFromJsonAsync<Route>();
        route.Should().NotBeNull();
        route!.Nodes
            .Should()
            .BeEquivalentTo(
                new List<RouteNode>()
                {
                    new(nodeA.Id, nodeA.Coordinates.Y, nodeA.Coordinates.X, nodeA.Level),
                    new(nodeB.Id, nodeB.Coordinates.Y, nodeB.Coordinates.X, nodeB.Level),
                    new(nodeC.Id, nodeC.Coordinates.Y, nodeC.Coordinates.X, nodeC.Level)
                }
            );
    }

    public RoutingControllerTests(IntegrationTestFactory factory)
        : base(factory) { }
}
