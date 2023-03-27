namespace API.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Responses;
using Routing;
using Routing.Entities;
using Routing.Ports;
using Settings;

public class RoutingController
{
    private readonly RoutingService routingService;
    private readonly AppSettings settings;
    private readonly IGraphVersionProvider graphVersionProvider;

    public RoutingController(
        RoutingService routingService,
        AppSettings settings,
        IGraphVersionProvider graphVersionProvider
    )
    {
        this.routingService = routingService;
        this.settings = settings;
        this.graphVersionProvider = graphVersionProvider;
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(RoutingConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<RoutingConfig>, NotFound>> GetRoute()
    {
        var version = await graphVersionProvider.GetCurrentGraphVersion();
        if (version is null)
        {
            return TypedResults.NotFound();
        }

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

    [HttpGet("route")]
    [ProducesResponseType(typeof(Route), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<Route>, NotFound>> GetRoute(
        [FromQuery] [Required] long from,
        [FromQuery] [Required] long to,
        [FromQuery] [Required] long graphVersion
    )
    {
        var route = await routingService.FindRoute(from, to, graphVersion);
        if (route == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(route);
    }
}
