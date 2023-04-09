namespace GraphBuilding;

using NetTopologySuite.Geometries;

public record InMemoryNode(
    Point Coordinates,
    decimal Level,
    long? SourceId,
    bool IsLevelConnection = false
);
