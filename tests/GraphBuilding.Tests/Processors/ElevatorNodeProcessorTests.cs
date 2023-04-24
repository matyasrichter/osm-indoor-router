namespace GraphBuilding.Tests.Processors;

using ElementProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Xunit.Abstractions;

public class ElevatorNodeProcessorTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public ElevatorNodeProcessorTests(ITestOutputHelper testOutputHelper) =>
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
            "two-level",
            new(
                123456,
                new Dictionary<string, string>() { { "elevator", "yes" }, { "level", "1;2" } },
                point
            ),
            new() { new(point, 1, 123456), new(point, 2, 123456) },
            new HashSet<decimal>[]
            {
                new() { 1, 2 }
            }
        );
        data.Add(
            "three-level",
            new(
                123456,
                new Dictionary<string, string>() { { "elevator", "yes" }, { "level", "4-6" } },
                point
            ),
            new() { new(point, 4, 123456), new(point, 5, 123456), new(point, 6, 123456) },
            new HashSet<decimal>[]
            {
                new() { 5, 4 },
                new() { 6, 5 }
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

        var processor = new ElevatorNodeProcessor(
            Mock.Of<IOsmPort>(),
            new(Mock.Of<ILogger<LevelParser>>())
        );

        var result = processor.Process(source);
        result.Nodes.Should().BeEquivalentTo(expectedNodes);
        result.Should().HaveEdgesBetweenLevels(expectedEdges);
    }
}
