namespace GraphBuilding.Tests;

using OsmSharp;
using OsmSharp.Tags;
using Ports;
using Node = Node;

public class OsmStreamProcessorTests
{
    [Fact]
    public async Task CanBuildTrivialGraph()
    {
        var dataStream = new OsmGeo[]
        {
            new OsmSharp.Node
            {
                Id = 123456,
                Latitude = 50.1963895,
                Longitude = 14.3519902
            },
            new OsmSharp.Node
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

        var savingPort = new Mock<IGraphSavingPort>();
        var version = 102;
        savingPort.Setup(x => x.AddVersion()).ReturnsAsync(version);
        savingPort
            .Setup(
                x => x.SaveNode(It.Is<InsertedNode>(n => n.SourceId == dataStream[0].Id), version)
            )
            .ReturnsAsync(
                new Func<InsertedNode, long, Node>(
                    (node, _) =>
                        new()
                        {
                            Id = 1,
                            Coordinates = node.Coordinates,
                            Level = node.Level,
                            SourceId = node.SourceId
                        }
                )
            );
        savingPort
            .Setup(
                x => x.SaveNode(It.Is<InsertedNode>(n => n.SourceId == dataStream[1].Id), version)
            )
            .ReturnsAsync(
                new Func<InsertedNode, long, Node>(
                    (node, _) =>
                        new()
                        {
                            Id = 2,
                            Coordinates = node.Coordinates,
                            Level = node.Level,
                            SourceId = node.SourceId
                        }
                )
            );
        savingPort
            .Setup(x => x.SaveEdges(It.IsAny<IEnumerable<InsertedEdge>>(), version))
            .ReturnsAsync(
                new Func<IEnumerable<InsertedEdge>, long, IEnumerable<Edge>>(
                    (x, _) =>
                        x.Select(
                            e =>
                                new Edge()
                                {
                                    Id = 1,
                                    FromId = e.FromId,
                                    ToId = e.ToId,
                                    Cost = e.Cost,
                                    ReverseCost = e.ReverseCost,
                                    SourceId = e.SourceId
                                }
                        )
                )
            );
        await new OsmStreamProcessor(savingPort.Object).BuildGraphFromStream(dataStream);
        savingPort.VerifyAll();
    }
}
