namespace GraphBuilding.Tests;

using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Settings;

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
