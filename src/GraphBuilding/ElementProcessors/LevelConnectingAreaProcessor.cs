namespace GraphBuilding.ElementProcessors;

using Core;
using Parsers;
using Ports;
using Microsoft.FSharp.Collections;
using NetTopologySuite.Geometries;

public class LevelConnectingAreaProcessor
{
    private static readonly GeometryFactory Gf = new(new(), 4326);

    private LevelParser LevelParser { get; }

    public LevelConnectingAreaProcessor(LevelParser levelParser) => LevelParser = levelParser;

    public ProcessingResult Process(
        OsmMultiPolygon source,
        IReadOnlyDictionary<long, OsmPoint> points,
        SourceType sourceType
    )
    {
        // builds a list of (point, level) pairs from all door=* nodes
        var nodeCandidates = source.Members
            .SelectMany(x => x.Nodes)
            .Select(points.GetValueOrDefault)
            .Where(x => x is not null)
            .Select(x => x!)
            .Where(x => x.Tags.GetValueOrDefault("door") is not (null or ""))
            .Select(x => (x, GetAllLevels(x.Tags)))
            .SelectMany(x => x.Item2.Select(l => (x.x, l)));
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        var nodeIds = new Dictionary<(long, decimal), int>();

        foreach (var (a, b) in nodeCandidates.GetPairs())
        {
            var fromId = GetNodeId(nodes, nodeIds, a.x.NodeId, a.x.Geometry, a.l);
            var toId = GetNodeId(nodes, nodeIds, b.x.NodeId, b.x.Geometry, b.l);
            var cost =
                a.x.NodeId == b.x.NodeId
                    ? 0
                    : a.x.Geometry.GetMetricDistance(b.x.Geometry, Math.Abs(a.l - b.l));
            var distance =
                a.x.NodeId == b.x.NodeId ? 0 : a.x.Geometry.GetMetricDistance(b.x.Geometry, 0);
            edges.Add(
                new(
                    fromId,
                    toId,
                    a.x.NodeId == b.x.NodeId
                        ? LineString.Empty
                        : a.x.Geometry.GetLineStringTo(b.x.Geometry),
                    cost,
                    cost,
                    new(sourceType, source.AreaId),
                    distance
                )
            );
        }

        var wallEdges = GetWallEdges(source, nodes, nodeIds);

        return new(nodes, edges, wallEdges);
    }

    private List<(decimal Level, (int FromId, int ToId) Edge)> GetWallEdges(
        OsmMultiPolygon source,
        List<InMemoryNode> nodes,
        Dictionary<(long, decimal), int> nodeIds
    )
    {
        var wallEdges = new List<(decimal Level, (int FromId, int ToId) Edge)>();
        var allSourceLevels = GetAllLevels(source.Tags).ToList();
        if (source.Tags.GetValueOrDefault("indoor") is "room" && allSourceLevels.Count > 0)
            foreach (var polygon in source.Members)
            {
                var nodeSourceIdList = polygon.Nodes.Zip(polygon.Geometry.Coordinates);
                wallEdges.AddRange(
                    // for each level, create wall edges between pairs of nodes
                    allSourceLevels.SelectMany(
                        l =>
                            SeqModule
                                .Windowed(2, nodeSourceIdList)
                                .Select(
                                    x =>
                                        (
                                            l,
                                            (
                                                GetNodeId(
                                                    nodes,
                                                    nodeIds,
                                                    x[0].First,
                                                    Gf.CreatePoint(x[0].Second),
                                                    l
                                                ),
                                                GetNodeId(
                                                    nodes,
                                                    nodeIds,
                                                    x[1].First,
                                                    Gf.CreatePoint(x[1].Second),
                                                    l
                                                )
                                            )
                                        )
                                )
                    )
                );
            }

        return wallEdges;
    }

    private IEnumerable<decimal> GetAllLevels(IReadOnlyDictionary<string, string> tags)
    {
        var levelTag = tags.GetValueOrDefault("level");
        var repeatOnTag = tags.GetValueOrDefault("repeat_on");
        if (levelTag is null && repeatOnTag is null)
            return Enumerable.Empty<decimal>();
        var levels = new List<decimal>();
        if (levelTag is not null)
            levels.AddRange(LevelParser.Parse(levelTag));
        if (repeatOnTag is not null)
            levels.AddRange(LevelParser.Parse(repeatOnTag));
        return levels.Distinct();
    }

    private static int GetNodeId(
        IList<InMemoryNode> result,
        Dictionary<(long, decimal), int> nodeIds,
        long osmId,
        Point coordinate,
        decimal level
    )
    {
        if (!nodeIds.ContainsKey((osmId, level)))
        {
            var fromNode = new InMemoryNode(coordinate, level, new(SourceType.Point, osmId));
            result.Add(fromNode);
            nodeIds[(osmId, level)] = result.Count - 1;
        }

        return nodeIds[(osmId, level)];
    }
}
