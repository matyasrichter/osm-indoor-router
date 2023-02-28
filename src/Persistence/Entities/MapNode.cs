namespace Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using Graph;
using NetTopologySuite.Geometries;

public class MapNode
{
    [Key]
    public Guid Id { get; init; }

    public Guid Version { get; init; }
    public Point Coordinates { get; init; } = default!;
    public decimal Level { get; init; }
    public long? SourceId { get; init; }

    public static MapNode FromDomain(Node node, Guid version) =>
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
