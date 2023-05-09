namespace GraphBuilding.ElementProcessors;

using Parsers;
using Ports;

public class PlainNodeProcessor
{
    private LevelParser LevelParser { get; }

    public PlainNodeProcessor(LevelParser levelParser) => LevelParser = levelParser;

    public ProcessingResult Process(OsmPoint source)
    {
        var (ogLevel, _, repeatOnLevels) = ProcessorUtils.ExtractLevelInformation(
            LevelParser,
            source.Tags
        );
        return new(
            repeatOnLevels
                .Prepend(ogLevel)
                .Select(
                    x => new InMemoryNode(source.Geometry, x, new(SourceType.Point, source.NodeId))
                )
                .ToList(),
            new List<InMemoryEdge>(),
            new List<(decimal Level, (int FromId, int ToId) Edge)>()
        );
    }
}
