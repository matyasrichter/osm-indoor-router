namespace Persistence.Entities.Processed;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraphBuilding;
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

    public static RoutingNode FromDomain(Node node, long version) =>
        new()
        {
            Id = node.Id,
            Version = version,
            Coordinates = node.Coordinates,
            Level = node.Level,
            SourceId = node.SourceId
        };

    public Node ToDomain() =>
        new()
        {
            Id = Id,
            Coordinates = Coordinates,
            Level = Level,
            SourceId = SourceId
        };
}
