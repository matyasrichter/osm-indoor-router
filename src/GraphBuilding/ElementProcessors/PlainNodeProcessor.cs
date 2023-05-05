namespace GraphBuilding.ElementProcessors;

using Parsers;
using Ports;

public class PlainNodeProcessor : BaseOsmProcessor
{
    public PlainNodeProcessor(LevelParser levelParser)
        : base(levelParser) { }

    public ProcessingResult Process(OsmPoint source)
    {
        var (ogLevel, _, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        return new(
            repeatOnLevels
                .Prepend(ogLevel)
                .Select(x => new InMemoryNode(source.Geometry, x, source.NodeId))
                .ToList(),
            new List<InMemoryEdge>(),
            new List<(decimal Level, (int FromId, int ToId) Edge)>()
        );
    }
}
