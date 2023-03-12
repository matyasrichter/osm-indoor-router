namespace Persistence.Entities;

using System.ComponentModel.DataAnnotations;

public class MapVersion
{
    [Key]
    public Guid Version { get; init; }

    public DateTime CreatedAt { get; init; }
}
