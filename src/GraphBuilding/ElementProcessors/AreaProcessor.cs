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
        var nodeSet = source.Members.SelectMany(x => x.Nodes).ToHashSet();
        var nodeCandidatesToAdd = nodesInEnvelope
            .Where(x => source.Geometry.Covers(x.Coordinates))
            .Where(x => x.SourceId is not null && !nodeSet.Contains(x.SourceId.Value))
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
            var fromId = GetNodeId(
                nodes,
                nodeIds,
                from.IdWithCoord.First,
                from.IdWithCoord.Second,
                level
            );
            var toId = GetNodeId(
                nodes,
                nodeIds,
                to.IdWithCoord.First,
                to.IdWithCoord.Second,
                level
            );
            var distance = from.IdWithCoord.Second.GetMetricDistance(to.IdWithCoord.Second, 0);
            edges.Add(new(fromId, toId, lineGeometry, distance, distance, source.AreaId, distance));
        }

        var wallEdges = new List<(decimal Level, (int FromId, int ToId) Edge)>();
        if (HasWalls(source.Tags))
        {
            foreach (var polygon in source.Members)
            {
                var nodeSourceIdList =
                    polygon.Nodes[0] == polygon.Nodes[^1]
                        ? polygon.Nodes
                            .Zip(polygon.Geometry.Coordinates)
                            .Append((polygon.Nodes[0], polygon.Geometry.Coordinates[0]))
                            .ToList()
                        : polygon.Nodes.Zip(polygon.Geometry.Coordinates);
                wallEdges.AddRange(
                    SeqModule
                        .Windowed(2, nodeSourceIdList)
                        .Select(
                            x =>
                                (
                                    level,
                                    (
                                        GetNodeId(nodes, nodeIds, x[0].First, x[0].Second, level),
                                        GetNodeId(nodes, nodeIds, x[1].First, x[1].Second, level)
                                    )
                                )
                        )
                );
            }
        }

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
        long osmId,
        Coordinate coordinate,
        decimal level
    )
    {
        if (!nodeIds.ContainsKey(osmId))
        {
            var fromNode = new InMemoryNode(Gf.CreatePoint(coordinate), level, osmId);
            result.Add(fromNode);
            nodeIds[osmId] = result.Count - 1;
        }

        return nodeIds[osmId];
    }
}
