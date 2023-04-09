namespace Routing.Entities;

using NetTopologySuite.Geometries;

public record Node(long Id, Point Coordinates, decimal Level, bool IsLevelConnection);

public record Edge(long Id, double Cost);

public record RouteSegment(Node Node, Edge? Edge, double AggregatedCost);

public record RouteNode(
    long Id,
    double Latitude,
    double Longitude,
    decimal Level,
    bool IsLevelConnection
);

public record Route(IReadOnlyCollection<RouteNode> Nodes);
