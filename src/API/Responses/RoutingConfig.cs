namespace API.Responses;

public record RoutingConfig(
    long GraphVersion,
    Bbox Bbox,
    bool HasStairs,
    bool HasEscalators,
    bool HasElevators
);

public record Bbox(LngLat SouthWest, LngLat NorthEast);

public record LngLat(double Longitude, double Latitude);
