namespace GraphBuilding;

using Microsoft.Extensions.Logging;
using Ports;

public partial class MapProcessor
{
    private readonly IGraphSavingPort savingPort;
    private readonly ILogger<MapProcessor> logger;
    private readonly IGraphBuilder graphBuilder;

    public MapProcessor(
        IGraphSavingPort savingPort,
        ILogger<MapProcessor> logger,
        IGraphBuilder graphBuilder
    )
    {
        this.savingPort = savingPort;
        this.logger = logger;
        this.graphBuilder = graphBuilder;
    }

    public async Task Process(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;

        var graphHolder = await graphBuilder.BuildGraph(ct);
        LogBuiltGraph(graphHolder.Edges.Count, graphHolder.Nodes.Count);

        if (ct.IsCancellationRequested || graphHolder.Edges.Count == 0)
            return;

        var version = await savingPort.AddVersion();
        var nodesToInsert = graphHolder.GetNodesWithInternalIds().ToList();
        var nodeIds = await savingPort.SaveNodes(nodesToInsert.Select(x => x.Node), version);
        var edgesToInsert = graphHolder.GetRemappedEdges(
            nodesToInsert
                .Select(x => x.InternalId)
                .Zip(nodeIds)
                .ToDictionary(x => x.First, x => x.Second)
        );
        _ = await savingPort.SaveEdges(edgesToInsert, version);

        await savingPort.FinalizeVersion(version);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Built graph with {EdgeCount} edges, {NodeCount} nodes"
    )]
    private partial void LogBuiltGraph(int edgeCount, int nodeCount);
}
