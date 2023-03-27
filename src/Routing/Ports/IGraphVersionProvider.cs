namespace Routing.Ports;

public interface IGraphVersionProvider
{
    public Task<long?> GetCurrentGraphVersion();
}
