namespace Persistence.Repositories;

using System.Runtime.CompilerServices;
using Entities;
using Entities.PgRouting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Routing.Entities;
using Routing.Ports;

public class PgRoutingRepository : IRoutingPort
{
    private readonly RoutingDbContext db;

    public PgRoutingRepository(RoutingDbContext db) => this.db = db;

    public async Task<IReadOnlyCollection<RouteSegment>> FindRoute(
        long sourceId,
        long targetId,
        long graphVersion
    )
    {
        var routingNodesTable = db.RoutingNodes.EntityType.GetTableName();
        var routingEdgesTable = db.RoutingEdges.EntityType.GetTableName();
        return await db.PgRoutingAStarOneToOneResults
            .FromSqlRaw(
                "SELECT * FROM pgr_aStar("
                    + $"   'SELECT "
                    + $"      e.\"{nameof(RoutingEdge.Id)}\" as id,"
                    + $"      e.\"{nameof(RoutingEdge.FromId)}\" as source,"
                    + $"      e.\"{nameof(RoutingEdge.ToId)}\" as target,"
                    + $"      e.\"{nameof(RoutingEdge.Cost)}\" as cost,"
                    + $"      e.\"{nameof(RoutingEdge.ReverseCost)}\" as reverse_cost,"
                    + $"      ST_X(n1.\"{nameof(RoutingNode.Coordinates)}\") as x1,"
                    + $"      ST_Y(n1.\"{nameof(RoutingNode.Coordinates)}\") as y1,"
                    + $"      ST_X(n2.\"{nameof(RoutingNode.Coordinates)}\") as x2,"
                    + $"      ST_Y(n2.\"{nameof(RoutingNode.Coordinates)}\") as y2"
                    + $"   FROM \"{routingEdgesTable}\" e"
                    + $"   JOIN \"{routingNodesTable}\" n1"
                    + $"      ON n1.\"{nameof(RoutingNode.Id)}\" = e.\"{nameof(RoutingEdge.FromId)}\""
                    + $"   JOIN \"{routingNodesTable}\" n2"
                    + $"      ON n2.\"{nameof(RoutingNode.Id)}\" = e.\"{nameof(RoutingEdge.ToId)}\""
                    // this should be safe from sqli, since we have already validated that the value is a long
                    + $"   WHERE e.\"{nameof(RoutingEdge.Version)}\" = \'\'{graphVersion}\'\'"
                    + "', @sourceId, @targetId)",
                new NpgsqlParameter("version", graphVersion),
                new NpgsqlParameter("sourceId", sourceId),
                new NpgsqlParameter("targetId", targetId)
            )
            .Include(x => x.RoutingNode)
            .Include(x => x.RoutingEdge)
            .Select(
                x =>
                    new RouteSegment(
                        new(x.RoutingNode.Id, x.RoutingNode.Coordinates, x.RoutingNode.Level),
                        new(x.RoutingEdge.Id, x.Cost),
                        x.AggCost
                    )
            )
            .ToListAsync();
    }
}
