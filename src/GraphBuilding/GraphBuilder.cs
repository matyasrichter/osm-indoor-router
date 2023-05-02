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
        this.osm = osm;
        this.settings = settings;
        this.levelParser = levelParser;
        this.logger = logger;
        this.wallGraphCutter = wallGraphCutter;
    }

    public async Task<GraphHolder> BuildGraph(CancellationToken ct)
    {
        var holder = new GraphHolder();
        var points = (await osm.GetPoints(settings.Bbox.AsRectangle())).ToDictionary(
            x => x.NodeId,
            x => x
        );
        var lines = (await osm.GetLines(settings.Bbox.AsRectangle())).ToDictionary(
            x => x.WayId,
            x => x
        );
        var polygons = (await osm.GetPolygons(settings.Bbox.AsRectangle())).ToDictionary(
            x => x.AreaId,
            x => x
        );
        var multiPolygons = (await osm.GetMultiPolygons(settings.Bbox.AsRectangle())).ToDictionary(
            x => x.AreaId,
            x => x
        );

        foreach (var r in ProcessPoints(points, ct))
            SaveResult(holder, r);

        foreach (var r in ProcessLines(points, lines, ct))
            SaveResult(holder, r);

        foreach (var r in ProcessPolygons(points, polygons, multiPolygons, holder, ct))
            SaveResult(holder, r);

        wallGraphCutter.Run(holder, points);

        return holder;
    }

    private IEnumerable<ProcessingResult> ProcessPoints(
        IReadOnlyDictionary<long, OsmPoint> points,
        CancellationToken ct
    )
    {
        if (ct.IsCancellationRequested)
            yield break;
        var elevatorProcessor = new ElevatorNodeProcessor(levelParser);
        foreach (var point in points.Values)
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

    private IEnumerable<ProcessingResult> ProcessLines(
        IReadOnlyDictionary<long, OsmPoint> points,
        IReadOnlyDictionary<long, OsmLine> lines,
        CancellationToken ct
    )
    {
        if (ct.IsCancellationRequested)
            yield break;
        var hwProcessor = new HighwayWayProcessor(levelParser);
        var wallProcessor = new WallProcessor(levelParser);
        foreach (var line in lines.Values)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmLine), line.WayId);
            if (line.Tags.ContainsKey("highway"))
                yield return hwProcessor.Process(line, points);
            else if (
                line.Tags.GetValueOrDefault("indoor") is "wall"
                || line.Tags.GetValueOrDefault("barrier") is "wall" or "fence"
                || line.Tags.GetValueOrDefault("building") is not null and not "roof"
            )
                yield return wallProcessor.Process(line);
        }
    }

    private IEnumerable<ProcessingResult> ProcessPolygons(
        IReadOnlyDictionary<long, OsmPoint> points,
        IReadOnlyDictionary<long, OsmPolygon> polygons,
        IReadOnlyDictionary<long, OsmMultiPolygon> multiPolygons,
        GraphHolder holder,
        CancellationToken ct
    )
    {
        if (ct.IsCancellationRequested)
            yield break;
        var areaProcessor = new AreaProcessor(levelParser);
        foreach (var polygon in polygons.Values)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmPolygon), polygon.AreaId);
            if (IsRoutableArea(polygon.Tags))
                yield return areaProcessor.Process(
                    // convert to a multipolygon with a single polygon
                    new(
                        polygon.AreaId,
                        polygon.Tags,
                        new(new[] { polygon.Geometry }),
                        new[]
                        {
                            new OsmLine(
                                polygon.AreaId,
                                polygon.Tags,
                                polygon.Nodes,
                                polygon.GeometryAsLinestring
                            )
                        }
                    ),
                    holder.GetNodesInArea(polygon.Geometry.EnvelopeInternal),
                    points
                );
        }

        if (ct.IsCancellationRequested)
            yield break;
        if (ct.IsCancellationRequested)
            yield break;
        foreach (var mp in multiPolygons.Values)
        {
            if (ct.IsCancellationRequested)
                yield break;
            LogProcessingItem(nameof(OsmMultiPolygon), mp.AreaId);
            if (IsRoutableArea(mp.Tags))
                yield return areaProcessor.Process(
                    mp,
                    holder.GetNodesInArea(mp.Geometry.EnvelopeInternal),
                    points
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
