namespace GraphBuilding.LineProcessors;

using NetTopologySuite.Geometries;
using Parsers;
using Ports;

public class UnwalledAreaProcessor : BaseOsmProcessor, IOsmElementProcessor<OsmPolygon>
{
    public UnwalledAreaProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm, levelParser) { }

    public async Task<ProcessingResult> Process(OsmPolygon source)
    {
        var (ogLevel, _, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var ogLevelResult = await ProcessSingleLevel(source, ogLevel);
        return CreateReplicatedResult(ogLevelResult, repeatOnLevels, ogLevel);
    }

    private async Task<ProcessingResult> ProcessSingleLevel(OsmPolygon source, decimal level)
    {
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        var points = await Osm.GetPointsByOsmIds(source.Nodes);
        var idsWithPoints = source.Nodes.Zip(points).Select(x => (OsmId: x.First, Point: x.Second));
        var coords = source.GeometryAsLinestring.Coordinates
            .Zip(idsWithPoints)
            .Select(x => (Coord: x.First, IdWithPoint: x.Second));
        var nodeIds = new Dictionary<long, int>();
        foreach (
            var (from, to) in GetPairs(coords)
                .Where(x => x.Item1.IdWithPoint.OsmId != x.Item2.IdWithPoint.OsmId)
        )
        {
            var lineGeometry = Gf.CreateLineString(new[] { from.Coord, to.Coord });
            if (!lineGeometry.CoveredBy(source.Geometry))
                continue;
            var fromId = GetNodeId(nodes, nodeIds, from, level);
            var toId = GetNodeId(nodes, nodeIds, to, level);
            var distance = from.Coord.GetMetricDistance(to.Coord, 0);
            edges.Add(new(fromId, toId, distance, distance, source.AreaId));
        }

        return new(nodes, edges);
    }

    private static int GetNodeId(
        IList<InMemoryNode> result,
        Dictionary<long, int> nodeIds,
        (Coordinate First, (long First, OsmPoint? Second) Second) node,
        decimal level
    )
    {
        var sourceId = node.Second.First;
        if (!nodeIds.ContainsKey(node.Second.First))
        {
            var fromNode = new InMemoryNode(Gf.CreatePoint(node.First), level, sourceId);
            result.Add(fromNode);
            nodeIds[sourceId] = result.Count - 1;
        }

        return nodeIds[sourceId];
    }

    private static IEnumerable<(T, T)> GetPairs<T>(IEnumerable<T> source)
    {
        var list = source.ToList();
        for (var i = 0; i < list.Count - 2; i++)
            for (var j = i + 1; j < list.Count - 1; j++)
                yield return (list[i], list[j]);
    }
}
