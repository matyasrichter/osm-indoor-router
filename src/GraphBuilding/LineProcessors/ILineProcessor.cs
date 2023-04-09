namespace GraphBuilding.LineProcessors;

using Ports;

public record ProcessingResult(IList<InMemoryNode> Nodes, IList<InMemoryEdge> Edges);

public interface ILineProcessor
{
    public Task<ProcessingResult> Process(OsmLine source);
}
