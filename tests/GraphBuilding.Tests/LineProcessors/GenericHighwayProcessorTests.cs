namespace GraphBuilding.Tests.LineProcessors;

using GraphBuilding.LineProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

public class GenericHighwayProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public GenericHighwayProcessorTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public static TheoryData<string, OsmLine, ProcessingResult> ProcessingTestData()
    {
        var gf = new GeometryFactory(new(), 4326);
        var data = new TheoryData<string, OsmLine, ProcessingResult>();

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
            new(
                new List<InMemoryNode>()
                {
                    new(points[0], 0, 1),
                    new(points[1], 0, 2),
                    new(points[2], 0, 3)
                },
                new List<InMemoryEdge>()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
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
            new(
                new List<InMemoryNode>()
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
                new List<InMemoryEdge>()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
                        123456
                    ),
                    new(
                        6,
                        7,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        7,
                        8,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
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
            new(
                new List<InMemoryNode>()
                {
                    new(points[0], 0, 1),
                    new(points[1], 0, 2),
                    new(points[2], 0, 3),
                    new(points[0], 1, 1),
                    new(points[1], 1, 2),
                    new(points[2], 1, 3)
                },
                new List<InMemoryEdge>()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
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
            new(
                new List<InMemoryNode>()
                {
                    new(points[0], 3, 1),
                    new(points[1], 3, 2),
                    new(points[2], 3, 3),
                    new(points[0], 6, 1),
                    new(points[1], 6, 2),
                    new(points[2], 6, 3)
                },
                new List<InMemoryEdge>()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
                        123456
                    ),
                    new(
                        3,
                        4,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        4,
                        5,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
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
            new(
                new List<InMemoryNode>()
                {
                    new(points[0], 3, 1),
                    new(points[1], 3, 2),
                    new(points[2], 3, 3)
                },
                new List<InMemoryEdge>()
                {
                    new(
                        0,
                        1,
                        points[0].GetMetricDistance(points[1]),
                        points[0].GetMetricDistance(points[1]),
                        123456
                    ),
                    new(
                        1,
                        2,
                        points[1].GetMetricDistance(points[2]),
                        points[1].GetMetricDistance(points[2]),
                        123456
                    )
                }
            )
        );

        return data;
    }

    [Theory]
    [MemberData(nameof(ProcessingTestData))]
    public async Task TestProcessing(string name, OsmLine line, ProcessingResult expected)
    {
        testOutputHelper.WriteLine(name);
        var osm = new Mock<IOsmPort>();
        var processor = new GenericHighwayProcessor(
            osm.Object,
            new(Mock.Of<ILogger<LevelParser>>())
        );

        var result = await processor.Process(line);
        result.Should().BeEquivalentTo(expected);
    }
}
