namespace Settings;

using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

public record AppSettings
{
    [Required]
    public required Bbox Bbox { get; init; }
}

public record Bbox
{
    [Required]
    public required LatLng SouthWest { get; init; }

    [Required]
    public required LatLng NorthEast { get; init; }

    public Geometry AsRectangle() =>
        new GeometryFactory(new(), 4326).ToGeometry(
            new(SouthWest.ToCoordinate(), NorthEast.ToCoordinate())
        );
}

public record LatLng
{
    [Required]
    public required double Latitude { get; init; }

    [Required]
    public required double Longitude { get; init; }

    public Coordinate ToCoordinate() => new(Longitude, Latitude);
}
