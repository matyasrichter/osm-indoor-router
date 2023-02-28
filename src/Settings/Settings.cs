namespace Settings;

using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

public record Settings
{
    [Required] public required Bbox Bbox { get; init; }
}

public record Bbox
{
    [Required] public required Coordinates SouthWest { get; init; }
    [Required] public required Coordinates NorthEast { get; init; }
}

public record Coordinates
{
    [Required] public required double Latitude { get; init; }
    [Required] public required double Longitude { get; init; }
    public Point AsPoint() => new(Latitude, Longitude);
};
