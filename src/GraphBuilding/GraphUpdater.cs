namespace GraphBuilding;

using Graph;
using Ports;
using Settings;

public class GraphUpdater
{
    private readonly IGraphSavingPort savingPort;
    private readonly OverpassLoader overpassLoader;
    private readonly AppSettings appSettings;

    public GraphUpdater(
        IGraphSavingPort savingPort,
        OverpassLoader overpassLoader,
        AppSettings appSettings
    )
    {
        this.savingPort = savingPort;
        this.overpassLoader = overpassLoader;
        this.appSettings = appSettings;
    }

    public async Task UpdateGraph(CancellationToken ct)
    {
        IGraph graph;
        using (
            var source = await overpassLoader.LoadInBBox(
                appSettings.Bbox.SouthWest.AsPoint(),
                appSettings.Bbox.NorthEast.AsPoint()
            )
        )
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            graph = OsmStreamProcessor.BuildGraphFromStream(source);
        }

        if (ct.IsCancellationRequested)
        {
            return;
        }

        await savingPort.SaveNodes(graph.Nodes, graph.Version);
        await savingPort.SaveEdges(graph.GetEdges(), graph.Version);
        await savingPort.SaveCurrentVersion(graph.Version);
    }
}
