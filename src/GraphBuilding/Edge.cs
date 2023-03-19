namespace GraphBuilding;

public record Edge
{
    public required long Id { get; init; }
    public required long FromId { get; init; }
    public required long ToId { get; init; }
    public required double Cost { get; init; }
    public required double ReverseCost { get; init; }
    public required long? SourceId { get; init; }
}
