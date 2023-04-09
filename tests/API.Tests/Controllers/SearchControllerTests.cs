namespace API.Tests.Controllers;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using GraphBuilding;
using Microsoft.AspNetCore.Http.Extensions;
using NetTopologySuite.Geometries;
using Responses;
using Persistence.Repositories;
using Routing.Entities;
using TestUtils;

[Collection("Controller")]
[Trait("Category", "DB")]
public class SearchControllerTests : ControllerTestBase
{
    [Fact]
    public async Task FindsNode()
    {
        var timeMachine = new TestingTimeMachine
        {
            Now = new(2023, 03, 01, 1, 1, 1, DateTimeKind.Utc)
        };
        var repo = new RoutingGraphRepository(DbContext, timeMachine);
        var gf = new GeometryFactory(new(), 4326);

        var version = await repo.AddVersion();
        var ids = (
            await repo.SaveNodes(
                new[]
                {
                    new InMemoryNode(gf.CreatePoint(new Coordinate(0, 0)), 0, 123),
                    new InMemoryNode(gf.CreatePoint(new Coordinate(10, 10)), 0, 1234)
                },
                version
            )
        ).ToList();
        await repo.FinalizeVersion(version);

        var queryBuilder = new QueryBuilder
        {
            { "latitude", "2" },
            { "longitude", "2" },
            { "level", "0" },
            { "graphVersion", version.ToString(CultureInfo.InvariantCulture) }
        };
        var response = await Client.GetAsync($"search/closest-node?{queryBuilder.ToQueryString()}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<RouteNode>())
            .Should()
            .Be(new RouteNode(ids[0], 0, 0, 0, false));
    }

    [Fact]
    public async Task Returns404WhenNoNodesFound()
    {
        var timeMachine = new TestingTimeMachine
        {
            Now = new(2023, 03, 01, 1, 1, 1, DateTimeKind.Utc)
        };
        var repo = new RoutingGraphRepository(DbContext, timeMachine);
        var gf = new GeometryFactory(new(), 4326);

        var version = await repo.AddVersion();
        await repo.SaveNodes(
            new[]
            {
                new InMemoryNode(gf.CreatePoint(new Coordinate(0, 0)), 2, 123),
                new InMemoryNode(gf.CreatePoint(new Coordinate(10, 10)), 3, 1234)
            },
            version
        );
        await repo.FinalizeVersion(version);

        var queryBuilder = new QueryBuilder
        {
            { "latitude", "2" },
            { "longitude", "2" },
            { "level", "1" },
            { "graphVersion", version.ToString(CultureInfo.InvariantCulture) }
        };
        var response = await Client.GetAsync($"search/closest-node?{queryBuilder.ToQueryString()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public SearchControllerTests(IntegrationTestFactory factory)
        : base(factory) { }
}
