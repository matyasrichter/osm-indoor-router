namespace GraphBuilding.Tests;

using GraphBuilding;
using OsmSharp;
using OsmSharp.Tags;

public class OsmStreamProcessorTests
{
    [Fact]
    public void CanBuildTrivialGraph()
    {
        var dataStream = new OsmGeo[]
        {
            new Node
            {
                Id = 123456,
                Latitude = 50.1963895,
                Longitude = 14.3519902
            },
            new Node
            {
                Id = 123457,
                Latitude = 50.1965417,
                Longitude = 14.3523953
            },
            new Way
            {
                Id = 123458,
                Nodes = new[] { 123456L, 123457 },
                Tags = new TagsCollection { { "highway", "footway" } }
            }
        };

        var graph = OsmStreamProcessor.BuildGraphFromStream(dataStream);

        graph.Nodes.Should().HaveCount(2);
        graph.GetEdges().Should().HaveCount(2);
        var nodeA = graph.Nodes.First();
        var nodeB = graph.Nodes.Last();
        graph
            .GetEdgesFromNode(nodeA)
            .Should()
            .ContainSingle(x => x.FromId == nodeA.Id && x.ToId == nodeB.Id);
        graph
            .GetEdgesFromNode(nodeB)
            .Should()
            .ContainSingle(x => x.FromId == nodeB.Id && x.ToId == nodeA.Id);
    }
}
