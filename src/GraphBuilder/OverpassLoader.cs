namespace GraphBuilder;

using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Streams;

public class OverpassLoader
{
    private const string OverpassApiUrl = "https://overpass-api.de/api/interpreter";
    private readonly HttpClient httpClient;

    public OverpassLoader(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<IEnumerable<OsmGeo>> LoadInBBox(Point southWest, Point northEast)
    {
        var bbox = $"{southWest.Coordinate.Y},{southWest.Coordinate.X},{northEast.Coordinate.Y},{northEast.Coordinate.X}";
        var query = $"nwr({bbox});out geom({bbox});";
        using var result = await httpClient.GetAsync($"{OverpassApiUrl}/{query}");
        return new XmlOsmStreamSource(await result.Content.ReadAsStreamAsync());
    }
}
