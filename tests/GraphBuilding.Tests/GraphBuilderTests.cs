namespace GraphBuilding.Tests;

using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Settings;

#pragma warning disable CA1506
public class GraphBuilderTests
{
    private readonly GeometryFactory gf = new(new(), 4326);

    [Fact]
    public async Task CanBuildGraphWithSingleLine()
    {
        var osm = new Mock<IOsmPort>();
        var builder = new GraphBuilder(
            osm.Object,
            Settings,
            new(Mock.Of<ILogger<LevelParser>>()),
            new(Mock.Of<ILogger<WallGraphCutter>>()),
            Mock.Of<ILogger<GraphBuilder>>()
        );
        var points = new List<Point>()
        {
            gf.CreatePoint(new Coordinate(1, 1)),
            gf.CreatePoint(new Coordinate(1, 2)),
            gf.CreatePoint(new Coordinate(2, 3))
        };

        osm.Setup(x => x.GetLines(It.IsAny<Geometry>()))
            .ReturnsAsync(
                new[]
                {
                    new OsmLine(
                        123456,
                        new Dictionary<string, string>() { { "highway", "footway" } },
                        new List<long>() { 1, 2, 3 },
                        new(points.Select(x => x.Coordinate).ToArray())
                    )
                }
            );

        var holder = await builder.BuildGraph(CancellationToken.None);
        holder.Edges
            .Select(x => (x.FromId, x.ToId))
            .Should()
            .BeEquivalentTo(new List<(long, long)>() { (0, 1), (1, 2) });
    }

    [Fact]
    public async Task CanBuildTShapedGraph()
    {
        var osm = new Mock<IOsmPort>();
        var builder = new GraphBuilder(
            osm.Object,
            Settings,
            new(Mock.Of<ILogger<LevelParser>>()),
            new(Mock.Of<ILogger<WallGraphCutter>>()),
            Mock.Of<ILogger<GraphBuilder>>()
        );
        var points = new List<Point>()
        {
            gf.CreatePoint(new Coordinate(1, 1)),
            gf.CreatePoint(new Coordinate(1, 2)),
            gf.CreatePoint(new Coordinate(2, 3)),
            gf.CreatePoint(new Coordinate(2, 4)),
            gf.CreatePoint(new Coordinate(3, 4))
        };

        osm.Setup(x => x.GetLines(It.IsAny<Geometry>()))
            .ReturnsAsync(
                new[]
                {
                    new OsmLine(
                        123456,
                        new Dictionary<string, string>() { { "highway", "footway" } },
                        new List<long>() { 1, 2, 3 },
                        new(points.Take(3).Select(x => x.Coordinate).ToArray())
                    ),
                    new OsmLine(
                        123457,
                        new Dictionary<string, string>() { { "highway", "footway" } },
                        new List<long>() { 4, 5, 2 },
                        new(
                            new[] { points[3], points[4], points[1] }
                                .Select(x => x.Coordinate)
                                .ToArray()
                        )
                    )
                }
            );

        var holder = await builder.BuildGraph(CancellationToken.None);
        holder
            .Should()
            .HaveEdgesBetweenSourceIds((2, 0), new (long, decimal)[] { (1, 0), (3, 0), (5, 0) });
        holder.Should().HaveEdgesBetweenSourceIds((4, 0), new (long, decimal)[] { (5, 0) });
        holder.Edges.Should().HaveCount(4);
    }

