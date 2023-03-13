namespace Routing;

using Entities;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;

public partial class RoutingService
{
    private readonly ILogger<RoutingService> logger;

    public RoutingService(ILogger<RoutingService> logger) => this.logger = logger;

    public Route? FindRoute(long from, long to)
    {
        LogStartingRouting(from, to);

        var routeNodes = new List<RouteNode>();

        if (routeNodes.Count == 0)
        {
            LogCouldNotFindRoute(from, to, 0);
            return null;
        }

        LogFoundRoute(from, to, routeNodes.Count, 0);

        var route = routeNodes.Select(x => new RouteNode(x.Id, x.Coordinates, x.Level)).ToList();
        var distance = SeqModule
            .Windowed(2, route)
            .Aggregate(0d, (agg, pair) => agg + pair[0].Coordinates.Distance(pair[1].Coordinates));
        return new(distance * 100, route);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting routing from {From} to {To}")]
    private partial void LogStartingRouting(long from, long to);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Could not find route from {From} to {To} in graph version {Version}"
    )]
    private partial void LogCouldNotFindRoute(long from, long to, long version);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found route from {From} to {To} of length {Length} in graph version {Version}"
    )]
    private partial void LogFoundRoute(long from, long to, int length, long version);
}
