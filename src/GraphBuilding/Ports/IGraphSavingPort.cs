namespace GraphBuilding.Ports;

using NetTopologySuite.Geometries;

public record InsertedNode(long Version, Point Coordinates, decimal Level, long? SourceId);

public record InsertedEdge(
    long Version,
    long FromId,
    long ToId,
    double Cost,
    double ReverseCost,
    long? SourceId
);

public interface IGraphSavingPort
{
    Task<Node> SaveNode(InsertedNode node);
    Task<IEnumerable<Edge>> SaveEdges(IEnumerable<InsertedEdge> edges);
    Task<long> AddVersion();
    Task FinalizeVersion(long version);
}
