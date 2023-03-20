namespace Persistence;

using Entities;
using Entities.PgRouting;
using Microsoft.EntityFrameworkCore;

public class RoutingDbContext : DbContext
{
    public DbSet<RoutingNode> RoutingNodes => Set<RoutingNode>();
    public DbSet<RoutingEdge> RoutingEdges => Set<RoutingEdge>();

    public DbSet<RoutingGraphVersion> RoutingGraphVersions => Set<RoutingGraphVersion>();

    public DbSet<PgRoutingAStarOneToOneResult> PgRoutingAStarOneToOneResults =>
        Set<PgRoutingAStarOneToOneResult>();

    public RoutingDbContext(DbContextOptions options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _ = optionsBuilder.UseNpgsql(o => o.UseNetTopologySuite());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder
            .HasPostgresExtension("postgis")
            .HasPostgresExtension("hstore")
            .HasPostgresExtension("pgrouting")
            .Entity<PgRoutingAStarOneToOneResult>()
            .HasNoKey()
            .ToView(null);
    }
}
