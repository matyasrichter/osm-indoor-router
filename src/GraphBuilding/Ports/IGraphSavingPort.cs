namespace GraphBuilding.Ports;

using Graph;

public interface IGraphSavingPort
{
    Task SaveNodes(IEnumerable<Node> nodes, Guid version);
    Task SaveEdges(IEnumerable<Edge> edges, Guid version);
    Task SaveCurrentVersion(Guid version);
}
