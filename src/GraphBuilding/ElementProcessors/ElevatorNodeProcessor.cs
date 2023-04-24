namespace GraphBuilding.ElementProcessors;

using Parsers;
using Ports;

public class ElevatorNodeProcessor : BaseOsmProcessor
{
    public ElevatorNodeProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm, levelParser) { }

    public ProcessingResult Process(OsmPoint source)
    {
        var (ogLevel, _, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var ogResult = ProcessOgLevel(source);
        if (repeatOnLevels.Count > 0)
            return JoinResults(
                DuplicateResults(ogResult, repeatOnLevels, ogLevel).Select(x => x.Result)
            );
        return ogResult;
    }

    private ProcessingResult ProcessOgLevel(OsmPoint source)
    {
        var levels = source.Tags.GetValueOrDefault("level") switch
        {
            null or "" => Enumerable.Empty<decimal>(),
            var l => LevelParser.Parse(l)
        };
        InMemoryNode? prev = null;
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        const double levelVerticalDistance = 3.0;
        foreach (var level in levels.Order())
        {
            InMemoryNode node = new(source.Geometry, level, source.NodeId);
            if (prev is not null)
                edges.Add(
                    new(
                        nodes.Count - 1,
                        nodes.Count,
                        levelVerticalDistance,
                        levelVerticalDistance,
                        source.NodeId,
                        levelVerticalDistance
                    )
                );

            nodes.Add(node);
            prev = node;
        }

        return new(nodes, edges);
    }
}
