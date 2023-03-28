namespace API.Controllers;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Responses;
using Routing.Ports;
using Settings;

[Route("config")]
public class ConfigController : Controller
{
    private readonly AppSettings settings;
    private readonly IGraphVersionProvider graphVersionProvider;

    public ConfigController(IGraphVersionProvider graphVersionProvider, AppSettings settings)
    {
        this.graphVersionProvider = graphVersionProvider;
        this.settings = settings;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(RoutingConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<RoutingConfig>, NotFound>> GetConfig()
    {
        var version = await graphVersionProvider.GetCurrentGraphVersion();
        if (version is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(
            new RoutingConfig(
                version.Value,
                new(
                    new(settings.Bbox.SouthWest.Longitude, settings.Bbox.SouthWest.Latitude),
                    new(settings.Bbox.NorthEast.Longitude, settings.Bbox.NorthEast.Latitude)
                )
            )
        );
    }
}
