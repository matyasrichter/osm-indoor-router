namespace Routing;

using Entities;
using Microsoft.Extensions.Logging;
using Ports;

public partial class RoutingService
{
    private readonly ILogger<RoutingService> logger;
    private readonly IRoutingPort routingPort;

    public RoutingService(ILogger<RoutingService> logger, IRoutingPort routingPort)
    {
        this.logger = logger;
        this.routingPort = routingPort;
    }

    public async Task<Route?> FindRoute(long from, long to, long graphVersion)
    {
        LogStartingRouting(from, to, graphVersion);

        var routeNodes = await routingPort.FindRoute(from, to, graphVersion);

        if (routeNodes.Count == 0)
        {
            LogCouldNotFindRoute(from, to, graphVersion);
            return null;
        }

        LogFoundRoute(from, to, routeNodes.Count, graphVersion);

        var route = routeNodes
            .Select(
                x =>
                    new RouteNode(
                        x.Node.Id,
                        x.Node.Coordinates.Y,
                        x.Node.Coordinates.X,
                        x.Node.Level,
                        x.Node.IsLevelConnection
                    )
            )
            .ToList();
        return new(route, routeNodes.Sum(x => x.Edge?.Distance ?? 0));
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting routing from {From} to {To} in graph version {Version}"
    )]
    private partial void LogStartingRouting(long from, long to, long version);

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
