namespace Graph;

public class Graph : IGraph
{
    public Graph(Dictionary<Guid, Node> nodes, Dictionary<Guid, List<Edge>> edges, Guid version)
    {
        this.nodes = nodes;
        this.edges = edges;
        Version = version;
    }

    public Guid Version { get; }
    private readonly Dictionary<Guid, Node> nodes;
    private readonly Dictionary<Guid, List<Edge>> edges;
    public IReadOnlyCollection<Node> Nodes => nodes.Values;
    public Node? GetNode(Guid id) => nodes.TryGetValue(id, out var value) ? value : null;

    public IEnumerable<Edge> GetEdgesFromNode(Node node)
    {
        var found = edges.TryGetValue(node.Id, out var value);
        return !found || value == null ? Enumerable.Empty<Edge>() : value;
    }

    public IEnumerable<Edge> GetEdges() => edges.SelectMany(x => x.Value);
}
