namespace GraphBuilding;

using Graph;
using Ports;
using Settings;

public class GraphUpdater
{
    private readonly IGraphSavingPort savingPort;
    private readonly OverpassLoader overpassLoader;
    private readonly Settings settings;

    public GraphUpdater(IGraphSavingPort savingPort, OverpassLoader overpassLoader, Settings settings)
    {
        this.savingPort = savingPort;
        this.overpassLoader = overpassLoader;
        this.settings = settings;
    }

    public async Task UpdateGraph(CancellationToken ct)
    {
        IGraph graph;
        using (var source =
               await overpassLoader.LoadInBBox(
                   settings.Bbox.SouthWest.AsPoint(),
                   settings.Bbox.NorthEast.AsPoint()
               ))
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
