namespace API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using Responses;
using Persistence.Repositories;
using TestUtils;

[Collection("Controller")]
[Trait("Category", "DB")]
public class ConfigControllerTests : ControllerTestBase
{
    [Fact]
    public async Task ReturnsConfig()
    {
        var timeMachine = new TestingTimeMachine
        {
            Now = new(2023, 03, 01, 1, 1, 1, DateTimeKind.Utc)
        };
        var repo = new RoutingGraphRepository(DbContext, timeMachine);

        var version = await repo.AddVersion();
        await repo.FinalizeVersion(version);

        var response = await Client.GetAsync("config");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<RoutingConfig>())
            .Should()
            .BeEquivalentTo(
                new RoutingConfig(
                    version,
                    new(
                        new(Settings.Bbox.SouthWest.Longitude, Settings.Bbox.SouthWest.Latitude),
                        new(Settings.Bbox.NorthEast.Longitude, Settings.Bbox.NorthEast.Latitude)
                    ),
                    false,
                    false,
                    false
                )
            );
    }

    [Fact]
    public async Task Returns404WithoutActiveVersion()
    {
        var timeMachine = new TestingTimeMachine
        {
            Now = new(2023, 03, 01, 1, 1, 1, DateTimeKind.Utc)
        };
        var repo = new RoutingGraphRepository(DbContext, timeMachine);

        await repo.AddVersion();

        var response = await Client.GetAsync("config");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns404WithoutAnyVersion()
    {
        var response = await Client.GetAsync("config");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public ConfigControllerTests(IntegrationTestFactory factory)
        : base(factory) { }
}
