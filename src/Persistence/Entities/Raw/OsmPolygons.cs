namespace Persistence.Entities.Raw;

using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

[Table("osm_polygons")]
public record OsmPolygon
{
    [Column("area_id")]
    public required long AreaId { get; init; }

    [Column("tags", TypeName = "jsonb")]
    public required IReadOnlyDictionary<string, string> Tags { get; init; }

    [Column("geom", TypeName = "geometry")]
    public required Polygon Geometry { get; init; }

    [Column("geom_linestring", TypeName = "geometry")]
    public required LineString GeometryAsLinestring { get; init; }

    [Column("nodes", TypeName = "bigint[]")]
    public required List<long> Nodes { get; init; }

    [Column("updated_at")]
    public required DateTime UpdatedAt { get; init; }
}
