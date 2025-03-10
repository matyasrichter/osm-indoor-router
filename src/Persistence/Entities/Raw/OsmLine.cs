namespace Persistence.Entities.Raw;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

[Table("osm_lines")]
public record OsmLine
{
    [Column("way_id")]
    [Key]
    public required long WayId { get; init; }

    [Column("tags", TypeName = "jsonb")]
    public required IReadOnlyDictionary<string, string> Tags { get; init; }

    [Column("geom", TypeName = "geometry")]
    public required LineString Geometry { get; init; }

    [Column("nodes", TypeName = "bigint[]")]
    public required List<long> Nodes { get; init; }
    public IEnumerable<OsmMultiPolygon> MultiPolygons { get; init; } = default!;

    [Column("updated_at")]
    public required DateTime UpdatedAt { get; init; }
}
