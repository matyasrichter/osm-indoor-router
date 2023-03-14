namespace API.Controllers;

using System.ComponentModel.DataAnnotations;
using Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Routing;

public class RoutingController
{
    private readonly RoutingService routingService;

    public RoutingController(RoutingService routingService) => this.routingService = routingService;

    [HttpGet("route")]
    [ProducesResponseType(typeof(RoutingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<RoutingResult>, NotFound>> GetRoute(
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

        var apiRoute = new RoutingResult(
            route.TotalMeters,
            route.Nodes.Select(
                x => new RouteNode(x.Id, new(x.Coordinates.X, x.Coordinates.Y), x.Level)
            )
        );
        return TypedResults.Ok(apiRoute);
    }
}
