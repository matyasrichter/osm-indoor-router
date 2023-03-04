namespace Routing.Tests;

using System.Diagnostics.CodeAnalysis;
using Graph;
using Microsoft.Extensions.Logging;
using Moq;
using Ports;

public class GraphHolderTests
{
    [ExcludeFromCodeCoverage]
    private sealed record MockGraph(Guid Version) : IGraph
    {
        public IReadOnlyCollection<Node> Nodes { get; } = new List<Node>();

        public Node? GetNode(Guid id) => null;

        public IEnumerable<Edge> GetEdgesFromNode(Node node) => new List<Edge>();

        public IEnumerable<Edge> GetEdges() => new List<Edge>();
    }

    public static TheoryData<Guid?, IGraph?, Guid?, IGraph?, IGraph?> LoadingWithChangeTestData()
    {
        var testGraph1 = new MockGraph(Guid.NewGuid());
        var testGraph2 = new MockGraph(Guid.NewGuid());
        return new()
        {
            { null, null, testGraph1.Version, testGraph1, testGraph1 },
            { testGraph1.Version, testGraph1, testGraph2.Version, testGraph2, testGraph2 },
            { testGraph1.Version, testGraph1, null, null, testGraph1 }
        };
    }

    [Theory]
    [MemberData(nameof(LoadingWithChangeTestData))]
    public async Task TestLoadingWithChange(
        Guid? initialVersion,
        IGraph? initialGraph,
        Guid? returnedCurrentVersion,
        IGraph? returnedGraph,
        IGraph? finalGraph
    )
    {
        var loadingPortMock = new Mock<IGraphLoadingPort>();
        var holder = new GraphHolder(
            loadingPortMock.Object,
            new Mock<ILogger<GraphHolder>>().Object
        );

        // load initial graph
        loadingPortMock.Setup(x => x.GetCurrentGraphVersion()).ReturnsAsync(initialVersion);
        loadingPortMock.Setup(x => x.GetGraph(It.IsAny<Guid>())).ReturnsAsync(initialGraph);
        await holder.LoadGraph();

        // load new graph
        loadingPortMock
            .Setup(x => x.GetCurrentGraphVersion())
            .ReturnsAsync(returnedCurrentVersion);
        loadingPortMock.Setup(x => x.GetGraph(It.IsAny<Guid>())).ReturnsAsync(returnedGraph);
        await holder.LoadGraph();

        holder.Graph.Should().Be(finalGraph);
    }

    public static TheoryData<Guid?, IGraph?> LoadingWithoutChangeTestData()
    {
        var testGraph = new MockGraph(Guid.NewGuid());
        return new() { { null, null }, { testGraph.Version, testGraph } };
    }

    [Theory]
    [MemberData(nameof(LoadingWithoutChangeTestData))]
    public async Task TestLoadingWithoutChange(Guid? initialVersion, IGraph? initialGraph)
    {
        var loadingPortMock = new Mock<IGraphLoadingPort>();
        var holder = new GraphHolder(
            loadingPortMock.Object,
            new Mock<ILogger<GraphHolder>>().Object
        );

        // load initial graph
        loadingPortMock.Setup(x => x.GetCurrentGraphVersion()).ReturnsAsync(initialVersion);
        loadingPortMock.Setup(x => x.GetGraph(It.IsAny<Guid>())).ReturnsAsync(initialGraph);
        await holder.LoadGraph();

        // load new graph
        loadingPortMock.Invocations.Clear();
        await holder.LoadGraph();
        loadingPortMock.Verify(x => x.GetCurrentGraphVersion());
        loadingPortMock.VerifyNoOtherCalls();

        holder.Graph.Should().Be(initialGraph);
    }
}
