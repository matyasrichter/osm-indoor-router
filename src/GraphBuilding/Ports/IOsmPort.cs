namespace GraphBuilding.Ports;

using NetTopologySuite.Geometries;

public record OsmPoint(long NodeId, IReadOnlyDictionary<string, string> Tags, Point Geometry);

public record OsmLine(
    long WayId,
    IReadOnlyDictionary<string, string> Tags,
    ICollection<long> Nodes,
    LineString Geometry
);

public record OsmPolygon(
    long AreaId,
    IReadOnlyDictionary<string, string> Tags,
    ICollection<long> Nodes,
    Polygon Geometry,
    LineString GeometryAsLinestring
);

public record OsmMultiPolygon(
    long AreaId,
    IReadOnlyDictionary<string, string> Tags,
    MultiPolygon Geometry,
    ICollection<OsmLine> Members
);

public interface IOsmPort
{
    public Task<IEnumerable<OsmPoint>> GetPoints(Geometry boundingBox);
    public Task<IEnumerable<OsmLine>> GetLines(Geometry boundingBox);
    public Task<IEnumerable<OsmPolygon>> GetPolygons(Geometry boundingBox);
    public Task<IEnumerable<OsmMultiPolygon>> GetMultiPolygons(Geometry boundingBox);
}
