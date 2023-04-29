namespace GraphBuilding;

using NetTopologySuite.Geometries;
using Ports;

/// <summary>
/// Proxy to IOsmPort that caches id lookups.
/// </summary>
public class CachingOsmPortWrapper : IOsmPort
{
    private readonly IOsmPort osm;
    private readonly Dictionary<long, OsmPoint> pointsCache = new();

    public CachingOsmPortWrapper(IOsmPort osm) => this.osm = osm;

    public async Task<OsmPoint?> GetPointByOsmId(long osmId)
    {
        var result = pointsCache.GetValueOrDefault(osmId);
        if (result is null)
        {
            result = await osm.GetPointByOsmId(osmId);
            if (result != null)
                pointsCache[osmId] = result;
        }

        return result;
    }

    public async Task<IEnumerable<OsmPoint?>> GetPointsByOsmIds(IEnumerable<long> osmId)
    {
        var osmIdL = osmId.ToList();
        // load as many from cache as possible
        var result = osmIdL.Select(x => (id: x, cached: pointsCache.GetValueOrDefault(x))).ToList();
        // fetch missing
        var missing = result.Where(x => x.cached is null).ToList();
        if (missing.Count == 0)
            return result.Select(x => x.cached);
        var missingResult = await osm.GetPointsByOsmIds(missing.Select(x => x.id));
        // add newly fetched to cache
        foreach (var point in missingResult.Where(x => x is not null))
            pointsCache[point!.NodeId] = point;
        // load from cache again, this time the missing ones should be there
        return result.Select(x => x.cached ?? pointsCache.GetValueOrDefault(x.id));
    }

    public async Task<IEnumerable<OsmPoint>> GetPoints(Geometry boundingBox) =>
        await osm.GetPoints(boundingBox);

    public async Task<IEnumerable<OsmLine>> GetLines(Geometry boundingBox) =>
        await osm.GetLines(boundingBox);

    public async Task<IEnumerable<OsmPolygon>> GetPolygons(Geometry boundingBox) =>
        await osm.GetPolygons(boundingBox);

    public async Task<IEnumerable<OsmMultiPolygon>> GetMultiPolygons(Geometry boundingBox) =>
        await osm.GetMultiPolygons(boundingBox);
}
