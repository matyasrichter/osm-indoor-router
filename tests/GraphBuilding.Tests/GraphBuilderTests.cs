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
        var builder = new GraphBuilder(osm.Object, Settings, new(Mock.Of<ILogger<LevelParser>>()));
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

        var holder = new GraphHolder();
        await builder.BuildGraph(holder, CancellationToken.None);
        holder.Edges
            .Select(x => (x.FromId, x.ToId))
            .Should()
            .BeEquivalentTo(new List<(long, long)>() { (0, 1), (1, 2) });
    }

    [Fact]
    public async Task CanBuildTShapedGraph()
    {
        var osm = new Mock<IOsmPort>();
        var builder = new GraphBuilder(osm.Object, Settings, new(Mock.Of<ILogger<LevelParser>>()));
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
                        123456,
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

        var holder = new GraphHolder();
        await builder.BuildGraph(holder, CancellationToken.None);
        holder.Edges
            .Select(x => (x.FromId, x.ToId))
            .Should()
            .BeEquivalentTo(new List<(long, long)>() { (0, 1), (1, 2), (3, 4), (4, 1) });
    }

    [Fact]
    public async Task CanBuildLevelConnections()
    {
        var osm = new Mock<IOsmPort>();
        var builder = new GraphBuilder(osm.Object, Settings, new(Mock.Of<ILogger<LevelParser>>()));
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
        osm.Setup(x => x.GetPointByOsmId(It.IsAny<long>()))
            .Returns((long osmId) => Task.FromResult(osmPoints.GetValueOrDefault(osmId)));
        osm.Setup(x => x.GetPointsByOsmIds(It.IsAny<IEnumerable<long>>()))
            .Returns(
                (IEnumerable<long> osmIds) =>
                    Task.FromResult(osmIds.Select(osmPoints.GetValueOrDefault))
            );

        var holder = new GraphHolder();
        await builder.BuildGraph(holder, CancellationToken.None);

        var levelConnections = holder.Nodes.Where(x => x.SourceId == 4173362012).ToList();
        levelConnections.Should().HaveCount(3, "there are levels 0;1;2");

        var l1id = holder.Nodes.FindIndex(x => x is { SourceId: 4173362012, Level: 1 });

        holder.Edges
            .Where(x => x.ToId == l1id || x.FromId == l1id)
            .Should()
            .HaveCount(3, "there are two edges on level 2 and one on stairs");
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
