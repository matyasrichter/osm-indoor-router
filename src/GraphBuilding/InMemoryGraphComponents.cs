namespace GraphBuilding;

using NetTopologySuite.Geometries;

public enum SourceType
{
    Point,
    Line,
    Polygon,
    Multipolygon
}

public record struct Source(SourceType Type, long Id);

public record InMemoryNode(
    Point Coordinates,
    decimal Level,
    Source? Source,
    bool IsLevelConnection = false
);

public record InMemoryEdge(
    long FromId,
    long ToId,
    LineString Geometry,
    double Cost,
    double ReverseCost,
    Source? Source,
    double Distance
);
