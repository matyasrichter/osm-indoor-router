namespace Persistence.Entities;

using System.ComponentModel.DataAnnotations;

public class MapEdge
{
    [Key]
    public int Id { get; init; }

    public required Guid Version { get; init; }
    public required MapNode From { get; init; }
    public required MapNode To { get; init; }
    public string? SourceId { get; init; }
}
