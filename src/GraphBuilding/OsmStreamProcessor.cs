namespace GraphBuilding;

using Graph;
using Microsoft.FSharp.Collections;
using OsmSharp;
using Node = OsmSharp.Node;

public static class OsmStreamProcessor
{
    public static IGraph BuildGraphFromStream(IEnumerable<OsmGeo> source)
    {
        var builder = new GraphBuilder(Guid.NewGuid());

        foreach (var geo in source)
        {
            builder = geo switch
            {
                Node node => ProcessNode(builder, node),
                Way way => ProcessWay(builder, way),
                _ => builder
            };
        }

        return builder.Build();
    }

    private static GraphBuilder ProcessNode(GraphBuilder builder, Node node)
    {
        if (node is not { Longitude: not null, Latitude: not null })
        {
            return builder;
        }

        return builder.AddNode(
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(node.Longitude.Value, node.Latitude.Value),
                Level = 0,
                SourceId = node.Id
            }
        );
    }

    private static GraphBuilder ProcessWay(GraphBuilder builder, Way way)
    {
        if (way.Tags is null || !way.Tags.ContainsKey("highway"))
        {
            return builder;
        }

        foreach (
            var (wayNodeA, wayNodeB) in SeqModule.Windowed(2, way.Nodes).Select(x => (x[0], x[1]))
        )
        {
            var nodeA = builder.GetNodeBySourceId(wayNodeA);
            var nodeB = builder.GetNodeBySourceId(wayNodeB);
            if (nodeA is null || nodeB is null)
            {
                continue;
            }

            builder = builder
                .AddEdge(
                    new()
                    {
                        Id = Guid.NewGuid(),
                        FromId = nodeA.Id,
                        ToId = nodeB.Id,
                        SourceId = way.Id
                    }
                )
                .AddEdge(
                    new()
                    {
                        Id = Guid.NewGuid(),
                        FromId = nodeB.Id,
                        ToId = nodeA.Id,
                        SourceId = way.Id
                    }
                );
        }

        return builder;
    }
}
