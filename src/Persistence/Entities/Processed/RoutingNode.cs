namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraphBuilding;
using NetTopologySuite.Geometries;

public class RoutingNode
{
    public RoutingNode() { }

    public RoutingNode(
        long version,
        Point coordinates,
        decimal level,
        Source? source,
        bool isLevelConnection
    )
    {
        Version = version;
        Coordinates = coordinates;
        Level = level;
        SourceId = source?.Id;
        SourceType = source?.Type;
        IsLevelConnection = isLevelConnection;
    }

    public RoutingNode(
        long id,
        long version,
        Point coordinates,
        decimal level,
        Source? source,
        bool isLevelConnection
    )
    {
        Id = id;
        Version = version;
        Coordinates = coordinates;
        Level = level;
        SourceId = source?.Id;
        SourceType = source?.Type;
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
    public SourceType? SourceType { get; init; }
}
