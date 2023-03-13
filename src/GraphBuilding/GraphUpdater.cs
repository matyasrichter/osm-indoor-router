namespace GraphBuilding;

using Ports;
using Settings;

public class GraphUpdater
{
    private readonly OsmStreamProcessor streamProcessor;
    private readonly OverpassLoader overpassLoader;
    private readonly AppSettings appSettings;

    public GraphUpdater(
        OsmStreamProcessor streamProcessor,
        OverpassLoader overpassLoader,
        AppSettings appSettings
    )
    {
        this.streamProcessor = streamProcessor;
        this.overpassLoader = overpassLoader;
        this.appSettings = appSettings;
    }

    public async Task UpdateGraph(CancellationToken ct)
    {
        using var source = await overpassLoader.LoadInBBox(
            appSettings.Bbox.SouthWest.AsPoint(),
            appSettings.Bbox.NorthEast.AsPoint()
        );
        if (ct.IsCancellationRequested)
        {
            return;
        }

        await streamProcessor.BuildGraphFromStream(source);
    }
}
