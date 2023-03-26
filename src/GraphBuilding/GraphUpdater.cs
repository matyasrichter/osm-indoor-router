namespace GraphBuilding;

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
            new(appSettings.Bbox.SouthWest.Latitude, appSettings.Bbox.SouthWest.Longitude),
            new(appSettings.Bbox.NorthEast.Latitude, appSettings.Bbox.NorthEast.Longitude)
        );
        if (ct.IsCancellationRequested)
        {
            return;
        }

        await streamProcessor.BuildGraphFromStream(source);
    }
}
