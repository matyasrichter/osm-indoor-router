namespace GraphBuilding.Tests;

using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;

public class MapProcessorTests
{
    private static readonly GeometryFactory Gf = new(new(), 4326);

    private sealed class DbStub : IGraphSavingPort
    {
        public List<InMemoryNode> Nodes { get; } = new();
        public List<InMemoryEdge> Edges { get; } = new();
        public Dictionary<long, bool> Versions { get; } = new();

        public Task<IEnumerable<long>> SaveNodes(IEnumerable<InMemoryNode> nodes, long version)
        {
            var fromId = Nodes.Count;
            Nodes.AddRange(nodes);
            return Task.FromResult(Enumerable.Range(fromId, Nodes.Count).Select(x => (long)x));
        }

        public Task<IEnumerable<long>> SaveEdges(IEnumerable<InMemoryEdge> edges, long version)
        {
            var fromId = Edges.Count;
            Edges.AddRange(edges);
            return Task.FromResult(Enumerable.Range(fromId, Edges.Count).Select(x => (long)x));
        }

#pragma warning disable CA1822,IDE0060
        public Task<int> RemoveNodesWithoutEdges(long version) => Task.FromResult(0);

        public Task<int> RemoveSmallComponents(decimal threshold, long version) =>
            Task.FromResult(0);
#pragma warning restore CA1822,IDE0060

        public Task<long> AddVersion()
        {
            Versions[Versions.Count] = false;
            return Task.FromResult((long)Versions.Count - 1);
        }

        public Task FinalizeVersion(long version)
        {
            Versions[version] = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CanProcessGraph()
    {
        var builder = new Mock<IGraphBuilder>();
        var db = new DbStub();
        var processor = new MapProcessor(db, Mock.Of<ILogger<MapProcessor>>(), builder.Object);
        var builderResult = new GraphHolder()
        {
            Nodes =
            {
                new(Gf.CreatePoint(new Coordinate(1, 0)), 0, new(SourceType.Point, 1)),
                new(Gf.CreatePoint(new Coordinate(2, 0)), 0, new(SourceType.Point, 2)),
                new(Gf.CreatePoint(new Coordinate(3, 0)), 0, new(SourceType.Point, 3)),
                new(Gf.CreatePoint(new Coordinate(2, 1)), 0, new(SourceType.Point, 4))
            },
            Edges =
            {
                new(
                    0,
                    1,
                    new(new Coordinate[] { new(1, 0), new(2, 0) }),
                    10,
                    10,
                    new(SourceType.Point, 1),
                    10
                ),
                new(
                    1,
                    2,
                    new(new Coordinate[] { new(2, 0), new(3, 0) }),
                    10,
                    10,
                    new(SourceType.Point, 1),
                    10
                ),
                new(
                    1,
                    3,
                    new(new Coordinate[] { new(2, 0), new(2, 1) }),
                    10,
                    10,
                    new(SourceType.Point, 2),
                    10
                ),
            }
        };
        builder.Setup(x => x.BuildGraph(It.IsAny<CancellationToken>())).ReturnsAsync(builderResult);

        await processor.Process(CancellationToken.None);
        db.Nodes.Should().HaveCount(4).And.BeEquivalentTo(builderResult.Nodes);
        db.Edges.Should().HaveCount(3);
        db.Edges.Where(x => x.FromId == 1 || x.ToId == 1).Should().HaveCount(3);
        db.Versions.Should().ContainValues(true);

        await processor.Process(CancellationToken.None);
        db.Nodes.Skip(4).Should().HaveCount(4).And.BeEquivalentTo(builderResult.Nodes);
        db.Edges.Skip(3).Should().HaveCount(3);
        db.Edges.Skip(3).Where(x => x.FromId == 5 || x.ToId == 5).Should().HaveCount(3);
        db.Versions.Should().ContainValues(true, true);
    }
}
