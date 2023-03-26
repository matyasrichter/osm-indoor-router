namespace Persistence;

using Entities.PgRouting;
using Entities.Processed;
using Entities.Raw;
using Microsoft.EntityFrameworkCore;

public class RoutingDbContext : DbContext
{
    public DbSet<RoutingNode> RoutingNodes => Set<RoutingNode>();
    public DbSet<RoutingEdge> RoutingEdges => Set<RoutingEdge>();
    public DbSet<RoutingGraphVersion> RoutingGraphVersions => Set<RoutingGraphVersion>();

    public DbSet<PgRoutingAStarOneToOneResult> PgRoutingAStarOneToOneResults =>
        Set<PgRoutingAStarOneToOneResult>();

    public DbSet<OsmPoint> OsmPoints => Set<OsmPoint>();
    public DbSet<OsmLine> OsmLines => Set<OsmLine>();
    public DbSet<OsmPolygon> OsmPolygons => Set<OsmPolygon>();

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
            .HasPostgresExtension("pgrouting");
        _ = modelBuilder.Entity<PgRoutingAStarOneToOneResult>().HasNoKey().ToView(null);
        // osm2pgsql output tables
        _ = modelBuilder.Entity<OsmPoint>().HasNoKey().ToTable(t => t.ExcludeFromMigrations());
        _ = modelBuilder.Entity<OsmLine>().HasNoKey().ToTable(t => t.ExcludeFromMigrations());
        _ = modelBuilder.Entity<OsmPolygon>().HasNoKey().ToTable(t => t.ExcludeFromMigrations());
    }
}
