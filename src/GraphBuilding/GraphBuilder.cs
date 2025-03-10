namespace GraphBuilding;

using ElementProcessors;
using Microsoft.Extensions.Logging;
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
        LogLoadedFromDB(points.Count, lines.Count, polygons.Count, multiPolygons.Count);

        foreach (var r in ProcessPoints(points))
            SaveResult(holder, r);

        foreach (var r in ProcessLines(points, lines))
            SaveResult(holder, r);

        foreach (var r in ProcessPolygons(points, polygons, multiPolygons, holder, ct))
            SaveResult(holder, r);

        wallGraphCutter.Run(holder, points, polygons, multiPolygons);

        return holder;
    }

    private IEnumerable<ProcessingResult> ProcessPoints(IReadOnlyDictionary<long, OsmPoint> points)
    {
        var elevatorProcessor = new ElevatorNodeProcessor(levelParser);
        var plainNodeProcessor = new PlainNodeProcessor(levelParser);
        var entranceNodeProcessor = new EntranceNodeProcessor(levelParser);
        return points.Values
            .AsParallel()
            .Select(point =>
            {
                LogProcessingItem(nameof(OsmPoint), point.NodeId);
                if (
                    point.Tags.GetValueOrDefault("elevator") is "yes"
                    || point.Tags.GetValueOrDefault("highway") is "elevator"
                )
                    return (point, elevatorProcessor.Process(point));
                else if (point.Tags.GetValueOrDefault("entrance") is not null)
                    return (point, entranceNodeProcessor.Process(point));
                else if (
                    HasAnyOfTags(point.Tags, "door", "level", "amenity", "shop", "name", "ref")
                )
                    return (point, plainNodeProcessor.Process(point));
                return (point, null);
            })
            .Where(x => x.Item2 is not null)
            .Select(x => GetEdgesWithFlags(x.Item2!, x.point.Tags));
    }

    private IEnumerable<ProcessingResult> ProcessLines(
        IReadOnlyDictionary<long, OsmPoint> points,
        IReadOnlyDictionary<long, OsmLine> lines
    )
    {
        var hwProcessor = new HighwayWayProcessor(levelParser);
        var wallProcessor = new WallProcessor(levelParser);
        return lines.Values
            .AsParallel()
            .Select(line =>
            {
                LogProcessingItem(nameof(OsmLine), line.WayId);
                if (line.Tags.ContainsKey("highway"))
                    return (line, hwProcessor.Process(line, points));
                else if (IsWalledElement(line.Tags))
                    return (line, wallProcessor.Process(line));
                return (line, null);
            })
            .Where(x => x.Item2 is not null)
            .Select(x => GetEdgesWithFlags(x.Item2!, x.line.Tags));
    }

    private IEnumerable<ProcessingResult> ProcessPolygons(
        IReadOnlyDictionary<long, OsmPoint> points,
        IReadOnlyDictionary<long, OsmPolygon> polygons,
        IReadOnlyDictionary<long, OsmMultiPolygon> multiPolygons,
        GraphHolder holder,
        CancellationToken ct
    )
    {
        var areaProcessor = new AreaProcessor(levelParser);
        var connectingAreaProcessor = new LevelConnectingAreaProcessor(levelParser);
        var wallProcessor = new WallProcessor(levelParser);
        return polygons.Values
            .AsParallel()
            .Select(polygon =>
            {
                LogProcessingItem(nameof(OsmPolygon), polygon.AreaId);
                if (IsLevelConnectingArea(polygon.Tags))
                    return (
                        polygon,
                        connectingAreaProcessor.Process(
                            PolygonToMultiPolygon(polygon),
                            points,
                            SourceType.Polygon
                        )
                    );
                else if (IsRoutableArea(polygon.Tags))
                    return (
                        polygon,
                        areaProcessor.Process(
                            PolygonToMultiPolygon(polygon),
                            holder.GetNodesInArea(polygon.Geometry.EnvelopeInternal),
                            points,
                            SourceType.Polygon
                        )
                    );
                else if (IsWalledElement(polygon.Tags))
                    return (polygon, wallProcessor.Process(polygon));
                return (polygon, null);
            })
            .Where(x => x.Item2 is not null)
            .Select(x => GetEdgesWithFlags(x.Item2!, x.polygon.Tags))
            .Concat(
                multiPolygons.Values
                    .AsParallel()
                    .Select(mp =>
                    {
                        LogProcessingItem(nameof(OsmMultiPolygon), mp.AreaId);
                        if (IsLevelConnectingArea(mp.Tags))
                            return (
                                mp,
                                connectingAreaProcessor.Process(mp, points, SourceType.Polygon)
                            );
                        else if (IsRoutableArea(mp.Tags))
                            return (
                                mp,
                                areaProcessor.Process(
                                    mp,
                                    holder.GetNodesInArea(mp.Geometry.EnvelopeInternal),
                                    points,
                                    SourceType.Multipolygon
                                )
                            );
                        else if (IsWalledElement(mp.Tags))
                            return (mp, wallProcessor.Process(mp));
                        return (mp, null);
                    })
                    .Where(x => x.Item2 is not null)
                    .Select(x => GetEdgesWithFlags(x.Item2!, x.mp.Tags))
            );
    }

    private static OsmMultiPolygon PolygonToMultiPolygon(OsmPolygon polygon) =>
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
        );

    private static bool IsWalledElement(IReadOnlyDictionary<string, string> tags) =>
        tags.GetValueOrDefault("walls") is not "no"
        && (
            tags.GetValueOrDefault("indoor") is "wall"
            || tags.GetValueOrDefault("barrier") is "wall" or "fence" or "guard_rail"
            || tags.GetValueOrDefault("building") is not null and not "roof"
            || tags.GetValueOrDefault("public_transport") is "platform"
        );

    private static bool IsLevelConnectingArea(IReadOnlyDictionary<string, string> tags) =>
        tags.GetValueOrDefault("indoor") is "area" or "room"
        && (
            tags.GetValueOrDefault("room") is "stairs" or "elevator"
            || tags.GetValueOrDefault("stairs") is "yes"
            || tags.GetValueOrDefault("highway") is "elevator"
        );

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
            node.Source is not null && !node.IsLevelConnection
                ? holder.GetNodeBySourceId(node.Source.Value.Id, node.Level)
                : null;

        if (existingNode is { Id: var existingId })
            return existingId;
        return holder.AddNode(node);
    }

    private static ProcessingResult GetEdgesWithFlags(
        ProcessingResult result,
        IReadOnlyDictionary<string, string> tags
    )
    {
        if (result.Edges.Count == 0)
            return result;
        var isElevator =
            tags.GetValueOrDefault("highway") is "elevator"
            || tags.GetValueOrDefault("room") is "elevator";
        var isEscalatorOrStairs =
            tags.GetValueOrDefault("stairs") is "yes"
            || tags.GetValueOrDefault("highway") is "steps"
            || tags.GetValueOrDefault("room") is "stairs"
            || tags.GetValueOrDefault("area") is "stairs";
        var isEscalator = isEscalatorOrStairs && tags.GetValueOrDefault("conveying") is "yes";
        var isStairs = isEscalatorOrStairs && !isEscalator;
        return result with
        {
            Edges = result.Edges
                .Select(
                    x =>
                        x with
                        {
                            IsStairs = isStairs,
                            IsEscalator = isEscalator,
                            IsElevator = isElevator
                        }
                )
                .ToList()
        };
    }

    private static bool HasAnyOfTags(
        IReadOnlyDictionary<string, string> tags,
        params string[] tagsOfInterest
    ) => tagsOfInterest.Any(x => tags.GetValueOrDefault(x) is not (null or ""));

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded data from database: {PointCount} points, {LineCount} lines,"
            + " {PolygonCount} polygons, {MPCount} multipolygons"
    )]
    private partial void LogLoadedFromDB(
        int pointCount,
        int lineCount,
        int polygonCount,
        int mpCount
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Type} ID {SourceId}")]
    private partial void LogProcessingItem(string type, long sourceId);
}
