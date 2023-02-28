namespace Graph;

public interface IGraph
{
    Guid Version { get; }
    IReadOnlyCollection<Node> Nodes { get; }
    Node? GetNode(Guid id);
    IEnumerable<Edge> GetEdgesFromNode(Node node);
    IEnumerable<Edge> GetEdges();
}
