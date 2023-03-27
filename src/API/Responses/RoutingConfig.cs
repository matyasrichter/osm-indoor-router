namespace API.Responses;

public record RoutingConfig(long GraphVersion, Bbox Bbox);

public record Bbox(LngLat SouthWest, LngLat NorthEast);

public record LngLat(double Longitude, double Latitude);
