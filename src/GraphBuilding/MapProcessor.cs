namespace GraphBuilding;

using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;
using Settings;

public partial class MapProcessor
{
    private readonly IOsmPort osm;
    private readonly IGraphSavingPort savingPort;
    private readonly AppSettings settings;
    private readonly ILogger<MapProcessor> logger;

    public MapProcessor(
        IOsmPort osm,
        IGraphSavingPort savingPort,
        AppSettings settings,
        ILogger<MapProcessor> logger
    )
    {
        this.osm = osm;
        this.savingPort = savingPort;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task Process(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;

        var graphBuilder = new GraphBuilder();
        await BuildGraph(graphBuilder, ct);
        LogBuiltGraph(graphBuilder.Edges.Count, graphBuilder.Nodes.Count);

        if (ct.IsCancellationRequested || graphBuilder.Edges.Count == 0)
            return;

        var version = await savingPort.AddVersion();
        var nodesToInsert = graphBuilder.GetNodesWithInternalIds().ToList();
        var nodeIds = await savingPort.SaveNodes(nodesToInsert.Select(x => x.Node), version);
        var edgesToInsert = graphBuilder.GetRemappedEdges(
            nodesToInsert
                .Select(x => x.InternalId)
                .Zip(nodeIds)
                .ToDictionary(x => x.First, x => x.Second)
        );
        _ = await savingPort.SaveEdges(edgesToInsert, version);

        await savingPort.FinalizeVersion(version);
    }

    private async Task BuildGraph(GraphBuilder builder, CancellationToken ct)
    {
        var lines = await osm.GetLines(settings.Bbox.AsRectangle());
        if (ct.IsCancellationRequested)
            return;
        foreach (var line in lines)
            if (line.Tags.ContainsKey("highway"))
                await CreateEdgesFromLine(builder, line);
    }

    private async Task CreateEdgesFromLine(GraphBuilder builder, OsmLine line)
    {
        (int Id, InMemoryNode Node)? prev = null;
        foreach (var (coord, nodeOsmId) in line.Geometry.Coordinates.Zip(line.Nodes))
        {
            var node = await GetOrCreateNode(builder, nodeOsmId, coord);

            if (node is not null && prev is not null)
            {
                var distance = prev.Value.Node.Coordinates.Distance(node.Value.Node.Coordinates);
                builder.AddEdge(new(prev.Value.Id, node.Value.Id, distance, distance, line.WayId));
            }

            prev = node;
        }
    }

    private async Task<(int Id, InMemoryNode Node)?> GetOrCreateNode(
        GraphBuilder builder,
        long nodeOsmId,
        Coordinate coord
    )
    {
        var level = 0;
        var existingNode = builder.GetNodeBySourceId(nodeOsmId, level);
        if (existingNode is null)
        {
            var osmNode = await osm.GetPointByOsmId(nodeOsmId);
            InMemoryNode node = osmNode switch
            {
                null => new(new(coord), level, nodeOsmId),
                _ => new(new(coord), level, nodeOsmId)
            };

            var id = builder.AddNode(node);
            return (id, node);
        }

        return existingNode;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Built graph with {EdgeCount} edges, {NodeCount} nodes"
    )]
    private partial void LogBuiltGraph(int edgeCount, int nodeCount);
}
