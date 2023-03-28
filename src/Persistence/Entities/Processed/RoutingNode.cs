namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

public class RoutingNode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }

    public long Version { get; init; }
    public Point Coordinates { get; init; } = default!;
    public decimal Level { get; init; }
    public long? SourceId { get; init; }
}
