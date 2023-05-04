namespace GraphBuilding.ElementProcessors;

using Parsers;
using NetTopologySuite.Geometries;

public abstract class BaseOsmProcessor
{
    protected static readonly GeometryFactory Gf = new(new(), 4326);
    protected LevelParser LevelParser { get; }

    protected BaseOsmProcessor(LevelParser levelParser) => LevelParser = levelParser;

    protected (
        decimal OgLevel,
        decimal LevelDiff,
        IList<decimal> RepeatOnLevels
    ) ExtractLevelInformation(IReadOnlyDictionary<string, string> tags)
    {
        var levelTag = tags.GetValueOrDefault("level");
        var repeatOnTag = tags.GetValueOrDefault("repeat_on");
        var levels = (
            levelTag is null ? Enumerable.Empty<decimal>() : LevelParser.Parse(levelTag)
        ).ToList();
        var repeatOnLevels = (
            repeatOnTag is null ? Enumerable.Empty<decimal>() : LevelParser.Parse(repeatOnTag)
        ).ToList();

        var ogLevel =
            levels.Count != 0
                ? levels.Min()
                : repeatOnLevels.Count != 0
                    ? repeatOnLevels.Min()
                    : 0M;
        var levelDiff = levels.Count != 0 ? levels.Max() - levels.Min() : 0M;

        repeatOnLevels = repeatOnLevels.Where(x => x != ogLevel).ToList();
        return (ogLevel, levelDiff, repeatOnLevels);
    }

    protected decimal? ExtractNodeLevelInformation(IReadOnlyDictionary<string, string> tags)
    {
        var levelTag = tags.GetValueOrDefault("level");
        var repeatOnTag = tags.GetValueOrDefault("repeat_on");
        // we need the lowest (original) level of the node
        // taking min handles cases where a node is incorrectly tagged with multiple levels
        return levelTag is not null
            ? LevelParser.Parse(levelTag).Min()
            : repeatOnTag is not null
                ? LevelParser.Parse(repeatOnTag).Min()
                : null;
    }

    protected static IEnumerable<(decimal LowestLevel, ProcessingResult Result)> DuplicateResults(
        ProcessingResult ogResult,
        IList<decimal> repeatOnLevels,
        decimal ogLevel
    ) =>
        repeatOnLevels
            .Select(
                l =>
                    (
                        l,
                        new ProcessingResult(
                            ogResult.Nodes
                                .Select(x => x with { Level = x.Level + l - ogLevel })
                                .ToList(),
                            ogResult.Edges.ToList(),
                            ogResult.WallEdges.Select(x => (l, x.Edge)).ToList()
                        )
                    )
            )
            .Prepend((ogLevel, ogResult));

    protected static ProcessingResult JoinResults(IEnumerable<ProcessingResult> results)
    {
        var nodes = new List<InMemoryNode>();
        var edges = new List<InMemoryEdge>();
        var walls = new List<(decimal Level, (int FromId, int ToId) Edge)>();

        foreach (var result in results)
        {
            var nodeOffset = nodes.Count;
            nodes.AddRange(result.Nodes);
            edges.AddRange(
                result.Edges.Select(
                    x => x with { FromId = x.FromId + nodeOffset, ToId = x.ToId + nodeOffset }
                )
            );
            walls.AddRange(result.WallEdges);
        }

        return new(nodes, edges, walls);
    }
}
