namespace API.Controllers;

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
    public Results<Ok<RoutingResult>, NotFound> GetRoute([FromQuery] long from, [FromQuery] long to)
    {
        var route = routingService.FindRoute(from, to);
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
