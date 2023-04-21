namespace GraphBuilding.Tests.Processors;

using ElementProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

# pragma warning disable CA1506
public class AreaPolygonProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;
    private static readonly GeometryFactory Gf = new(new(), 4326);

    public AreaPolygonProcessorTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public static TheoryData<
        string,
        OsmPolygon,
        Dictionary<long, OsmPoint>,
        HashSet<InMemoryNode>,
        HashSet<(Point, Point)>
    > ProcessingTestData()
    {
        var data =
            new TheoryData<
                string,
                OsmPolygon,
                Dictionary<long, OsmPoint>,
                HashSet<InMemoryNode>,
                HashSet<(Point, Point)>
            >();

        var points = new List<Point>()
        {
            Gf.CreatePoint(new Coordinate(1, 1)),
            Gf.CreatePoint(new Coordinate(1, 2)),
            Gf.CreatePoint(new Coordinate(2, 2)),
            Gf.CreatePoint(new Coordinate(2, 1))
        };

        data.Add(
            "simple rectangle",
            new(
                123456,
                new Dictionary<string, string>() { { "highway", "pedestrian" }, { "area", "yes" } },
                new List<long>() { 1, 2, 3, 4, 1 },
                Gf.CreatePolygon(points.Append(points[0]).Select(x => x.Coordinate).ToArray()),
                Gf.CreateLineString(points.Append(points[0]).Select(x => x.Coordinate).ToArray())
            ),
            new(),
            new()
            {
                new(points[0], 0, 1),
                new(points[1], 0, 2),
                new(points[2], 0, 3),
                new(points[3], 0, 4)
            },
            new()
            {
                (points[0], points[1]),
                (points[1], points[2]),
                (points[2], points[3]),
                (points[3], points[0]),
                (points[0], points[2]),
                (points[1], points[3])
            }
        );

        points = new()
        {
            Gf.CreatePoint(new Coordinate(1, 1)),
            Gf.CreatePoint(new Coordinate(1, 2)),
            Gf.CreatePoint(new Coordinate(1, 3)),
            Gf.CreatePoint(new Coordinate(2, 1)),
            Gf.CreatePoint(new Coordinate(1.5, 1.5))
        };
        data.Add(
            "l-shaped",
            new(
                123456,
                new Dictionary<string, string>() { { "highway", "pedestrian" }, { "area", "yes" } },
                new List<long>() { 1, 2, 3, 4, 5, 1 },
                Gf.CreatePolygon(
                    new[] { points[0], points[1], points[2], points[3], points[4], points[0] }
                        .Select(x => x.Coordinate)
                        .ToArray()
                ),
                Gf.CreateLineString(
                    new[] { points[0], points[1], points[2], points[3], points[4], points[0] }
                        .Select(x => x.Coordinate)
                        .ToArray()
                )
            ),
            new(),
            new()
            {
                new(points[0], 0, 1),
                new(points[1], 0, 2),
                new(points[2], 0, 3),
                new(points[3], 0, 4),
                new(points[4], 0, 5)
            },
            new()
            {
                (points[0], points[1]),
                (points[0], points[2]),
                (points[0], points[4]),
                (points[1], points[2]),
                (points[1], points[3]),
                (points[1], points[4]),
                (points[2], points[3]),
                (points[2], points[4]),
                (points[3], points[4])
            }
        );
        return data;
    }

    [Theory]
    [MemberData(nameof(ProcessingTestData))]
    public async Task TestProcessing(
        string name,
        OsmPolygon polygon,
        Dictionary<long, OsmPoint> points,
        HashSet<InMemoryNode> expectedNodes,
        HashSet<(Point FromId, Point ToId)> expectedEdges
    )
    {
        testOutputHelper.WriteLine(name);
        var osm = new Mock<IOsmPort>();
        osm.Setup(x => x.GetPointByOsmId(It.IsAny<long>()))
            .Returns((long osmId) => Task.FromResult(points.GetValueOrDefault(osmId)));
        osm.Setup(x => x.GetPointsByOsmIds(It.IsAny<IEnumerable<long>>()))
            .Returns(
                (IEnumerable<long> osmIds) =>
                    Task.FromResult(osmIds.Select(points.GetValueOrDefault))
            );
        var processor = new UnwalledAreaProcessor(osm.Object, new(Mock.Of<ILogger<LevelParser>>()));

        var result = await processor.Process(polygon, Enumerable.Empty<InMemoryNode>());
        result.Nodes.Should().BeEquivalentTo(expectedNodes);
        var edgePairs = result.Edges
            .Select(
                x =>
                    (result.Nodes[(int)x.FromId].Coordinates, result.Nodes[(int)x.ToId].Coordinates)
            )
            .ToHashSet();

        result.Edges.Should().HaveCount(expectedEdges.Count);
        foreach (var (expFrom, expTo) in expectedEdges)
            (edgePairs.Contains((expFrom, expTo)) || edgePairs.Contains((expTo, expFrom)))
                .Should()
                .BeTrue("result should have edge {0} or {1}", (expFrom, expTo), (expTo, expFrom));
    }

    [Fact]
    public async Task CanHandleNodeInsideEnvelope()
    {
        var points = new List<KeyValuePair<long, Point>>()
        {
            new(2911907727, Gf.CreatePoint(new Coordinate(14.3896986, 50.1043596))),
            new(10779255894, Gf.CreatePoint(new Coordinate(14.3895723, 50.1042648))),
            new(9566779413, Gf.CreatePoint(new Coordinate(14.3897029, 50.1042739))),
            new(3776391910, Gf.CreatePoint(new Coordinate(14.3897692, 50.1043239))),
            new(563250924, Gf.CreatePoint(new Coordinate(14.3896805, 50.1043039))),
        };
        var osm = new Mock<IOsmPort>();
        osm.Setup(x => x.GetPointByOsmId(It.IsAny<long>())).ReturnsAsync((OsmPoint?)null);
        osm.Setup(x => x.GetPointsByOsmIds(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync((IEnumerable<long> osmIds) => osmIds.Select<long, OsmPoint?>(_ => null));

        var polygon = new OsmPolygon(
            374272471,
            new Dictionary<string, string>() { { "highway", "pedestrian" }, { "area", "yes" } },
            points.Take(4).Append(points[0]).Select(x => x.Key).ToList(),
            new(new(points.Take(4).Append(points[0]).Select(x => x.Value.Coordinate).ToArray())),
            new(points.Take(4).Append(points[0]).Select(x => x.Value.Coordinate).ToArray())
        );
        var processor = new UnwalledAreaProcessor(osm.Object, new(Mock.Of<ILogger<LevelParser>>()));

        var existingNode = new InMemoryNode(points.Last().Value, 0, 563250924);
        var result = await processor.Process(polygon, new[] { existingNode });
        result.Edges
            .Join(
                result.Nodes.Select((x, i) => (x, i)),
                x => x.FromId,
                x => x.i,
                (x, y) => (Edge: x, FromSId: y.x.SourceId)
            )
            .Join(
                result.Nodes.Select((x, i) => (x, i)),
                x => x.Edge.ToId,
                x => x.i,
                (x, y) => (x.Edge, x.FromSId, ToSId: y.x.SourceId)
            )
            .Where(x => x.FromSId == 563250924 || x.ToSId == 563250924)
            .Select(x => new HashSet<long?>() { x.FromSId, x.ToSId })
            .Should()
            .BeEquivalentTo(
                new HashSet<long>[]
                {
                    new() { 563250924, 2911907727 },
                    new() { 563250924, 10779255894 },
                    new() { 563250924, 9566779413 },
                    new() { 563250924, 3776391910 },
                }
            );
    }
}
# pragma warning restore CA1506
