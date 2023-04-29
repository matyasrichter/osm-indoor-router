namespace GraphBuilding.ElementProcessors;

using GraphBuilding.Parsers;
using GraphBuilding.Ports;
using NetTopologySuite.Geometries;

public class UnwalledAreaProcessor : BaseOsmProcessor
{
    public UnwalledAreaProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm, levelParser) { }

    public async Task<ProcessingResult> Process(
        OsmMultiPolygon source,
        IEnumerable<InMemoryNode> nodesInEnvelope
    )
    {
        var (ogLevel, _, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var resultsWithLevels = DuplicateResults(
            await ProcessSingleLevel(source, ogLevel),
            repeatOnLevels,
            ogLevel
        );
        var nodeCandidatesToAdd = nodesInEnvelope
            .Where(x => source.Geometry.Covers(x.Coordinates))
            .ToList();
        var results = resultsWithLevels.Select(
            x =>
                AddNodesFromEnvelope(
                    x.Result,
                    nodeCandidatesToAdd.Where(n => n.Level == x.LowestLevel),
                    source
                )
        );
        return JoinResults(results);
    }

    private async Task<ProcessingResult> ProcessSingleLevel(OsmMultiPolygon source, decimal level)
    {
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        var points = await Osm.GetPointsByOsmIds(source.Members.SelectMany(x => x.Nodes));
        var coords = source.Members
            .SelectMany(x => x.Nodes.Zip(x.Geometry.Coordinates))
            .Zip(points)
            .Select(x => (IdWithCoord: x.First, Point: x.Second))
            .DistinctBy(x => x.IdWithCoord.First);
        var nodeIds = new Dictionary<long, int>();
        foreach (
            var (from, to) in GetPairs(coords)
                .Where(x => x.Item1.IdWithCoord.First != x.Item2.IdWithCoord.First)
        )
        {
            var lineGeometry = Gf.CreateLineString(
                new[] { from.IdWithCoord.Second, to.IdWithCoord.Second }
            );
            if (!lineGeometry.CoveredBy(source.Geometry))
                continue;
            var fromId = GetNodeId(nodes, nodeIds, from, level);
            var toId = GetNodeId(nodes, nodeIds, to, level);
            var distance = from.IdWithCoord.Second.GetMetricDistance(to.IdWithCoord.Second, 0);
            edges.Add(new(fromId, toId, distance, distance, source.AreaId, distance));
        }

        return new(nodes, edges);
    }

    private static ProcessingResult AddNodesFromEnvelope(
        ProcessingResult result,
        IEnumerable<InMemoryNode> nodeCandidates,
        OsmMultiPolygon source
    )
    {
        foreach (var node in nodeCandidates)
        {
            var count = result.Nodes.Count;
            result.Nodes.Add(node);
            for (var i = 0; i < count; i++)
            {
                var existingNode = result.Nodes[i];
                var lineGeometry = Gf.CreateLineString(
                    new[] { node.Coordinates.Coordinate, existingNode.Coordinates.Coordinate }
                );
                if (!lineGeometry.CoveredBy(source.Geometry))
                    continue;
                var distance = node.Coordinates.GetMetricDistance(
                    existingNode.Coordinates,
                    Math.Abs(node.Level - existingNode.Level)
                );
                result.Edges.Add(
                    new(i, result.Nodes.Count - 1, distance, distance, source.AreaId, distance)
                );
            }
        }

        return result;
    }

    private static int GetNodeId(
        IList<InMemoryNode> result,
        Dictionary<long, int> nodeIds,
        ((long Id, Coordinate Coordinate) Node, OsmPoint? Point) node,
        decimal level
    )
    {
        if (!nodeIds.ContainsKey(node.Node.Id))
        {
            var fromNode = new InMemoryNode(
                Gf.CreatePoint(node.Node.Coordinate),
                level,
                node.Node.Id
            );
            result.Add(fromNode);
            nodeIds[node.Node.Id] = result.Count - 1;
        }

        return nodeIds[node.Node.Id];
    }

    private static IEnumerable<(T, T)> GetPairs<T>(IEnumerable<T> source)
    {
        var list = source.ToList();
        for (var i = 0; i < list.Count - 1; i++)
            for (var j = i + 1; j < list.Count; j++)
                yield return (list[i], list[j]);
    }
}
