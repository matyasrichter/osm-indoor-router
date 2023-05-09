namespace GraphBuilding.ElementProcessors;

using NetTopologySuite.Geometries;
using Parsers;
using Ports;

public class EntranceNodeProcessor
{
    private LevelParser LevelParser { get; }

    public EntranceNodeProcessor(LevelParser levelParser) => LevelParser = levelParser;

    public ProcessingResult Process(OsmPoint source)
    {
        var (ogLevel, _, repeatOnLevels) = ProcessorUtils.ExtractLevelInformation(
            LevelParser,
            source.Tags
        );

        var nodes = repeatOnLevels
            .Prepend(ogLevel)
            .Select(x => new InMemoryNode(source.Geometry, x, new(SourceType.Point, source.NodeId)))
            .ToList();
        if (ogLevel != 0 && !repeatOnLevels.Contains(0))
            nodes.Add(new(source.Geometry, 0, new(SourceType.Point, source.NodeId)));

        var groundLevelNodeId = nodes
            .Select((x, i) => (x, i))
            .Where(x => x.x.Level == 0)
            .Select(x => x.i)
            .First();

        var edges = nodes
            .Select((x, i) => (x, i))
            .Where(x => x.x.Level != 0)
            .Select(
                x =>
                    new InMemoryEdge(
                        groundLevelNodeId,
                        x.i,
                        LineString.Empty,
                        0,
                        0,
                        new(SourceType.Point, source.NodeId),
                        0
                    )
            )
            .ToList();

        return new(nodes, edges, new List<(decimal Level, (int FromId, int ToId) Edge)>());
    }
}
