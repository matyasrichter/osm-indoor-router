namespace GraphBuilding;

using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;

public class GraphHolder
{
    private KdTree<InMemoryNode> NodesIndex { get; } = new();
    public IList<InMemoryNode> Nodes { get; } = new List<InMemoryNode>();
    public IList<InMemoryEdge> Edges { get; } = new List<InMemoryEdge>();

    public IEnumerable<((int Id, InMemoryNode Node), (int Id, InMemoryNode Node))> GetWallEdges(
        decimal level
    ) =>
        WallEdgeIds.TryGetValue(level, out var value)
            ? value.Select(x => ((x.Item1, Nodes[x.Item1]), (x.Item2, Nodes[x.Item2])))
            : Enumerable.Empty<((int, InMemoryNode), (int, InMemoryNode))>();

    public IEnumerable<decimal> WallEdgeLevels => WallEdgeIds.Keys;

    // private Dictionary<decimal, HashSet<int>> WallNodeIds { get; } = new();
    private Dictionary<decimal, HashSet<(int, int)>> WallEdgeIds { get; } = new();

    // sourceId -> level -> nodes index
    private readonly Dictionary<(long SourceId, decimal Level), int> sourceIdToNodeId = new();

    public InMemoryNode? GetNode(int id) => Nodes.Count < id ? null : Nodes[id];

    public IEnumerable<InMemoryNode> GetNodesInArea(Envelope area) =>
        NodesIndex.Query(area).Select(x => x.Data);

    public (int Id, InMemoryNode Node)? GetNodeBySourceId(long sourceId, decimal level) =>
        sourceIdToNodeId.TryGetValue((sourceId, level), out var id) ? (id, Nodes[id]) : null;

    public int AddNode(InMemoryNode inMemoryNode, bool checkUniqueness = true)
    {
        if (checkUniqueness && inMemoryNode is { SourceId: { } sid, Level: var level })
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

    public void AddWallEdge((int FromId, int ToId) edge, decimal level)
    {
        // _ = WallNodeIds.Add(edge.FromId);
        // _ = WallNodeIds.Add(edge.ToId);
        if (WallEdgeIds.TryGetValue(level, out var levelEdges))
            _ = levelEdges.Add(edge);
        else
            WallEdgeIds[level] = new(new UnorderedPairEqualityComparer()) { edge };
    }

    public IEnumerable<(long InternalId, InMemoryNode Node)> GetNodesWithInternalIds() =>
        Nodes.Select((x, i) => ((long)i, x));

    public IEnumerable<InMemoryEdge> GetRemappedEdges(IReadOnlyDictionary<long, long> nodeIdsMap) =>
        Edges.Select(x => x with { FromId = nodeIdsMap[x.FromId], ToId = nodeIdsMap[x.ToId] });
}

internal sealed class UnorderedPairEqualityComparer : IEqualityComparer<(int, int)>
{
    public bool Equals((int, int) x, (int, int) y) =>
        (x.Item1 == y.Item1 && x.Item2 == y.Item2) || (x.Item1 == x.Item2 && x.Item2 == y.Item1);

    public int GetHashCode((int, int) obj) => obj.Item1.GetHashCode() + obj.Item2.GetHashCode();
}
