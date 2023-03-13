namespace GraphBuilding;

using Microsoft.FSharp.Collections;
using OsmSharp;
using Ports;

public class OsmStreamProcessor
{
    private readonly IGraphSavingPort savingPort;

    public OsmStreamProcessor(IGraphSavingPort savingPort) => this.savingPort = savingPort;

    public async Task BuildGraphFromStream(IEnumerable<OsmGeo> source)
    {
        var version = await savingPort.AddVersion();
        var builder = new GraphBuilder(version);

        foreach (var geo in source)
        {
            _ = geo switch
            {
                OsmSharp.Node node => await ProcessNode(builder, node, version),
                Way way => await ProcessWay(builder, way, version),
                _ => builder
            };
        }

        await savingPort.FinalizeVersion(version);
    }

    private async Task<GraphBuilder> ProcessNode(
        GraphBuilder builder,
        OsmSharp.Node node,
        long version
    )
    {
        if (node is not { Longitude: not null, Latitude: not null })
        {
            return builder;
        }

        var inserted = new InsertedNode(
            builder.Version,
            new(node.Longitude.Value, node.Latitude.Value),
            0,
            node.Id
        );
        var graphNode = await savingPort.SaveNode(inserted, version);
        return builder.AddNode(graphNode);
    }

    private async Task<GraphBuilder> ProcessWay(GraphBuilder builder, Way way, long version)
    {
        if (way.Tags is null || !way.Tags.ContainsKey("highway"))
        {
            return builder;
        }

        var insertedEdges = new List<InsertedEdge>();
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

            var edge = new InsertedEdge(
                builder.Version,
                nodeA.Id,
                nodeB.Id,
                GetPedestrianCost(nodeA, nodeB),
                GetPedestrianCost(nodeB, nodeA),
                way.Id
            );
            insertedEdges.Add(edge);
        }

        var edges = await savingPort.SaveEdges(insertedEdges, version);
        foreach (var edge in edges)
        {
            builder = builder.AddEdge(edge);
        }

        return builder;
    }

    private static double GetPedestrianCost(Node a, Node b)
    {
        var distance = a.Coordinates.Distance(b.Coordinates);
        // var verticalDistance = Math.Abs(a.Level - b.Level);
        return distance;
    }
}
