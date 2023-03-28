namespace API.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Routing.Entities;
using Routing.Ports;

[Route("search")]
public class SearchController : Controller
{
    private readonly INodeFinder nodeFinder;

    public SearchController(INodeFinder nodeFinder) => this.nodeFinder = nodeFinder;

    [HttpGet("closest-node")]
    [ProducesResponseType(typeof(RouteNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<RouteNode>, NotFound>> GetClosestNode(
        [FromQuery] [Required] double latitude,
        [FromQuery] [Required] double longitude,
        [FromQuery] [Required] decimal level,
        [FromQuery] [Required] long graphVersion
    )
    {
        var node = await nodeFinder.FindClosestNode(latitude, longitude, level, graphVersion);
        if (node == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(
            new RouteNode(node.Id, node.Coordinates.Y, node.Coordinates.X, node.Level)
        );
    }
}
