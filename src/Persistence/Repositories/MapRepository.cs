namespace Persistence.Repositories;

using Entities;
using Graph;
using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;

public class MapRepository : IGraphSavingPort
{
    private readonly MapDbContext dbContext;

    public MapRepository(MapDbContext dbContext) => this.dbContext = dbContext;

    public async Task<IGraph> GetAllByVersion(Guid version)
    {
        var nodes = await dbContext.MapNodes
            .AsNoTracking()
            .Where(x => x.Version == version)
            .ToDictionaryAsync(x => x.Id, x => x.ToDomain());
        var edges = await dbContext.MapEdges
            .AsNoTracking()
            .Where(x => x.Version == version)
            .GroupBy(x => x.FromId)
            .ToDictionaryAsync(x => x.Key, x => x.Select(y => y.ToDomain()).ToList());
        return new DictionaryGraph(nodes, edges, version);
    }

    public async Task SaveNodes(IEnumerable<Node> nodes, Guid version)
    {
        var mapNodes = nodes.Select(x => MapNode.FromDomain(x, version));
        await dbContext.MapNodes.AddRangeAsync(mapNodes);
        _ = await dbContext.SaveChangesAsync();
    }

    public async Task SaveEdges(IEnumerable<Edge> edges, Guid version)
    {
        var mapEdges = edges.Select(x => MapEdge.FromDomain(x, version));
        await dbContext.MapEdges.AddRangeAsync(mapEdges);
        _ = await dbContext.SaveChangesAsync();
    }

    public Task SaveCurrentVersion(Guid version) => Task.CompletedTask;
}
