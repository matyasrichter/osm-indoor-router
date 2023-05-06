namespace GraphBuilding.Tests.Processors;

using ElementProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

public class PlainNodeProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public PlainNodeProcessorTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public static TheoryData<string, OsmPoint, HashSet<InMemoryNode>> ProcessingTestData()
    {
        var gf = new GeometryFactory(new(), 4326);
        var data = new TheoryData<string, OsmPoint, HashSet<InMemoryNode>>();
        var point = gf.CreatePoint(new Coordinate(1, 1));
        data.Add(
            "no level tag",
            new(123456, new Dictionary<string, string>() { { "door", "no" } }, point),
            new() { new(point, 0, new(SourceType.Point, 123456)) }
        );
        data.Add(
            "two levels",
            new(
                123456,
                new Dictionary<string, string>() { { "door", "yes" }, { "level", "1;2" } },
                point
            ),
            new() { new(point, 1, new(SourceType.Point, 123456)) }
        );
        data.Add(
            "level and repeat_on",
            new(
                123456,
                new Dictionary<string, string>()
                {
                    { "door", "no" },
                    { "level", "4" },
                    { "repeat_on", "5-6" }
                },
                point
            ),
            new()
            {
                new(point, 4, new(SourceType.Point, 123456)),
                new(point, 5, new(SourceType.Point, 123456)),
                new(point, 6, new(SourceType.Point, 123456))
            }
        );

        return data;
    }

    [Theory]
    [MemberData(nameof(ProcessingTestData))]
    public void TestProcessing(string name, OsmPoint source, HashSet<InMemoryNode> expectedNodes)
    {
        testOutputHelper.WriteLine(name);

        var processor = new PlainNodeProcessor(new(Mock.Of<ILogger<LevelParser>>()));

        var result = processor.Process(source);
        result.Nodes.Should().BeEquivalentTo(expectedNodes);
        result.Edges.Should().BeEmpty();
        result.WallEdges.Should().BeEmpty();
    }
}
