namespace GraphBuilding.Ports;

public interface IGraphSavingPort
{
    Task<IEnumerable<long>> SaveNodes(IEnumerable<InMemoryNode> nodes, long version);
    Task<IEnumerable<long>> SaveEdges(IEnumerable<InMemoryEdge> edges, long version);
    Task<int> RemoveNodesWithoutEdges(long version);
    Task<int> RemoveSmallComponents(decimal threshold, long version);
    Task<long> AddVersion();
    Task FinalizeVersion(long version);
}
