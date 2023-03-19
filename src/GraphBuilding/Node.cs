namespace GraphBuilding;

using NetTopologySuite.Geometries;

public record Node
{
    public required long Id { get; init; }
    public required Point Coordinates { get; init; }
    public required decimal Level { get; init; }
    public required long? SourceId { get; init; }
}
