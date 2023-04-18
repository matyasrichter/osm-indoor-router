namespace GraphBuilding.LineProcessors;

using Parsers;
using Ports;

public class HighwayWayProcessor : BaseOsmProcessor, IOsmElementProcessor<OsmLine>
{
    public HighwayWayProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm, levelParser) { }

    public async Task<ProcessingResult> Process(OsmLine source)
    {
        var (ogLevel, levelDiff, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var ogLevelLine = await ProcessSingleLevel(source, ogLevel, levelDiff);

        return CreateReplicatedResult(ogLevelLine, repeatOnLevels, ogLevel);
    }

    private async Task<ProcessingResult> ProcessSingleLevel(
        OsmLine source,
        decimal level,
        decimal maxLevelOffset
    )
    {
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        InMemoryNode? prev = null;
        var currLevel = level;
        var points = await Osm.GetPointsByOsmIds(source.Nodes);
        var coords = source.Geometry.Coordinates.Zip(source.Nodes.Zip(points));
        foreach (var (coord, osmNode) in coords)
        {
            var nodeLevel = osmNode.Second?.Tags is not null
                ? ExtractNodeLevelInformation(osmNode.Second.Tags)
                : null;
            currLevel = nodeLevel ?? currLevel;
            // level connections are nodes "between" levels,
            // we set this flag if this line is not single-level and this node does not have a level tag
            var isLevelConnection = maxLevelOffset != 0 && nodeLevel is null;

            InMemoryNode node =
                new(Gf.CreatePoint(coord), currLevel, osmNode.First, isLevelConnection);

            if (prev is not null)
            {
                var distance = prev.Coordinates.Coordinate.GetMetricDistance(
                    node.Coordinates.Coordinate,
                    prev.Level - node.Level
                );
                edges.Add(new(nodes.Count - 1, nodes.Count, distance, distance, source.WayId));
            }

            nodes.Add(node);
            prev = node;
        }

        return new(nodes, edges);
    }
}
