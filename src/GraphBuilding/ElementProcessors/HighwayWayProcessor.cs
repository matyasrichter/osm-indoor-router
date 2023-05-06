namespace GraphBuilding.ElementProcessors;

using Parsers;
using Ports;

public class HighwayWayProcessor : BaseOsmProcessor
{
    public HighwayWayProcessor(LevelParser levelParser)
        : base(levelParser) { }

    public ProcessingResult Process(OsmLine source, IReadOnlyDictionary<long, OsmPoint> points)
    {
        var (ogLevel, levelDiff, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var ogLevelLine = ProcessSingleLevel(source, ogLevel, levelDiff, points);
        if (repeatOnLevels.Count > 0)
            return JoinResults(
                DuplicateResults(ogLevelLine, repeatOnLevels, ogLevel).Select(x => x.Result)
            );
        return ogLevelLine;
    }

    private ProcessingResult ProcessSingleLevel(
        OsmLine source,
        decimal level,
        decimal maxLevelOffset,
        IReadOnlyDictionary<long, OsmPoint> osmPoints
    )
    {
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        InMemoryNode? prev = null;
        var currLevel = level;
        var points = source.Nodes.Select(osmPoints.GetValueOrDefault);
        var coords = source.Geometry.Coordinates.Zip(source.Nodes.Zip(points));
        foreach (var (coord, osmNode) in coords)
        {
            var isLevelConnection = false;
            if (maxLevelOffset != 0)
            {
                var nodeLevel = osmNode.Second?.Tags is not null
                    ? ExtractNodeLevelInformation(osmNode.Second.Tags)
                    : null;
                currLevel = nodeLevel ?? currLevel;
                // level connections are nodes "between" levels,
                // we set this flag if this line is not single-level and this node does not have a level tag
                isLevelConnection = maxLevelOffset != 0 && nodeLevel is null;
            }

            InMemoryNode node =
                new(
                    Gf.CreatePoint(coord),
                    currLevel,
                    new(SourceType.Point, osmNode.First),
                    isLevelConnection
                );

            if (prev is not null)
            {
                var distance = prev.Coordinates.Coordinate.GetMetricDistance(
                    node.Coordinates.Coordinate,
                    prev.Level - node.Level
                );
                edges.Add(
                    new(
                        nodes.Count - 1,
                        nodes.Count,
                        prev.Coordinates.GetLineStringTo(node.Coordinates),
                        distance,
                        distance,
                        new(SourceType.Line, source.WayId),
                        distance
                    )
                );
            }

            nodes.Add(node);
            prev = node;
        }

        return new(nodes, edges, new List<(decimal Level, (int FromId, int ToId) Edge)>());
    }
}
