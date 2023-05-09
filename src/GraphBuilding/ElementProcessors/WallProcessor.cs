namespace GraphBuilding.ElementProcessors;

using Microsoft.FSharp.Collections;
using NetTopologySuite.Geometries;
using Parsers;
using Ports;

public class WallProcessor
{
    private static readonly GeometryFactory Gf = new(new(), 4326);
    private LevelParser LevelParser { get; }

    public WallProcessor(LevelParser levelParser) => LevelParser = levelParser;

    public ProcessingResult Process(OsmLine source) =>
        Process(source.Tags, source.Nodes.Zip(source.Geometry.Coordinates));

    public ProcessingResult Process(OsmMultiPolygon source) =>
        Process(source.Tags, source.Members.SelectMany(x => x.Nodes.Zip(x.Geometry.Coordinates)));

    public ProcessingResult Process(OsmPolygon source) =>
        Process(source.Tags, source.Nodes.Zip(source.GeometryAsLinestring.Coordinates));

    private ProcessingResult Process(
        IReadOnlyDictionary<string, string> tags,
        IEnumerable<(long, Coordinate)> source
    )
    {
        var (ogLevel, _, repeatOnLevels) = ProcessorUtils.ExtractLevelInformation(
            LevelParser,
            tags
        );
        var ogLevelLine = ProcessSingleLevel(source, ogLevel);
        if (repeatOnLevels.Count > 0)
            return ProcessorUtils.JoinResults(
                ProcessorUtils
                    .DuplicateResults(ogLevelLine, repeatOnLevels, ogLevel)
                    .Select(x => x.Result)
            );
        return ogLevelLine;
    }

    private static ProcessingResult ProcessSingleLevel(
        IEnumerable<(long, Coordinate)> source,
        decimal level
    )
    {
        var nodes = source
            .Select(
                x =>
                    new InMemoryNode(Gf.CreatePoint(x.Item2), level, new(SourceType.Point, x.Item1))
            )
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
