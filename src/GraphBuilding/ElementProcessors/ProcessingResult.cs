namespace GraphBuilding.ElementProcessors;

public record ProcessingResult(IList<InMemoryNode> Nodes, IList<InMemoryEdge> Edges);
