namespace Persistence.Repositories;

using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

public class OsmRepository : IOsmPort
{
    private readonly RoutingDbContext context;

    public OsmRepository(RoutingDbContext context) => this.context = context;

    public async Task<IEnumerable<OsmPoint>> GetPoints(Geometry boundingBox) =>
        await context.OsmPoints
            .Where(p => boundingBox.Covers(p.Geometry))
            .Select(p => new OsmPoint(p.NodeId, p.Tags, p.Geometry))
            .ToListAsync();

    public async Task<OsmPoint?> GetPointByOsmId(long osmId) =>
        await context.OsmPoints
            .Where(x => x.NodeId == osmId)
            .Select(x => new OsmPoint(x.NodeId, x.Tags, x.Geometry))
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<OsmPoint?>> GetPointsByOsmIds(IEnumerable<long> osmId)
    {
        var targetL = osmId.ToList();
        var result = await context.OsmPoints
            .Where(x => targetL.Any(targetId => x.NodeId == targetId))
            .Select(x => new OsmPoint(x.NodeId, x.Tags, x.Geometry))
            .ToDictionaryAsync(x => x.NodeId, x => x);
        return targetL.Select(x => result.GetValueOrDefault(x));
    }

    public async Task<IEnumerable<OsmLine>> GetLines(Geometry boundingBox) =>
        await context.OsmLines
            .Where(p => boundingBox.Intersects(p.Geometry))
            .Select(p => new OsmLine(p.WayId, p.Tags, p.Nodes, p.Geometry))
            .ToListAsync();

    public async Task<IEnumerable<OsmPolygon>> GetPolygons(Geometry boundingBox) =>
        await context.OsmPolygons
            .Where(p => boundingBox.Intersects(p.Geometry))
            .Select(p => new OsmPolygon(p.AreaId, p.Tags, p.Nodes, p.Geometry))
            .ToListAsync();
}
