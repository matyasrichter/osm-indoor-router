namespace Routing.Ports;

using Entities;

public interface INodeFinder
{
    Task<Node?> FindClosestNode(
        double latitude,
        double longitude,
        decimal level,
        long graphVersion
    );
}
