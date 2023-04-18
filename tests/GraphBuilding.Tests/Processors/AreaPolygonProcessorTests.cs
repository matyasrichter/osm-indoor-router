namespace GraphBuilding.Tests.Processors;

using LineProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

public class AreaPolygonProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;

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
        var gf = new GeometryFactory(new(), 4326);
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
            gf.CreatePoint(new Coordinate(1, 1)),
            gf.CreatePoint(new Coordinate(1, 2)),
            gf.CreatePoint(new Coordinate(2, 2)),
            gf.CreatePoint(new Coordinate(2, 1))
        };

        data.Add(
            "simple rectangle",
            new(
                123456,
                new Dictionary<string, string>() { { "highway", "pedestrian" }, { "area", "yes" } },
                new List<long>() { 1, 2, 3, 4, 1 },
                gf.CreatePolygon(points.Append(points[0]).Select(x => x.Coordinate).ToArray()),
                gf.CreateLineString(points.Append(points[0]).Select(x => x.Coordinate).ToArray())
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
            gf.CreatePoint(new Coordinate(1, 1)),
            gf.CreatePoint(new Coordinate(1, 2)),
            gf.CreatePoint(new Coordinate(1, 3)),
            gf.CreatePoint(new Coordinate(2, 1)),
            gf.CreatePoint(new Coordinate(1.5, 1.5))
        };
        data.Add(
            "l-shaped",
            new(
                123456,
                new Dictionary<string, string>() { { "highway", "pedestrian" }, { "area", "yes" } },
                new List<long>() { 1, 2, 3, 4, 5, 1 },
                gf.CreatePolygon(
                    new[] { points[0], points[1], points[2], points[3], points[4], points[0] }
                        .Select(x => x.Coordinate)
                        .ToArray()
                ),
                gf.CreateLineString(
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

        var result = await processor.Process(polygon);
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
}
