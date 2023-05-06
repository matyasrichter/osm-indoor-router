namespace GraphBuilding.Tests.Processors;

using ElementProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

public class EntranceNodeProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public EntranceNodeProcessorTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public static TheoryData<
        string,
        OsmPoint,
        HashSet<InMemoryNode>,
        HashSet<decimal>[]
    > ProcessingTestData()
    {
        var gf = new GeometryFactory(new(), 4326);
        var data = new TheoryData<string, OsmPoint, HashSet<InMemoryNode>, HashSet<decimal>[]>();
        var point = gf.CreatePoint(new Coordinate(1, 1));
        data.Add(
            "ground level",
            new(123456, new Dictionary<string, string>() { { "entrance", "main" } }, point),
            new() { new(point, 0, new(SourceType.Point, 123456)) },
            Array.Empty<HashSet<decimal>>()
        );
        data.Add(
            "repeat_on",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "entrance", "yes" },
                    { "level", "-1" },
                    { "repeat_on", "1" }
                },
                point
            ),
            new()
            {
                new(point, -1, new(SourceType.Point, 123456)),
                new(point, 0, new(SourceType.Point, 123456)),
                new(point, 1, new(SourceType.Point, 123456))
            },
            new HashSet<decimal>[]
            {
                new() { 0, -1 },
                new() { 0, 1 }
            }
        );
        data.Add(
            "repeat_on with ground level",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "entrance", "yes" },
                    { "level", "-1" },
                    { "repeat_on", "0" }
                },
                point
            ),
            new()
            {
                new(point, -1, new(SourceType.Point, 123456)),
                new(point, 0, new(SourceType.Point, 123456))
            },
            new HashSet<decimal>[]
            {
                new() { 0, -1 }
            }
        );

        return data;
    }

    [Theory]
    [MemberData(nameof(ProcessingTestData))]
    public void TestProcessing(
        string name,
        OsmPoint source,
        HashSet<InMemoryNode> expectedNodes,
        HashSet<decimal>[] expectedEdges
    )
    {
        testOutputHelper.WriteLine(name);

        var processor = new EntranceNodeProcessor(new(Mock.Of<ILogger<LevelParser>>()));

        var result = processor.Process(source);
        result.Nodes.Should().BeEquivalentTo(expectedNodes);
        result.Should().HaveEdgesBetweenLevels(expectedEdges);
    }
}
