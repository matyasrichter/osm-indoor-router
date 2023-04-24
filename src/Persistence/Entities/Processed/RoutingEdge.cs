namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class RoutingEdge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }

    public long Version { get; init; }
    public long FromId { get; init; }
    public RoutingNode From { get; init; } = default!;
    public long ToId { get; init; }
    public RoutingNode To { get; init; } = default!;
    public double Cost { get; set; }
    public double ReverseCost { get; set; }
    public double Distance { get; set; }
    public long? SourceId { get; init; }
}
