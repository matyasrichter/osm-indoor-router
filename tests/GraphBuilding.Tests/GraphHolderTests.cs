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

    [Fact]
    public void DoesNotInsertTwice()
    {
        var builder = new GraphHolder();
        var node = new InMemoryNode(new(0, 1), 0, 123);
        var id1 = builder.AddNode(node);
        var id2 = builder.AddNode(node);
        id1.Should().Be(id2);
        builder.Nodes.Should().ContainSingle(x => x == node);
    }

    [Fact]
    public void CanHoldGraphElementsOnMultipleLevels()
    {
        var builder = new GraphHolder();
        var nodes = new InMemoryNode[]
        {
            // level 1
            new(new(0, 1), 1, 123),
            new(new(1, 0), 1, 124),
            // level 2
            new(new(0, 1), 2, 123),
            new(new(1, 0), 2, 124),
        };
        var nodeIds = nodes.Select(builder.AddNode).ToList();
        var edge1 = new InMemoryEdge(nodeIds[0], nodeIds[1], 1, 1, 987);
        builder.AddEdge(edge1);
        var edge2 = new InMemoryEdge(nodeIds[2], nodeIds[3], 1, 1, 987);
        builder.AddEdge(edge2);

        foreach (var (node, id) in nodes.Zip(nodeIds))
            builder.GetNode(id).Should().Be(node, $"we got id {id} when we inserted it");
        foreach (var node in nodes)
        {
            var n = builder.GetNodeBySourceId((long)node.SourceId!, node.Level);
            n.Should().NotBeNull();
            n?.Node.SourceId.Should().Be(node.SourceId);
            n?.Node.Level.Should().Be(node.Level);
        }

        builder.Edges.Should().BeEquivalentTo(new[] { edge1, edge2 });
    }
}
