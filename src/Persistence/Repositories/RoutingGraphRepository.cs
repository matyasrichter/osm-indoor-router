namespace Persistence.Repositories;

using Core;
using Entities;
using GraphBuilding;
using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;

public static class InsertedRoutingRecordExtensions
{
    public static Node ToNode(this InsertedNode from, long id) =>
        new()
        {
            Id = id,
            Coordinates = from.Coordinates,
            Level = from.Level,
            SourceId = from.SourceId
        };

    public static Edge ToEdge(this InsertedEdge from, long id) =>
        new()
        {
            Id = id,
            FromId = from.FromId,
            ToId = from.ToId,
            Cost = from.Cost,
            ReverseCost = from.ReverseCost,
            SourceId = from.SourceId
        };
}

public class RoutingGraphRepository : IGraphSavingPort
{
    private readonly RoutingDbContext db;
    private readonly ITimeMachine timeMachine;

    public RoutingGraphRepository(RoutingDbContext db, ITimeMachine timeMachine)
    {
        this.db = db;
        this.timeMachine = timeMachine;
    }

    public async Task<Node> SaveNode(InsertedNode node, long version)
    {
        var result = await db.RoutingNodes.AddAsync(
            new()
            {
                Version = node.Version,
                Coordinates = node.Coordinates,
                Level = node.Level,
                SourceId = node.SourceId
            }
        );
        _ = await db.SaveChangesAsync();
        return result.Entity.ToDomain();
    }

    public async Task<IEnumerable<Node>> GetNodes() =>
        await db.RoutingNodes.Select(x => x.ToDomain()).ToListAsync();

    public async Task<IEnumerable<Edge>> SaveEdges(IEnumerable<InsertedEdge> edges, long version)
    {
        var toInsert = edges
            .Select(
                e =>
                    new RoutingEdge()
                    {
                        Version = e.Version,
                        FromId = e.FromId,
                        ToId = e.ToId,
                        Cost = e.Cost,
                        ReverseCost = e.ReverseCost,
                        SourceId = e.SourceId
                    }
            )
            .ToList();

        await db.RoutingEdges.AddRangeAsync(toInsert);
        _ = await db.SaveChangesAsync();
        return toInsert.Select(x => x.ToDomain());
    }

    public async Task<IEnumerable<Edge>> GetEdges() =>
        await db.RoutingEdges.Select(x => x.ToDomain()).ToListAsync();

    public async Task<long> AddVersion() =>
        (
            await db.RoutingGraphVersions.AddAsync(
                new() { CreatedAt = timeMachine.Now, IsActive = false }
            )
        )
            .Entity
            .Id;

    public async Task FinalizeVersion(long version) =>
        await db.RoutingGraphVersions
            .Where(x => x.Id == version)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsActive, true));

    public async Task<long?> GetCurrentGraphVersion() =>
        await db.RoutingGraphVersions
            .OrderByDescending(x => x.CreatedAt)
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
}
