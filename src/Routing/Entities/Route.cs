namespace Routing.Entities;

using NetTopologySuite.Geometries;

public record RouteNode(Guid Id, Point Coordinates, decimal Level);

public record Route(double TotalMeters, IReadOnlyCollection<RouteNode> Nodes);
