namespace Graph.Tests;

public class GraphTests
{
    [Fact]
    public void CanCreateEmptyGraph()
    {
        var graph = new Graph(new(), new(), Guid.NewGuid());
        graph.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void CanCreateGraphWithoutEdges()
    {
        var node1 = new Node() { Id = Guid.NewGuid(), Coordinates = new(0, 1), Level = 0, SourceId = 12345 };
        var node2 = new Node() { Id = Guid.NewGuid(), Coordinates = new(1, 2), Level = 0, SourceId = 12346 };
        var graph = new Graph(
            new() { { node1.Id, node1 }, { node2.Id, node2 } },
            new(), Guid.NewGuid()
        );

        graph.Nodes.Should().BeEquivalentTo(new List<Node>() { node1, node2 });
        graph.GetNode(node1.Id).Should().Be(node1);
        graph.GetNode(node2.Id).Should().Be(node2);
        graph.GetEdgesFromNode(node1).Should().BeEmpty();
        graph.GetEdgesFromNode(node2).Should().BeEmpty();
    }

    [Fact]
    public void CanCreateGraphWithEdges()
    {
        var node1 = new Node() { Id = Guid.NewGuid(), Coordinates = new(0, 1), Level = 0, SourceId = 12345 };
        var node2 = new Node() { Id = Guid.NewGuid(), Coordinates = new(1, 2), Level = 0, SourceId = 12346 };
        var edge1 = new Edge() { Id = Guid.NewGuid(), FromId = node1.Id, ToId = node2.Id, SourceId = 12345 };
        var edge2 = new Edge() { Id = Guid.NewGuid(), FromId = node2.Id, ToId = node1.Id, SourceId = 12346 };
        var graph = new Graph(
            new() { { node1.Id, node1 }, { node2.Id, node2 } },
            new() { { node1.Id, new() { edge1 } }, { node2.Id, new() { edge2 } } },
            Guid.NewGuid()
        );

        graph.Nodes.Should().HaveCount(2);
        graph.GetEdgesFromNode(node1).Should().BeEquivalentTo(new List<Edge>() { edge1 });
        graph.GetEdgesFromNode(node2).Should().BeEquivalentTo(new List<Edge>() { edge2 });
    }
}
