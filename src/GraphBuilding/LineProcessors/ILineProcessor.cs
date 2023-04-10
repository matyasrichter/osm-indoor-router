namespace GraphBuilding.LineProcessors;

using Ports;

public record ProcessingResult(List<InMemoryNode> Nodes, List<InMemoryEdge> Edges);

public interface ILineProcessor
{
    public Task<ProcessingResult> Process(OsmLine source);
}
