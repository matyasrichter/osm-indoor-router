namespace GraphBuilding.LineProcessors;

public record ProcessingResult(List<InMemoryNode> Nodes, List<InMemoryEdge> Edges);

public interface IOsmElementProcessor<in T>
{
    public Task<ProcessingResult> Process(T source);
}
