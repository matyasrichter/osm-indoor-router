namespace Routing;

using Entities;
using Graph;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;

public partial class RoutingService
{
    private readonly GraphHolder graphHolder;
    private readonly ILogger<RoutingService> logger;

    public RoutingService(GraphHolder graphHolder, ILogger<RoutingService> logger)
    {
        this.graphHolder = graphHolder;
        this.logger = logger;
    }

    public Route? FindRoute(Guid from, Guid to)
    {
        LogStartingRouting(from, to);
        var graph = graphHolder.Graph;
        if (graph == null)
        {
            return null;
        }

        var routeNodes = AStar(graph, from, to);

        if (routeNodes.Count == 0)
        {
            LogCouldNotFindRoute(from, to, graph.Version);
            return null;
        }

        LogFoundRoute(from, to, routeNodes.Count, graph.Version);

        var route = routeNodes.Select(x => new RouteNode(x.Id, x.Coordinates, x.Level)).ToList();
        var distance = SeqModule
            .Windowed(2, route)
            .Aggregate(0d, (agg, pair) => agg + pair[0].Coordinates.Distance(pair[1].Coordinates));
        return new(distance, route);
    }

    private static List<Node> AStar(IGraph graph, Guid from, Guid to)
    {
        var fromNode = graph.GetNode(from);
        var toNode = graph.GetNode(to);
        if (fromNode == null || toNode == null)
        {
            return new();
        }

        var open = new PriorityQueue<Guid, double>();
        open.Enqueue(from, fromNode.Coordinates.Distance(toNode.Coordinates));
        var reverseMap = new Dictionary<Guid, Guid>();
        var costFromStart = new Dictionary<Guid, double>() { { from, 0 } };
        var costToGoal = new Dictionary<Guid, double>()
        {
            { from, fromNode.Coordinates.Distance(toNode.Coordinates) }
        };

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (current == to)
            {
                return BuildRoute(reverseMap, from, to).Select(x => graph.GetNode(x)!).ToList();
            }

            var currentNode = graph.GetNode(current)!;
            foreach (var edge in graph.GetEdgesFromNode(currentNode))
            {
                var neighbour = graph.GetNode(edge.ToId)!;
                var newCost =
                    costFromStart[current]
                    + currentNode.Coordinates.Distance(neighbour.Coordinates);
                if (newCost < costFromStart.GetValueOrDefault(neighbour.Id, double.MaxValue))
                {
                    costFromStart[neighbour.Id] = newCost;
                    costToGoal[neighbour.Id] =
                        newCost + neighbour.Coordinates.Distance(toNode.Coordinates);
                    reverseMap[neighbour.Id] = current;
                    open.Enqueue(neighbour.Id, costToGoal[neighbour.Id]);
                }
            }
        }

        return new();
    }

    private static List<Guid> BuildRoute(
        IReadOnlyDictionary<Guid, Guid> reverseMap,
        Guid from,
        Guid to
    )
    {
        var route = new List<Guid>();
        var current = to;
        while (current != from)
        {
            route.Insert(0, current);
            current = reverseMap[current];
        }

        if (route[0] != from)
        {
            route.Insert(0, from);
        }

        return route;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting routing from {From} to {To}")]
    private partial void LogStartingRouting(Guid from, Guid to);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Could not find route from {From} to {To} in graph version {Version}"
    )]
    private partial void LogCouldNotFindRoute(Guid from, Guid to, Guid version);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found route from {From} to {To} of length {Length} in graph version {Version}"
    )]
    private partial void LogFoundRoute(Guid from, Guid to, int length, Guid version);
}
