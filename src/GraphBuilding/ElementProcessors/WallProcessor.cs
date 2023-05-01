namespace GraphBuilding.ElementProcessors;

using Microsoft.FSharp.Collections;
using Parsers;
using Ports;

public class WallProcessor : BaseOsmProcessor
{
    public WallProcessor(IOsmPort osm, LevelParser levelParser)
        : base(osm, levelParser) { }

    public ProcessingResult Process(OsmLine source)
    {
        var (ogLevel, _, repeatOnLevels) = ExtractLevelInformation(source.Tags);
        var ogLevelLine = ProcessSingleLevel(source, ogLevel);
        if (repeatOnLevels.Count > 0)
            return JoinResults(
                DuplicateResults(ogLevelLine, repeatOnLevels, ogLevel).Select(x => x.Result)
            );
        return ogLevelLine;
    }

    private static ProcessingResult ProcessSingleLevel(OsmLine source, decimal level)
    {
        var nodes = source.Nodes
            .Zip(source.Geometry.Coordinates)
            .Select(x => new InMemoryNode(Gf.CreatePoint(x.Second), level, x.First, false))
            .ToList();
        return new(
            nodes,
            new List<InMemoryEdge>(),
            SeqModule
                .Windowed(2, Enumerable.Range(0, nodes.Count))
                .Select(x => (level, (x[0], x[1])))
                .ToList()
        );
    }
}
