namespace Graph;

public interface IGraph
{
    Guid Version { get; }
    IReadOnlyCollection<Node> Nodes { get; }
    Node? GetNode(Guid id);
    IEnumerable<Edge> GetEdges(Node node);
}
