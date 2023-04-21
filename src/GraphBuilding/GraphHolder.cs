namespace GraphBuilding;

using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;

public class GraphHolder
{
    private KdTree<InMemoryNode> NodesIndex { get; } = new();
    public IList<InMemoryNode> Nodes { get; } = new List<InMemoryNode>();
    public IList<InMemoryEdge> Edges { get; } = new List<InMemoryEdge>();

    // sourceId -> level -> nodes index
    private readonly Dictionary<(long SourceId, decimal Level), int> sourceIdToNodeId = new();

    public InMemoryNode? GetNode(int id) => Nodes.Count < id ? null : Nodes[id];

    public IEnumerable<InMemoryNode> GetNodesInArea(Envelope area) =>
        NodesIndex.Query(area).Select(x => x.Data);

    public (int Id, InMemoryNode Node)? GetNodeBySourceId(long sourceId, decimal level) =>
        sourceIdToNodeId.TryGetValue((sourceId, level), out var id) ? (id, Nodes[id]) : null;

    public int AddNode(InMemoryNode inMemoryNode)
    {
        if (inMemoryNode is { SourceId: { } sid, Level: var level })
        {
            var key = (sid, level);
            if (!sourceIdToNodeId.ContainsKey(key))
            {
                var id = Nodes.Count;
                Nodes.Add(inMemoryNode);
                _ = NodesIndex.Insert(inMemoryNode.Coordinates.Coordinate, inMemoryNode);
                sourceIdToNodeId[key] = id;
                return id;
            }
            else
                return sourceIdToNodeId[key];
        }
        else
        {
            var id = Nodes.Count;
            Nodes.Add(inMemoryNode);
            _ = NodesIndex.Insert(inMemoryNode.Coordinates.Coordinate, inMemoryNode);
            return id;
        }
    }

    public void AddEdge(InMemoryEdge inMemoryEdge) => Edges.Add(inMemoryEdge);

    public IEnumerable<(long InternalId, InMemoryNode Node)> GetNodesWithInternalIds() =>
        Nodes.Select((x, i) => ((long)i, x));

    public IEnumerable<InMemoryEdge> GetRemappedEdges(IReadOnlyDictionary<long, long> nodeIdsMap) =>
        Edges.Select(x => x with { FromId = nodeIdsMap[x.FromId], ToId = nodeIdsMap[x.ToId] });
}
