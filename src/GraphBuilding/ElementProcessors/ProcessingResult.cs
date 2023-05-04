namespace GraphBuilding.ElementProcessors;

public record ProcessingResult(
    IList<InMemoryNode> Nodes,
    IList<InMemoryEdge> Edges,
    IList<(decimal Level, (int FromId, int ToId) Edge)> WallEdges
);
