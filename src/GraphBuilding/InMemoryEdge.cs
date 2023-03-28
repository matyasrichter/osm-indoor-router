namespace GraphBuilding;

public record InMemoryEdge(long FromId, long ToId, double Cost, double ReverseCost, long? SourceId);
