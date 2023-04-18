namespace GraphBuilding.LineProcessors;

public record ProcessingResult(
    IReadOnlyList<InMemoryNode> Nodes,
    IReadOnlyList<InMemoryEdge> Edges
);

public interface IOsmElementProcessor<in T>
{
    public Task<ProcessingResult> Process(T source);
}
