namespace Routing.Ports;

public record GraphFlags(bool HasStairs, bool HasEscalators, bool HasElevators);

public interface IGraphInformationProvider
{
    public Task<long?> GetCurrentGraphVersion();
    public Task<GraphFlags> GetGraphFlags(long version);
}
