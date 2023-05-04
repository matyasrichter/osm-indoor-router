namespace GraphBuilding.Ports;

public interface IGraphSavingPort
{
    Task<IEnumerable<long>> SaveNodes(IEnumerable<InMemoryNode> nodes, long version);
    Task<IEnumerable<long>> SaveEdges(IEnumerable<InMemoryEdge> edges, long version);
    Task RemoveNodesWithoutEdges();
    Task<long> AddVersion();
    Task FinalizeVersion(long version);
}
