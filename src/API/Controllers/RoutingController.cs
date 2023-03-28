namespace API.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Routing;
using Routing.Entities;

[Route("route")]
public class RoutingController : Controller
{
    private readonly RoutingService routingService;

    public RoutingController(RoutingService routingService) => this.routingService = routingService;

    [HttpGet("")]
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
            return TypedResults.NotFound();

        return TypedResults.Ok(route);
    }
}
