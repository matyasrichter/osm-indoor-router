namespace GraphBuilding;

public class GraphBuilder
{
    public List<InMemoryNode> Nodes { get; } = new();
    public List<InMemoryEdge> Edges { get; } = new();

    // sourceId -> level -> nodes index
    private readonly Dictionary<long, Dictionary<decimal, int>> sourceIdToNodeId = new();

    public InMemoryNode? GetNode(int id) => Nodes.Count < id ? null : Nodes[id];

    public (int Id, InMemoryNode Node)? GetNodeBySourceId(long sourceId, int level)
    {
        var ids = sourceIdToNodeId.GetValueOrDefault(sourceId);
        if (ids == default)
            return null;

        var id = ids.GetValueOrDefault(level);
        if (id == default)
            return null;

        return (id, Nodes[id]);
    }

    public int AddNode(InMemoryNode inMemoryNode)
    {
        var id = Nodes.Count;
        Nodes.Add(inMemoryNode);
        if (inMemoryNode.SourceId is { } sourceId)
        {
            if (!sourceIdToNodeId.ContainsKey(sourceId))
                sourceIdToNodeId[sourceId] = new() { { inMemoryNode.Level, id } };
            else
                _ = sourceIdToNodeId[sourceId][inMemoryNode.Level] = id;
        }

        return id;
    }

    public void AddEdge(InMemoryEdge inMemoryEdge) => Edges.Add(inMemoryEdge);

    public IEnumerable<(long InternalId, InMemoryNode Node)> GetNodesWithInternalIds() =>
        Nodes.Select((x, i) => ((long)i, x));

    public IEnumerable<InMemoryEdge> GetRemappedEdges(IReadOnlyDictionary<long, long> nodeIdsMap) =>
        Edges.Select(x => x with { FromId = nodeIdsMap[x.FromId], ToId = nodeIdsMap[x.ToId] });
}
