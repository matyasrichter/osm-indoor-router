namespace GraphBuilding;

using NetTopologySuite.Geometries;

public record InMemoryEdge(
    long FromId,
    long ToId,
    LineString Geometry,
    double Cost,
    double ReverseCost,
    long? SourceId,
    double Distance
);
