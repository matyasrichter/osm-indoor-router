namespace GraphBuilding;

using LineProcessors;
using Parsers;
using Ports;
using Settings;

public class GraphBuilder
{
    private readonly IOsmPort osm;
    private readonly AppSettings settings;
    private readonly LevelParser levelParser;

    public GraphBuilder(IOsmPort osm, AppSettings settings, LevelParser levelParser)
    {
        this.osm = new CachingOsmPortWrapper(osm);
        this.settings = settings;
        this.levelParser = levelParser;
    }

    public async Task BuildGraph(GraphHolder holder, CancellationToken ct)
    {
        var lines = await osm.GetLines(settings.Bbox.AsRectangle());
        if (ct.IsCancellationRequested)
            return;
        var hwProcessor = new GenericHighwayProcessor(osm, levelParser);
        foreach (var line in lines)
        {
            if (!line.Tags.ContainsKey("highway"))
                continue;
            SaveLine(holder, await hwProcessor.Process(line));
        }
    }

    private static void SaveLine(GraphHolder holder, ProcessingResult line)
    {
        var nodeIdMap = line.Nodes
            .Select(x => GetOrCreateNode(holder, x))
            .Select((node, index) => (node, index))
            .ToDictionary(x => (long)x.index, x => x.node);
        foreach (var edge in line.Edges)
        {
            var remapped = edge with
            {
                FromId = nodeIdMap[edge.FromId],
                ToId = nodeIdMap[edge.ToId]
            };
            holder.AddEdge(remapped);
        }
    }

    private static int GetOrCreateNode(GraphHolder holder, InMemoryNode node)
    {
        var existingNode =
            node.SourceId is not null && !node.IsLevelConnection
                ? holder.GetNodeBySourceId(node.SourceId.Value, node.Level)
                : null;

        if (existingNode is { Id: var existingId })
            return existingId;
        return holder.AddNode(node);
    }
}
