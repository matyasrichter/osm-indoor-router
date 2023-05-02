namespace GraphBuilding.ElementProcessors;

using Core;
using Parsers;
using Ports;
using Microsoft.FSharp.Collections;
using NetTopologySuite.Geometries;

public class AreaProcessor : BaseOsmProcessor
{
    public AreaProcessor(LevelParser levelParser)
        : base(levelParser) { }

    public ProcessingResult Process(
        OsmMultiPolygon source,
        IEnumerable<InMemoryNode> nodesInEnvelope,
        IReadOnlyDictionary<long, OsmPoint> points
    )
    {
        var (ogLevel, _, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var resultsWithLevels = DuplicateResults(
            ProcessSingleLevel(source, ogLevel, points),
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

    private static ProcessingResult ProcessSingleLevel(
        OsmMultiPolygon source,
        decimal level,
        IReadOnlyDictionary<long, OsmPoint> osmPoints
    )
    {
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        var points = source.Members.SelectMany(x => x.Nodes).Select(osmPoints.GetValueOrDefault);
        var coords = source.Members
            .SelectMany(x => x.Nodes.Zip(x.Geometry.Coordinates))
            .Zip(points)
            .Select(x => (IdWithCoord: x.First, Point: x.Second))
            .DistinctBy(x => x.IdWithCoord.First);
        var nodeIds = new Dictionary<long, int>();
        foreach (
            var (from, to) in coords
                .GetPairs()
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
            edges.Add(new(fromId, toId, lineGeometry, distance, distance, source.AreaId, distance));
        }

        var wallEdges = HasWalls(source.Tags)
            ? SeqModule
                .Windowed(2, Enumerable.Range(0, nodes.Count))
                .Select(x => (level, (x[0], x[1])))
                .ToList()
            : new List<(decimal Level, (int FromId, int ToId) Edge)>();

        return new(nodes, edges, wallEdges);
    }

    private static bool HasWalls(IReadOnlyDictionary<string, string> tags) =>
        tags.GetValueOrDefault("indoor") is "room" or "wall";

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
                    new(
                        i,
                        result.Nodes.Count - 1,
                        lineGeometry,
                        distance,
                        distance,
                        source.AreaId,
                        distance
                    )
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
}
