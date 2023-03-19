namespace GraphBuilding.Tests;

public class GraphBuilderTests
{
    [Fact]
    public void CanHoldGraphElements()
    {
        var version = 0;
        var id1 = 1;
        var id2 = 2;

        var builder = new GraphBuilder(version)
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
                    SourceId = 123
                }
            )
            .AddEdge(
                new()
                {
                    Id = 1,
                    FromId = id1,
                    ToId = id2,
                    Cost = 1,
                    ReverseCost = 1,
                    SourceId = null
                }
            );

        builder.HasNode(id1).Should().BeTrue();
        builder.GetNode(id1).Should().NotBeNull();
        builder.HasNode(id2).Should().BeTrue();
        builder.GetNode(id2).Should().NotBeNull();
        builder.HasNode(100).Should().BeFalse();
        builder.GetNode(100).Should().BeNull();
        builder.Version.Should().Be(version);
        builder.GetNodeBySourceId(123).Should().Be(builder.GetNode(id2));
    }
}