    [Fact]
    public async Task CanBuildLevelConnections()
    {
        var osm = new Mock<IOsmPort>();
        var builder = new GraphBuilder(
            osm.Object,
            Settings,
            new(Mock.Of<ILogger<LevelParser>>()),
            new(Mock.Of<ILogger<WallGraphCutter>>()),
            Mock.Of<ILogger<GraphBuilder>>()
        );
        var points = new List<Point>()
        {
            gf.CreatePoint(new Coordinate(14.3888106, 50.1047052)), // 4173362015
            gf.CreatePoint(new Coordinate(14.3888857, 50.1046643)), // 4173362010
            gf.CreatePoint(new Coordinate(14.3888664, 50.1046497)), // 4173362008
            gf.CreatePoint(new Coordinate(14.3887914, 50.1046906)), // 4173362012
            gf.CreatePoint(new Coordinate(14.3887832, 50.1046844)), // 4926914177
            gf.CreatePoint(new Coordinate(14.3888169, 50.1047100)) // 4926914178
        };

        osm.Setup(x => x.GetLines(It.IsAny<Geometry>()))
            .ReturnsAsync(
                new[]
                {
                    new OsmLine(
                        416585499,
                        new Dictionary<string, string>()
                        {
                            { "highway", "steps" },
                            { "incline", "up" },
                            { "level", "0;1" },
                            { "repeat_on", "1" }
                        },
                        new List<long>() { 4173362015, 4173362010, 4173362008, 4173362012 },
                        new(points.Take(4).Select(x => x.Coordinate).ToArray())
                    ),
                    new OsmLine(
                        416585492,
                        new Dictionary<string, string>()
                        {
                            { "highway", "footway" },
                            { "level", "0" },
                            { "repeat_on", "1-2" }
                        },
                        new List<long>() { 4926914177, 4173362012, 4173362015, 4926914178 },
                        new(
                            new[] { points[4], points[3], points[0], points[5] }
                                .Select(x => x.Coordinate)
                                .ToArray()
                        )
                    )
                }
            );
        var osmPoints = new Dictionary<long, OsmPoint>()
        {
            {
                4173362012,
                new(
                    4173362012,
                    new Dictionary<string, string>()
                    {
                        { "door", "no" },
                        { "level", "1" },
                        { "repeat_on", "2" }
                    },
                    points[3]
                )
            },
            {
                4173362015,
                new(
                    4173362015,
                    new Dictionary<string, string>()
                    {
                        { "door", "no" },
                        { "level", "0" },
                        { "repeat_on", "1" }
                    },
                    points[0]
                )
            }
        };
        osm.Setup(x => x.GetPoints(It.IsAny<Geometry>())).ReturnsAsync(osmPoints.Values);

        var holder = await builder.BuildGraph(CancellationToken.None);

        var levelConnections = holder.Nodes.Where(x => x.SourceId == 4173362012).ToList();
        levelConnections.Should().HaveCount(3, "there are levels 0;1;2");

        holder
            .Should()
            .HaveEdgesBetweenSourceIds(
                (4173362012, 1),
                new[] { (4173362015, 1M), (4926914177, 1M), (4173362008, 0M) }
            );
        holder
            .Should()
            .HaveEdgesBetweenSourceIds(
                (4173362012, 2),
                new[] { (4173362015, 2M), (4926914177, 2M), (4173362008, 1M) }
            );
        holder
            .Should()
            .HaveEdgesBetweenSourceIds(
                (4173362015, 1),
                new[] { (4173362012, 1M), (4926914178, 1M), (4173362010, 1M) }
            );
    }

    /// <summary>
    /// Tests for a situation like this (x's are nodes):
    /// <code>
    /// x---------x
    /// |         |
    /// |      x--+--x
    /// x---------x
    /// </code>
    /// </summary>
    [Fact]
    public async Task CanHandleRoutableNodeInsidePolygon()
    {
        var osm = new Mock<IOsmPort>();
        var builder = new GraphBuilder(
            osm.Object,
            Settings,
            new(Mock.Of<ILogger<LevelParser>>()),
            new(Mock.Of<ILogger<WallGraphCutter>>()),
            Mock.Of<ILogger<GraphBuilder>>()
        );
        var points = new List<KeyValuePair<long, Point>>()
        {
            new(2911907727, gf.CreatePoint(new Coordinate(14.3896986, 50.1043596))),
            new(10779255894, gf.CreatePoint(new Coordinate(14.3895723, 50.1042648))),
            new(9566779413, gf.CreatePoint(new Coordinate(14.3897029, 50.1042739))),
            new(3776391910, gf.CreatePoint(new Coordinate(14.3897692, 50.1043239))),
            new(563250924, gf.CreatePoint(new Coordinate(14.3896805, 50.1043039))),
            new(563250921, gf.CreatePoint(new Coordinate(14.3896792, 50.1041176))),
        };

        osm.Setup(x => x.GetPolygons(It.IsAny<Geometry>()))
            .ReturnsAsync(
                new[]
                {
                    new OsmPolygon(
                        374272471,
                        new Dictionary<string, string>()
                        {
                            { "highway", "pedestrian" },
                            { "area", "yes" }
                        },
                        points.Take(4).Append(points[0]).Select(x => x.Key).ToList(),
                        new(
                            new(
                                points
                                    .Take(4)
                                    .Append(points[0])
                                    .Select(x => x.Value.Coordinate)
                                    .ToArray()
                            )
                        ),
                        new(
                            points
                                .Take(4)
                                .Append(points[0])
                                .Select(x => x.Value.Coordinate)
                                .ToArray()
                        )
                    ),
                }
            );
        osm.Setup(x => x.GetLines(It.IsAny<Geometry>()))
            .ReturnsAsync(
                new[]
                {
                    new OsmLine(
                        44328671,
                        new Dictionary<string, string>() { { "highway", "footway" } },
                        points.TakeLast(2).Select(x => x.Key).ToList(),
                        new(points.TakeLast(2).Select(x => x.Value.Coordinate).ToArray())
                    )
                }
            );

        var holder = await builder.BuildGraph(CancellationToken.None);

        holder.Nodes
            .Where(x => x.SourceId == 563250924)
            .Should()
            .HaveCount(1, "there are no levels");
        holder
            .Should()
            .HaveEdgesBetweenSourceIds(
                (563250924, 0m),
                new[]
                {
                    (563250921, 0M),
                    (2911907727, 0M),
                    (10779255894, 0M),
                    (9566779413, 0M),
                    (3776391910, 0M),
                }
            );
    }

    private static readonly AppSettings Settings =
        new()
        {
            Bbox = new()
            {
                NorthEast = new() { Latitude = 50.105917, Longitude = 14.39519 },
                SouthWest = new() { Latitude = 50.1007, Longitude = 14.386007 }
            },
            CorsAllowedOrigins = new[] { "localhost" }
        };
}
#pragma warning restore CA1506
