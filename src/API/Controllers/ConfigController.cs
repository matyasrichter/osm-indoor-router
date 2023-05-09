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
    private readonly IGraphInformationProvider graphInformationProvider;

    public ConfigController(
        IGraphInformationProvider graphInformationProvider,
        AppSettings settings
    )
    {
        this.graphInformationProvider = graphInformationProvider;
        this.settings = settings;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(RoutingConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<RoutingConfig>, NotFound>> GetConfig()
    {
        var version = await graphInformationProvider.GetCurrentGraphVersion();
        if (version is null)
            return TypedResults.NotFound();

        var flags = await graphInformationProvider.GetGraphFlags(version.Value);

        return TypedResults.Ok(
            new RoutingConfig(
                version.Value,
                new(
                    new(settings.Bbox.SouthWest.Longitude, settings.Bbox.SouthWest.Latitude),
                    new(settings.Bbox.NorthEast.Longitude, settings.Bbox.NorthEast.Latitude)
                ),
                flags.HasStairs,
                flags.HasEscalators,
                flags.HasElevators
            )
        );
    }
}
