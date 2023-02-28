namespace GraphBuilding;

using Graph;

public class GraphBuilder
{
    private readonly Guid version;
    private readonly Dictionary<Guid, Node> nodes = new();
    private readonly Dictionary<long, Guid> sourceIdToId = new();
    private readonly Dictionary<Guid, List<Edge>> edges = new();

    public GraphBuilder(Guid version) => this.version = version;

    public bool HasNode(Guid id) => nodes.ContainsKey(id);

    public Node? GetNode(Guid id) => nodes.GetValueOrDefault(id);

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

    public IGraph Build() => new DictionaryGraph(nodes, edges, version);
}
