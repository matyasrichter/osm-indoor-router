namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

public class RoutingNode
{
    public RoutingNode() { }

    public RoutingNode(long version, Point coordinates, decimal level, long? sourceId)
    {
        Version = version;
        Coordinates = coordinates;
        Level = level;
        SourceId = sourceId;
    }

    public RoutingNode(long id, long version, Point coordinates, decimal level, long? sourceId)
    {
        Id = id;
        Version = version;
        Coordinates = coordinates;
        Level = level;
        SourceId = sourceId;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }

    public long Version { get; init; }
    public Point Coordinates { get; init; } = default!;
    public decimal Level { get; init; }
    public long? SourceId { get; init; }
}
