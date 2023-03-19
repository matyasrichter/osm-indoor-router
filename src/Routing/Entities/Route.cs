namespace Routing.Entities;

using NetTopologySuite.Geometries;

public record Node(long Id, Point Coordinates, decimal Level);

public record Edge(long Id, double Cost);

public record RouteSegment(Node Node, Edge? Edge, double AggregatedCost);

public record RouteNode(long Id, Point Coordinates, decimal Level);

public record Route(double TotalMeters, IReadOnlyCollection<RouteNode> Nodes);
