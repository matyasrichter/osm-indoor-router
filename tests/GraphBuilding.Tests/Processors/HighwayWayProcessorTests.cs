namespace GraphBuilding.Tests.Processors;

using LineProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

public class HighwayWayProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public HighwayWayProcessorTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public static TheoryData<
        string,
        OsmLine,
        Dictionary<long, OsmPoint>,
        ProcessingResult
    > ProcessingTestData()
    {
        var gf = new GeometryFactory(new(), 4326);
        var data = new TheoryData<string, OsmLine, Dictionary<long, OsmPoint>, ProcessingResult>();

        var points = new List<Point>()
        {
            gf.CreatePoint(new Coordinate(1, 1)),
            gf.CreatePoint(new Coordinate(1, 2)),
            gf.CreatePoint(new Coordinate(2, 3))
        };

        data.Add(
            "simple three-node line",
            new(
                123456,
                new Dictionary<string, string>() { { "highway", "footway" }, { "foot", "yes" } },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new(),
            new(
                new() { new(points[0], 0, 1), new(points[1], 0, 2), new(points[2], 0, 3) },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    )
                }
            )
        );

        data.Add(
            "line with repeat_on",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "highway", "footway" },
                    { "foot", "yes" },
                    { "level", "0" },
                    { "repeat_on", "1-2" }
                },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new(),
            new(
                new()
                {
                    new(points[0], 0, 1),
                    new(points[1], 0, 2),
                    new(points[2], 0, 3),
                    new(points[0], 1, 1),
                    new(points[1], 1, 2),
                    new(points[2], 1, 3),
                    new(points[0], 2, 1),
                    new(points[1], 2, 2),
                    new(points[2], 2, 3)
                },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    ),
                    new(
                        6,
                        7,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        7,
                        8,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    )
                }
            )
        );

        data.Add(
            "line with repeat_on overlapping level",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "highway", "footway" },
                    { "foot", "yes" },
                    { "level", "0" },
                    { "repeat_on", "0-1" }
                },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new(),
            new(
                new()
                {
                    new(points[0], 0, 1),
                    new(points[1], 0, 2),
                    new(points[2], 0, 3),
                    new(points[0], 1, 1),
                    new(points[1], 1, 2),
                    new(points[2], 1, 3)
                },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    )
                }
            )
        );

        data.Add(
            "line with repeat_on but without level",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "highway", "footway" },
                    { "foot", "yes" },
                    { "repeat_on", "3;6" }
                },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new(),
            new(
                new()
                {
                    new(points[0], 3, 1),
                    new(points[1], 3, 2),
                    new(points[2], 3, 3),
                    new(points[0], 6, 1),
                    new(points[1], 6, 2),
                    new(points[2], 6, 3)
                },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    )
                }
            )
        );

        data.Add(
            "line with level",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "highway", "footway" },
                    { "foot", "yes" },
                    { "level", "3" }
                },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new(),
            new(
                new() { new(points[0], 3, 1), new(points[1], 3, 2), new(points[2], 3, 3) },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 0),
                        points[1].GetMetricDistance(points[2], 0),
                        123456
                    )
                }
            )
        );

        data.Add(
            "level connection",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "highway", "footway" },
                    { "foot", "yes" },
                    { "level", "3-4" }
                },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new()
            {
                {
                    1,
                    new(
                        1,
                        new Dictionary<string, string>() { { "door", "no" }, { "level", "3" } },
                        points[0]
                    )
                },
                {
                    3,
                    new(
                        3,
                        new Dictionary<string, string>() { { "door", "no" }, { "level", "4" } },
                        points[2]
                    )
                }
            },
            new(
                new() { new(points[0], 3, 1), new(points[1], 3, 2, true), new(points[2], 4, 3) },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 1),
                        points[1].GetMetricDistance(points[2], 1),
                        123456
                    )
                }
            )
        );

        data.Add(
            "level connection with repeat_on",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "highway", "footway" },
                    { "foot", "yes" },
                    { "level", "3-4" },
                    { "repeat_on", "4;6" }
                },
                new List<long>() { 1, 2, 3 },
                new(points.Select(x => x.Coordinate).ToArray())
            ),
            new()
            {
                {
                    1,
                    new(
                        1,
                        new Dictionary<string, string>()
                        {
                            { "door", "no" },
                            { "level", "3" },
                            { "repeat_on", "4;6" }
                        },
                        points[0]
                    )
                },
                {
                    3,
                    new(
                        3,
                        new Dictionary<string, string>()
                        {
                            { "door", "no" },
                            { "level", "4" },
                            { "repeat_on", "5,7" }
                        },
                        points[2]
                    )
                }
            },
            new(
                new()
                {
                    new(points[0], 3, 1),
                    new(points[1], 3, 2, true),
                    new(points[2], 4, 3),
                    new(points[0], 4, 1),
                    new(points[1], 4, 2, true),
                    new(points[2], 5, 3),
                    new(points[0], 6, 1),
                    new(points[1], 6, 2, true),
                    new(points[2], 7, 3)
                },
                new()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2], 1),
                        points[1].GetMetricDistance(points[2], 1),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2], 1),
                        points[1].GetMetricDistance(points[2], 1),
                        123456
                    ),
                    new(
                        6,
                        7,
                        points[0].GetMetricDistance(points[1], 0),
                        points[0].GetMetricDistance(points[1], 0),
                        123456
                    ),
                    new(
                        7,
                        8,
                        points[1].GetMetricDistance(points[2], 1),
                        points[1].GetMetricDistance(points[2], 1),
                        123456
                    )
                }
            )
        );

        return data;
    }

    [Theory]
    [MemberData(nameof(ProcessingTestData))]
    public async Task TestProcessing(
        string name,
        OsmLine line,
        Dictionary<long, OsmPoint> points,
        ProcessingResult expected
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
        var processor = new HighwayWayProcessor(osm.Object, new(Mock.Of<ILogger<LevelParser>>()));

        var result = await processor.Process(line);
        result.Should().BeEquivalentTo(expected);
    }
}
