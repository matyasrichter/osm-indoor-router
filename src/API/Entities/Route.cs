namespace API.Entities;

public record Coordinates(double Latitude, double Longitude);

public record RouteNode(long Id, Coordinates Coordinates, decimal Level);

public record RoutingResult(double TotalMeters, IEnumerable<RouteNode> Nodes);
