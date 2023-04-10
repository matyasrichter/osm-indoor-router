namespace GraphBuilding.LineProcessors;

using Parsers;
using Ports;

public class GenericHighwayProcessor : BaseLineProcessor, ILineProcessor
{
    private readonly LevelParser levelParser;

    public GenericHighwayProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm) => this.levelParser = levelParser;

    public async Task<ProcessingResult> Process(OsmLine source)
    {
        var levelTag = source.Tags.GetValueOrDefault("level");
        var repeatOnTag = source.Tags.GetValueOrDefault("repeat_on");
        var levels = (
            levelTag is null ? Enumerable.Empty<decimal>() : levelParser.Parse(levelTag)
        ).ToList();
        var repeatOnLevels = (
            repeatOnTag is null ? Enumerable.Empty<decimal>() : levelParser.Parse(repeatOnTag)
        ).ToList();

        var ogLevel =
            levels.Count != 0
                ? levels.Min()
                : repeatOnLevels.Count != 0
                    ? repeatOnLevels.Min()
                    : 0M;
        var levelDiff = levels.Count != 0 ? levels.Max() - levels.Min() : 0M;

        repeatOnLevels = repeatOnLevels.Where(x => x != ogLevel).ToList();

        var ogLevelLine = await ProcessSingleLevel(source, ogLevel, levelDiff);

        var result = new ProcessingResult(new(), new());
        result.Nodes.AddRange(ogLevelLine.Nodes);
        result.Edges.AddRange(ogLevelLine.Edges);

        var nodeOffset = result.Nodes.Count;
        foreach (var l in repeatOnLevels)
        {
            var levelOffset = l - ogLevel;
            result.Nodes.AddRange(
                ogLevelLine.Nodes.Select(x => x with { Level = x.Level + levelOffset })
            );
            result.Edges.AddRange(
                ogLevelLine.Edges.Select(
                    x => x with { FromId = x.FromId + nodeOffset, ToId = x.ToId + nodeOffset }
                )
            );
            nodeOffset += ogLevelLine.Nodes.Count;
        }

        return result;
    }

    private async Task<ProcessingResult> ProcessSingleLevel(
        OsmLine source,
        decimal level,
        decimal maxLevelOffset
    )
    {
        var result = new ProcessingResult(new(), new());
        InMemoryNode? prev = null;
        var currLevel = level;
        var points = await Osm.GetPointsByOsmIds(source.Nodes);
        var coords = source.Geometry.Coordinates.Zip(source.Nodes.Zip(points));
        foreach (var (coord, osmNode) in coords)
        {
            var isLevelConnection = false;
            // for multi-level ways, we need to handle level connections
            if (maxLevelOffset != 0)
            {
                var levelTag = osmNode.Second?.Tags.GetValueOrDefault("level");
                var repeatOnTag = osmNode.Second?.Tags.GetValueOrDefault("repeat_on");
                // we need the lowest (original) level of the node
                // taking min handles cases where a node is incorrectly tagged with multiple levels
                currLevel = levelTag is not null
                    ? levelParser.Parse(levelTag).Min()
                    : repeatOnTag is not null
                        ? levelParser.Parse(repeatOnTag).Min()
                        : currLevel;
                // level connections are nodes "between" levels,
                // we set this flag if this line is not single-level and this node does not have a level tag
                isLevelConnection = levelTag is null && repeatOnTag is null;
            }

            InMemoryNode node =
                new(Gf.CreatePoint(coord), currLevel, osmNode.First, isLevelConnection);

            if (prev is not null)
            {
                var distance = prev.Coordinates.GetMetricDistance(node.Coordinates);
                result.Edges.Add(
                    new(
                        result.Nodes.Count - 1,
                        result.Nodes.Count,
                        distance,
                        distance,
                        source.WayId
                    )
                );
            }

            result.Nodes.Add(node);
            prev = node;
        }

        return result;
    }
}
