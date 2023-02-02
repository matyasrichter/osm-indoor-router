namespace Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

public class MapNode
{
    [Key]
    public int Id { get; init; }

    public required Guid Version { get; init; }
    public required Point Coordinates { get; init; }
    public required int Level { get; init; }
    public string? SourceId { get; init; }
}
