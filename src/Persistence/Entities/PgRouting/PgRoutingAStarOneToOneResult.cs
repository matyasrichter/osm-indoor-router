namespace Persistence.Entities.PgRouting;

using System.ComponentModel.DataAnnotations.Schema;
using Processed;

public sealed record PgRoutingAStarOneToOneResult
{
    [Column("seq")]
    public long Seq { get; init; }

    [Column("path_seq")]
    public long PathSeq { get; init; }

    [ForeignKey(nameof(RoutingNode))]
    [Column("node")]
    public long Node { get; init; }

    public RoutingNode RoutingNode { get; init; } = null!;

    [ForeignKey(nameof(RoutingEdge))]
    [Column("edge")]
    public long? Edge { get; init; }

    public RoutingEdge? RoutingEdge { get; init; } = null!;

    [Column("cost")]
    public double Cost { get; init; }

    [Column("agg_cost")]
    public double AggCost { get; init; }
}
