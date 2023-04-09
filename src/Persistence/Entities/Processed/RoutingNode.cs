namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

public class RoutingNode
{
    public RoutingNode() { }

    public RoutingNode(
        long version,
        Point coordinates,
        decimal level,
        long? sourceId,
        bool isLevelConnection
    )
    {
        Version = version;
        Coordinates = coordinates;
        Level = level;
        SourceId = sourceId;
        IsLevelConnection = isLevelConnection;
    }

    public RoutingNode(
        long id,
        long version,
        Point coordinates,
        decimal level,
        long? sourceId,
        bool isLevelConnection
    )
    {
        Id = id;
        Version = version;
        Coordinates = coordinates;
        Level = level;
        SourceId = sourceId;
        IsLevelConnection = isLevelConnection;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }

    public long Version { get; init; }

    [Column(TypeName = "geometry(Point,4326)")]
    public Point Coordinates { get; init; } = default!;

    public decimal Level { get; init; }
    public bool IsLevelConnection { get; init; }
    public long? SourceId { get; init; }
}
