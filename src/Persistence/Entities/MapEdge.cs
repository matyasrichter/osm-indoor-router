namespace Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using Graph;

public class MapEdge
{
    [Key] public Guid Id { get; init; }

    public Guid Version { get; init; }

    public Guid FromId { get; init; }
    public MapNode From { get; init; } = default!;
    public Guid ToId { get; init; }
    public MapNode To { get; init; } = default!;
    public long? SourceId { get; init; }

    public static MapEdge FromDomain(Edge edge, Guid version) => new()
    {
        Id = edge.Id,
        Version = version,
        FromId = edge.FromId,
        ToId = edge.ToId,
        SourceId = edge.SourceId
    };

    public Edge ToDomain() => new() { Id = Id, FromId = FromId, ToId = ToId, SourceId = SourceId };
}
