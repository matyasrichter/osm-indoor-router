namespace GraphBuilding;

using System.Runtime.CompilerServices;
using ElementProcessors;
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
        await foreach (var r in ProcessPoints(ct))
            SaveResult(holder, r);
        await foreach (var r in ProcessLines(ct))
            SaveResult(holder, r);
        await foreach (var r in ProcessPolygons(holder, ct))
            SaveResult(holder, r);
    }

    private async IAsyncEnumerable<ProcessingResult> ProcessPoints(
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var points = await osm.GetPoints(settings.Bbox.AsRectangle());
        if (ct.IsCancellationRequested)
            yield break;
        var elevatorProcessor = new ElevatorNodeProcessor(osm, levelParser);
        foreach (var point in points)
        {
            if (ct.IsCancellationRequested)
                yield break;
            if (
                point.Tags.GetValueOrDefault("elevator") is "yes"
                || point.Tags.GetValueOrDefault("highway") is "elevator"
            )
                yield return elevatorProcessor.Process(point);
        }
    }

    private async IAsyncEnumerable<ProcessingResult> ProcessLines(
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var lines = await osm.GetLines(settings.Bbox.AsRectangle());
        if (ct.IsCancellationRequested)
            yield break;
        var hwProcessor = new HighwayWayProcessor(osm, levelParser);
        foreach (var line in lines)
        {
            if (ct.IsCancellationRequested)
                yield break;
            if (!line.Tags.ContainsKey("highway"))
                continue;
            yield return await hwProcessor.Process(line);
        }
    }

    private async IAsyncEnumerable<ProcessingResult> ProcessPolygons(
        GraphHolder holder,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var areas = await osm.GetPolygons(settings.Bbox.AsRectangle());
        if (ct.IsCancellationRequested)
            yield break;
        var areaProcessor = new UnwalledAreaProcessor(osm, levelParser);
        foreach (var area in areas)
        {
            if (ct.IsCancellationRequested)
                yield break;
            if (area.Tags.ContainsKey("indoor") && area.Tags["indoor"] is "area" or "corridor")
                yield return await areaProcessor.Process(
                    area,
                    holder.GetNodesInArea(area.Geometry.EnvelopeInternal)
                );
            else if (area.Tags.GetValueOrDefault("highway") is "pedestrian")
                yield return await areaProcessor.Process(
                    area,
                    holder.GetNodesInArea(area.Geometry.EnvelopeInternal)
                );
        }
    }

    private static void SaveResult(GraphHolder holder, ProcessingResult line)
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
