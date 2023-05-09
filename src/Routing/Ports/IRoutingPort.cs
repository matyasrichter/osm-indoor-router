namespace Routing.Ports;

using Entities;

public interface IRoutingPort
{
    public Task<IReadOnlyCollection<RouteSegment>> FindRoute(
        long sourceId,
        long targetId,
        long graphVersion,
        bool disallowStairs,
        bool disallowElevators,
        bool disallowEscalators
    );
}
