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
        var processor = new AreaProcessor(new(Mock.Of<ILogger<LevelParser>>()));

        var mp = new OsmMultiPolygon(
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
        var result = processor.Process(mp, Enumerable.Empty<InMemoryNode>(), points);
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
    public void CanHandleNodeInsideEnvelope()
    {
        var points = new List<KeyValuePair<long, Point>>()
        {
            new(2911907727, Gf.CreatePoint(new Coordinate(14.3896986, 50.1043596))),
            new(10779255894, Gf.CreatePoint(new Coordinate(14.3895723, 50.1042648))),
            new(9566779413, Gf.CreatePoint(new Coordinate(14.3897029, 50.1042739))),
            new(3776391910, Gf.CreatePoint(new Coordinate(14.3897692, 50.1043239))),
            new(563250924, Gf.CreatePoint(new Coordinate(14.3896805, 50.1043039))),
        };

        var polygon = new OsmPolygon(
            374272471,
            new Dictionary<string, string>() { { "highway", "pedestrian" }, { "area", "yes" } },
            points.Take(4).Append(points[0]).Select(x => x.Key).ToList(),
            new(new(points.Take(4).Append(points[0]).Select(x => x.Value.Coordinate).ToArray())),
            new(points.Take(4).Append(points[0]).Select(x => x.Value.Coordinate).ToArray())
        );

        var processor = new AreaProcessor(new(Mock.Of<ILogger<LevelParser>>()));
        var mp = new OsmMultiPolygon(
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

        var existingNode = new InMemoryNode(points.Last().Value, 0, 563250924);
        var result = processor.Process(
            mp,
            new[] { existingNode },
            new Dictionary<long, OsmPoint>()
        );
        result
            .Should()
            .HaveEdgesBetweenSourceIds(
                563250924,
                new[] { 2911907727, 10779255894, 9566779413, 3776391910, }
            );
    }

    [Fact]
    public void CanHandlePolygonWithHole()
    {
        var innerPoints = new (long, Coordinate)[]
        {
            (1, new(1, 1)),
            (2, new(3, 2)),
            (3, new(4, 3)),
            (4, new(2, 2)),
            (1, new(1, 1)),
        };
        var outerPoints = new (long, Coordinate)[]
        {
            (5, new(0, 0)),
            (6, new(4, 0)),
            (7, new(5, 4)),
            (8, new(1, 4)),
            (5, new(0, 0)),
        };
        var multipolygon = Gf.CreateMultiPolygon(
            new[]
            {
                Gf.CreatePolygon(
                    new LinearRing(outerPoints.Select(x => x.Item2).ToArray()),
                    new LinearRing[] { new(innerPoints.Select(x => x.Item2).ToArray()) }
                )
            }
        );

        var osmMultiPolygon = new OsmMultiPolygon(
            123456,
            new Dictionary<string, string>() { { "indoor", "area" }, { "level", "2" } },
            multipolygon,
            new OsmLine[]
            {
                new(
                    123,
                    new Dictionary<string, string>(),
                    innerPoints.Select(x => x.Item1).ToList(),
                    new(innerPoints.Select(x => x.Item2).ToArray())
                ),
                new(
                    124,
                    new Dictionary<string, string>(),
                    outerPoints.Select(x => x.Item1).ToList(),
                    new(outerPoints.Select(x => x.Item2).ToArray())
                )
            }
        );
        var processor = new AreaProcessor(new(Mock.Of<ILogger<LevelParser>>()));

        var result = processor.Process(
            osmMultiPolygon,
            Array.Empty<InMemoryNode>(),
            new Dictionary<long, OsmPoint>()
        );
        result.Nodes
            .Select(x => x.SourceId)
            .Should()
            .BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        result.Nodes.Select(x => x.Level).Distinct().Should().BeEquivalentTo(new[] { 2 });
        result
            .Should()
            .HaveEdgesBetweenSourceIds(1, new long[] { 2, 4, 5, 6, 8 })
            .And.HaveEdgesBetweenSourceIds(7, new long[] { 6, 8, 2, 3, 4 });
        result.Edges.Should().HaveCount(20);
    }
}
# pragma warning restore CA1506
