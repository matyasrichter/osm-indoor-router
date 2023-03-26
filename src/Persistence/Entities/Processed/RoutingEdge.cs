namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraphBuilding;

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
    public long? SourceId { get; init; }

    public static RoutingEdge FromDomain(Edge edge, int version) =>
        new()
        {
            Id = edge.Id,
            Version = version,
            FromId = edge.FromId,
            ToId = edge.ToId,
            SourceId = edge.SourceId
        };

    public Edge ToDomain() =>
        new()
        {
            Id = Id,
            FromId = FromId,
            ToId = ToId,
            Cost = Cost,
            ReverseCost = ReverseCost,
            SourceId = SourceId
        };
}
