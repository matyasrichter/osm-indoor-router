namespace GraphBuilding;

using System.Runtime.CompilerServices;
using ElementProcessors;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Parsers;
using Ports;
using Settings;

public interface IGraphBuilder
{
    Task<GraphHolder> BuildGraph(CancellationToken ct);
}

public partial class GraphBuilder : IGraphBuilder
{
    private readonly IOsmPort osm;
    private readonly AppSettings settings;
    private readonly LevelParser levelParser;
    private readonly WallGraphCutter wallGraphCutter;
    private readonly ILogger<GraphBuilder> logger;

    public GraphBuilder(
        IOsmPort osm,
        AppSettings settings,
        LevelParser levelParser,
        WallGraphCutter wallGraphCutter,
        ILogger<GraphBuilder> logger
    )
    {
        this.osm = new CachingOsmPortWrapper(osm);
        this.settings = settings;
        this.levelParser = levelParser;
        this.logger = logger;
        this.wallGraphCutter = wallGraphCutter;
    }

    public async Task<GraphHolder> BuildGraph(CancellationToken ct)
    {
        var holder = new GraphHolder();
        var walls = new Dictionary<decimal, List<LineString>>();
        var points = (await osm.GetPoints(settings.Bbox.AsRectangle())).ToList();

        foreach (var r in ProcessPoints(points, ct))
            SaveResult(holder, r);

        await foreach (var r in ProcessLines(ct))
            SaveResult(holder, r);

        await foreach (var r in ProcessPolygons(holder, ct))
            SaveResult(holder, r);

        wallGraphCutter.Run(holder, points.ToDictionary(x => x.NodeId, x => x));

        return holder;
    }

    private IEnumerable<ProcessingResult> ProcessPoints(
        IEnumerable<OsmPoint> points,
        CancellationToken ct
    )
    {
        if (ct.IsCancellationRequested)
            yield break;
        var elevatorProcessor = new ElevatorNodeProcessor(osm, levelParser);
        foreach (var point in points)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmPoint), point.NodeId);
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
        var wallProcessor = new WallProcessor(osm, levelParser);
        foreach (var line in lines)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmLine), line.WayId);
            if (line.Tags.ContainsKey("highway"))
                yield return await hwProcessor.Process(line);
            else if (
                line.Tags.GetValueOrDefault("indoor") is "wall"
                || line.Tags.GetValueOrDefault("barrier") is "wall" or "fence"
                || line.Tags.GetValueOrDefault("building") is not null and not "roof"
            )
                yield return wallProcessor.Process(line);
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
        var areaProcessor = new AreaProcessor(osm, levelParser);
        foreach (var area in areas)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmPolygon), area.AreaId);
            if (IsRoutableArea(area.Tags))
                yield return await areaProcessor.Process(
                    // convert to a multipolygon with a single polygon
                    new(
                        area.AreaId,
                        area.Tags,
                        new(new[] { area.Geometry }),
                        new[]
                        {
                            new OsmLine(
                                area.AreaId,
                                area.Tags,
                                area.Nodes,
                                area.GeometryAsLinestring
                            )
                        }
                    ),
                    holder.GetNodesInArea(area.Geometry.EnvelopeInternal)
                );
        }

        if (ct.IsCancellationRequested)
            yield break;
        var multiPolygons = await osm.GetMultiPolygons(settings.Bbox.AsRectangle());
        if (ct.IsCancellationRequested)
            yield break;
        foreach (var mp in multiPolygons)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmMultiPolygon), mp.AreaId);
            if (IsRoutableArea(mp.Tags))
                yield return await areaProcessor.Process(
                    mp,
                    holder.GetNodesInArea(mp.Geometry.EnvelopeInternal)
                );
        }
    }

    private static bool IsRoutableArea(IReadOnlyDictionary<string, string> tags) =>
        tags.GetValueOrDefault("highway") is "pedestrian"
        || (tags.ContainsKey("indoor") && tags["indoor"] is "area" or "corridor" or "room");

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

        foreach (var (level, edge) in line.WallEdges)
            holder.AddWallEdge((nodeIdMap[edge.FromId], nodeIdMap[edge.ToId]), level);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Type} ID {SourceId}")]
    private partial void LogProcessingItem(string type, long sourceId);
}
