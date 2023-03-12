namespace Persistence.Repositories;

using Core;
using Entities;
using Graph;
using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;
using Routing.Ports;

public class MapRepository : IGraphSavingPort, IGraphLoadingPort
{
    private readonly MapDbContext dbContext;
    private readonly ITimeMachine timeMachine;

    public MapRepository(MapDbContext dbContext, ITimeMachine timeMachine)
    {
        this.dbContext = dbContext;
        this.timeMachine = timeMachine;
    }

    public async Task<IGraph?> GetGraph(Guid version)
    {
        var nodes = await dbContext.MapNodes
            .AsNoTracking()
            .Where(x => x.Version == version)
            .ToDictionaryAsync(x => x.Id, x => x.ToDomain());
        if (nodes.Count == 0)
        {
            return null;
        }

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

    public async Task SaveCurrentVersion(Guid version)
    {
        _ = await dbContext.MapVersions.AddAsync(
            new() { CreatedAt = timeMachine.Now, Version = version }
        );
        _ = await dbContext.SaveChangesAsync();
    }

    public async Task<Guid?> GetCurrentGraphVersion() =>
        await dbContext.MapVersions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Version)
            .FirstOrDefaultAsync();
}
