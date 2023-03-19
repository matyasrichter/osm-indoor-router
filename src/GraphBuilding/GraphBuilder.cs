namespace GraphBuilding;

public class GraphBuilder
{
    public long Version { get; }
    private readonly Dictionary<long, Node> nodes = new();
    private readonly Dictionary<long, long> sourceIdToId = new();
    private readonly Dictionary<long, List<Edge>> edges = new();

    public GraphBuilder(long version) => Version = version;

    public bool HasNode(long id) => nodes.ContainsKey(id);

    public Node? GetNode(long id) => nodes.GetValueOrDefault(id);

    public Node? GetNodeBySourceId(long sourceId)
    {
        var id = sourceIdToId.GetValueOrDefault(sourceId);
        if (id == default)
        {
            return null;
        }

        return nodes.GetValueOrDefault(id);
    }

    public GraphBuilder AddNode(Node node)
    {
        nodes[node.Id] = node;
        if (node.SourceId is not null)
        {
            sourceIdToId[node.SourceId.Value] = node.Id;
        }

        return this;
    }

    public GraphBuilder AddEdge(Edge edge)
    {
        if (!edges.ContainsKey(edge.FromId))
        {
            edges[edge.FromId] = new() { edge };
            return this;
        }

        edges[edge.FromId].Add(edge);
        return this;
    }
}
