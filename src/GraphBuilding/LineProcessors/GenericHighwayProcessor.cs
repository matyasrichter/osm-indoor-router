namespace GraphBuilding.LineProcessors;

using Parsers;
using Ports;

public class GenericHighwayProcessor : BaseLineProcessor, ILineProcessor
{
    private readonly LevelParser levelParser;

    public GenericHighwayProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm) => this.levelParser = levelParser;

    private static readonly IEnumerable<decimal> GroundLevelEnumerable = new[] { 0M };

    public Task<ProcessingResult> Process(OsmLine source)
    {
        var levelTag = source.Tags.GetValueOrDefault("level");
        var repeatOnTag = source.Tags.GetValueOrDefault("repeat_on");
        var levels = (levelTag, repeatOnTag) switch
        {
            (null, null) => GroundLevelEnumerable,
            (null, not null) => levelParser.Parse(repeatOnTag),
            (not null, null) => levelParser.Parse(levelTag),
            _ => levelParser.Parse(levelTag).Concat(levelParser.Parse(repeatOnTag)).Distinct()
        };
        return Task.FromResult(
            levels.Aggregate(
                new ProcessingResult(new List<InMemoryNode>(), new List<InMemoryEdge>()),
                (agg, level) => ProcessSingleLevel(source, level, agg)
            )
        );
    }

    private static ProcessingResult ProcessSingleLevel(
        OsmLine source,
        decimal level,
        ProcessingResult agg
    )
    {
        var index = agg.Nodes.Count;
        var prevIndex = agg.Nodes.Count;
        InMemoryNode? prev = null;
        foreach (var (coord, nodeOsmId) in source.Geometry.Coordinates.Zip(source.Nodes))
        {
            InMemoryNode node = new(Gf.CreatePoint(coord), level, nodeOsmId);

            if (prev is not null)
            {
                var distance = prev.Coordinates.GetMetricDistance(node.Coordinates);
                agg.Edges.Add(new(prevIndex++, index, distance, distance, source.WayId));
            }

            agg.Nodes.Add(node);
            prev = node;
            index++;
        }

        return agg;
    }
}
