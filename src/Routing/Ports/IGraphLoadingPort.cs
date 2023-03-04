namespace Routing.Ports;

using Graph;

public interface IGraphLoadingPort
{
    Task<Guid?> GetCurrentGraphVersion();
    Task<IGraph?> GetGraph(Guid version);
}
