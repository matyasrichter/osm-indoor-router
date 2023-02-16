namespace Graph;

public record Edge
{
    public required Guid Id { get; init; }
    public required Guid FromId { get; init; }
    public required Guid ToId { get; init; }
    public required string? SourceId { get; init; }
}
