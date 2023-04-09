namespace GraphBuilding.Tests;

public class GraphHolderTests
{
    [Fact]
    public void CanHoldGraphElements()
    {
        var builder = new GraphHolder();
        var id1 = builder.AddNode(new(new(0, 1), 0, null));
        var id2 = builder.AddNode(new(new(1, 0), 0, 123));
        var edge = new InMemoryEdge(id1, id2, 1, 1, null);
        builder.AddEdge(edge);

        builder.GetNode(id1).Should().NotBeNull();
        builder.GetNode(id2).Should().NotBeNull();
        builder.GetNode(100).Should().BeNull();
        builder.GetNodeBySourceId(123, 0).Should().Be((id2, builder.GetNode(id2)));
        builder.Edges.Should().ContainSingle(x => x == edge);
    }
}
