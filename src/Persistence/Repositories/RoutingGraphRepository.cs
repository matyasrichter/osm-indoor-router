namespace Persistence.Repositories;

using Core;
using Entities.Processed;
using GraphBuilding;
using GraphBuilding.Ports;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Routing.Entities;
using Routing.Ports;

public class RoutingGraphRepository : IGraphSavingPort, IGraphInformationProvider, INodeFinder
{
    private readonly RoutingDbContext db;
    private readonly ITimeMachine timeMachine;

    public RoutingGraphRepository(RoutingDbContext db, ITimeMachine timeMachine)
    {
        this.db = db;
        this.timeMachine = timeMachine;
    }

    public async Task<IEnumerable<long>> SaveNodes(IEnumerable<InMemoryNode> nodes, long version)
    {
        var entities = nodes
            .Select(
                n => new RoutingNode(version, n.Coordinates, n.Level, n.Source, n.IsLevelConnection)
            )
            .ToList();
        await db.RoutingNodes.AddRangeAsync(entities);
        _ = await db.SaveChangesAsync();
        return entities.Select(x => x.Id);
    }

    public async Task<IEnumerable<long>> SaveEdges(IEnumerable<InMemoryEdge> edges, long version)
    {
        var toInsert = edges
            .Select(
                e =>
                    new RoutingEdge()
                    {
                        Version = version,
                        FromId = e.FromId,
                        ToId = e.ToId,
                        Cost = e.Cost,
                        ReverseCost = e.ReverseCost,
                        SourceId = e.Source?.Id,
                        SourceType = e.Source?.Type,
                        Distance = e.Distance,
                        IsElevator = e.IsElevator,
                        IsStairs = e.IsStairs,
                        IsEscalator = e.IsEscalator
                    }
            )
            .ToList();

        await db.RoutingEdges.AddRangeAsync(toInsert);
        _ = await db.SaveChangesAsync();
        return toInsert.Select(x => x.Id);
    }

    public async Task<int> RemoveNodesWithoutEdges(long version)
    {
        var removed = await db.RoutingNodes
            .Where(x => x.Version == version)
            .Where(n => !db.RoutingEdges.Any(e => e.FromId == n.Id || e.ToId == n.Id))
            .ExecuteDeleteAsync();
        _ = await db.SaveChangesAsync();
        return removed;
    }

    public async Task<int> RemoveSmallComponents(decimal threshold, long version)
    {
        var edgeCount = await db.Database
            .SqlQuery<long>(
                $"select count(*) as \"Value\" from \"RoutingNodes\" where \"Version\" = {version}"
            )
            .SingleAsync();
        var removeSmallerThan = edgeCount * threshold;
        var removed = await db.Database.ExecuteSqlRawAsync(
            $@"WITH nodeIds AS (SELECT unnest(nodes) as id
                     FROM (SELECT component, ARRAY_AGG(node) AS nodes
                           FROM pgr_connectedComponents(
                                   'SELECT ""Id"" as id, ""FromId"" as source, ""ToId"" as target, ""Cost"" as cost, ""ReverseCost"" as reverse_cost FROM ""RoutingEdges"" r WHERE r.""Version"" = {version}'
                    )
                    GROUP BY component
                        HAVING COUNT(node) < {removeSmallerThan}) as subquery)
            DELETE
                FROM ""RoutingEdges"" AS e
            USING nodeIds AS n
            WHERE e.""Version"" = {version}
                AND (e.""FromId"" = n.id OR e.""ToId"" = n.id)
                AND (n.id IS NOT NULL)"
        );
        _ = await db.SaveChangesAsync();
        return removed;
    }

    public async Task<long> AddVersion()
    {
        var inserted = await db.RoutingGraphVersions.AddAsync(
            new() { CreatedAt = timeMachine.Now, IsActive = false }
        );
        _ = await db.SaveChangesAsync();
        return inserted.Entity.Id;
    }

    public async Task FinalizeVersion(long version)
    {
        _ = await db.RoutingGraphVersions
            .Where(x => x.Id == version)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsActive, true));
        _ = await db.SaveChangesAsync();
    }

    public async Task<long?> GetCurrentGraphVersion() =>
        (
            await db.RoutingGraphVersions
                .OrderByDescending(x => x.CreatedAt)
                .Where(x => x.IsActive)
                .FirstOrDefaultAsync()
        )?.Id;

    public async Task<GraphFlags> GetGraphFlags(long version) =>
        // todo: replace with db.Database.SqlQuery<GraphFlags> in EF Core 8.0
        await db.GraphFlags
            .FromSql(
                $@"SELECT
                    EXISTS(SELECT 1 FROM ""RoutingEdges"" WHERE ""Version"" = {version} AND ""IsStairs"" IS TRUE) AS ""HasStairs"",
                    EXISTS(SELECT 1 FROM ""RoutingEdges"" WHERE ""Version"" = {version} AND ""IsEscalator"" IS TRUE) AS ""HasEscalators"",
                    EXISTS(SELECT 1 FROM ""RoutingEdges"" WHERE ""Version"" = {version} AND ""IsElevator"" IS TRUE) AS ""HasElevators"""
            )
            .SingleAsync();

    public async Task<Node?> FindClosestNode(
        double latitude,
        double longitude,
        decimal level,
        long graphVersion
    ) =>
        await db.RoutingNodes
            .Where(x => x.Version == graphVersion)
            .Where(x => x.Level == level)
            .OrderBy(
                x =>
                    x.Coordinates.Distance(
                        new GeometryFactory(new(), 4326).CreatePoint(
                            new Coordinate(longitude, latitude)
                        )
                    )
            )
            .Select(x => new Node(x.Id, x.Coordinates, x.Level, x.IsLevelConnection))
            .FirstOrDefaultAsync();
}
