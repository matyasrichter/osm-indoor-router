namespace Persistence.Entities.Raw;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

[Table("osm_multipolygons")]
public record OsmMultiPolygon
{
    [Column("area_id")]
    [Key]
    public required long AreaId { get; init; }

    [Column("tags", TypeName = "jsonb")]
    public required IReadOnlyDictionary<string, string> Tags { get; init; }

    [Column("geom", TypeName = "geometry")]
    public required MultiPolygon Geometry { get; init; }

    public IEnumerable<OsmLine> Members { get; init; } = default!;

    [Column("updated_at")]
    public required DateTime UpdatedAt { get; init; }
}

[Table("osm_multipolygons_m2m")]
public record OsmMultiPolygonM2M
{
    [Column("mp_id")]
    public long OsmMultiPolygonId { get; set; }

    [Column("l_id")]
    public long OsmLineId { get; set; }
}
