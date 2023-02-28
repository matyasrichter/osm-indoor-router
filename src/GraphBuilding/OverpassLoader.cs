namespace GraphBuilding;

using System.Web;
using NetTopologySuite.Geometries;
using OsmSharp.Streams;

public class OverpassLoader
{
    private const string OverpassApiUrl = "https://overpass-api.de/api/interpreter";
    private readonly HttpClient httpClient;

    public OverpassLoader(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<XmlOsmStreamSource> LoadInBBox(Point southWest, Point northEast)
    {
        var bbox =
            $"{southWest.Coordinate.X},{southWest.Coordinate.Y},{northEast.Coordinate.X},{northEast.Coordinate.Y}";
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["data"] = $"nwr({bbox});out;";
        var uriBuilder = new UriBuilder(OverpassApiUrl) { Query = query.ToString() };
        var result = await httpClient.GetAsync(uriBuilder.Uri);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Could not load data from overpass api: status {result.StatusCode}"
            );
        }

        return new(await result.Content.ReadAsStreamAsync());
    }
}
