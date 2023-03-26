namespace Persistence.Entities.Raw;

using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

[Table("osm_points")]
public record OsmPoint
{
    [Column("node_id")]
    public required long NodeId { get; init; }

    [Column("tags", TypeName = "jsonb")]
    public required IReadOnlyDictionary<string, string> Tags { get; init; }

    [Column("geom", TypeName = "geometry")]
    public required Point Geometry { get; init; }

    [Column("updated_at")]
    public required DateTime UpdatedAt { get; init; }
}
