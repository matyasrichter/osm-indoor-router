namespace GraphBuilding.Tests;

public class GraphBuilderTests
{
    [Fact]
    public void CanCreateGraph()
    {
        var version = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var graph = new GraphBuilder(version)
            .AddNode(
                new()
                {
                    Id = id1,
                    Coordinates = new(0, 1),
                    Level = 0,
                    SourceId = null
                }
            )
            .AddNode(
                new()
                {
                    Id = id2,
                    Coordinates = new(1, 0),
                    Level = 0,
                    SourceId = null
                }
            )
            .AddEdge(
                new()
                {
                    Id = Guid.NewGuid(),
                    FromId = id1,
                    ToId = id2,
                    SourceId = null
                }
            )
            .Build();

        graph.Nodes.Select(x => x.Id).Should().BeEquivalentTo(new[] { id1, id2 });
        graph.GetEdgesFromNode(graph.GetNode(id1)!).Should().ContainSingle(x => x.ToId == id2);
        graph.Version.Should().Be(version);
    }

    [Fact]
    public void CanCheckForNodeExistence()
    {
        var id1 = Guid.NewGuid();

        var builder = new GraphBuilder(Guid.NewGuid());
        builder.HasNode(id1).Should().BeFalse();
        builder.GetNode(id1).Should().BeNull();

        builder.AddNode(
            new()
            {
                Id = id1,
                Coordinates = new(0, 1),
                Level = 0,
                SourceId = null
            }
        );
        builder.HasNode(id1).Should().BeTrue();
        builder.GetNode(id1).Should().NotBeNull();
    }
}
