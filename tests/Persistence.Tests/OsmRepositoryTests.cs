namespace Persistence.Tests;

using Entities.Raw;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Repositories;
using OsmLine = GraphBuilding.Ports.OsmLine;
using OsmPoint = GraphBuilding.Ports.OsmPoint;
using OsmPolygon = GraphBuilding.Ports.OsmPolygon;

[Collection("DB")]
[Trait("Category", "DB")]
public sealed class OsmRepositoryTests : DbTestClass
{
    private readonly GeometryFactory gf = new(new(), 4326);

    [Fact]
    public async Task CanRetrievePoints()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var points = new OsmPoint[]
        {
            new(
                1,
                CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
                CreatePoint(10, 15)
            ),
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                CreatePoint(10, 16)
            )
        };

        foreach (var p in points)
            await SavePoint(p, now);

        var result = await repo.GetPoints(
            gf.CreatePolygon(
                new LinearRing(
                    new Coordinate[] { new(0, 0), new(0, 50), new(50, 50), new(50, 0), new(0, 0) }
                )
            )
        );

        result.OrderBy(x => x.NodeId).Should().BeEquivalentTo(points);
    }

    [Fact]
    public async Task CanRetrievePointById()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var points = new OsmPoint[]
        {
            new(
                1,
                CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
                CreatePoint(10, 15)
            ),
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                CreatePoint(10, 16)
            )
        };

        foreach (var p in points)
            await SavePoint(p, now);

        (await repo.GetPointByOsmId(1)).Should().BeEquivalentTo(points[0]);
        (await repo.GetPointByOsmId(123)).Should().BeNull();
    }

    [Fact]
    public async Task CanRetrieveMultiplePointsById()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var points = new OsmPoint[]
        {
            new(
                1,
                CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
                CreatePoint(10, 15)
            ),
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                CreatePoint(10, 16)
            )
        };

        foreach (var p in points)
            await SavePoint(p, now);

        (await repo.GetPointsByOsmIds(new long[] { 1, 2 }))
            .Should()
            .BeEquivalentTo(points, o => o.WithStrictOrdering());
        (await repo.GetPointsByOsmIds(new long[] { 2, 1 }))
            .Should()
            .BeEquivalentTo(points.Reverse(), o => o.WithStrictOrdering());
        (await repo.GetPointsByOsmIds(new long[] { 3, 4 }))
            .Should()
            .BeEquivalentTo(new OsmPoint?[] { null, null }, o => o.WithStrictOrdering());
        (await repo.GetPointsByOsmIds(new long[] { 1, 4 }))
            .Should()
            .BeEquivalentTo(new[] { points[0], null }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task LimitsPointsInBbox()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var points = new OsmPoint[]
        {
            new(
                1,
                new Dictionary<string, string>()
                {
                    { "amenity", "bicycle_parking" },
                    { "bicycle_parking", "stands" }
                },
                CreatePoint(40, 80)
            ),
            new(
                2,
                new Dictionary<string, string>() { { "amenity", "vending_machine" } },
                CreatePoint(-10, 10)
            ),
            new(3, new Dictionary<string, string>(), CreatePoint(20, 30)),
            new(4, new Dictionary<string, string>(), CreatePoint(0, 50))
        };

        foreach (var p in points)
            await SavePoint(p, now);

        var result = await repo.GetPoints(
            gf.CreatePolygon(
                new LinearRing(
                    new Coordinate[] { new(0, 0), new(0, 50), new(50, 50), new(50, 0), new(0, 0) }
                )
            )
        );

        result.OrderBy(x => x.NodeId).Should().BeEquivalentTo(points[2..4]);
    }

    [Fact]
    public async Task CanRetrieveLines()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var lines = new OsmLine[]
        {
            new(
                1,
                CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
                new long[] { 1, 2, 3 },
                CreateLineString((10, 15), (11, 16), (12, 16))
            ),
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 4, 1 },
                CreateLineString((10, 16), (12, 13), (10, 15))
            )
        };

        foreach (var p in lines)
            await SaveLine(p, now);

        var result = await repo.GetLines(
            gf.CreatePolygon(
                new LinearRing(
                    new Coordinate[] { new(0, 0), new(0, 50), new(50, 50), new(50, 0), new(0, 0) }
                )
            )
        );

        result.OrderBy(x => x.WayId).Should().BeEquivalentTo(lines);
    }

    [Fact]
    public async Task LimitsLinesInBbox()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);

        var lines = new OsmLine[]
        {
            // same as the boundary
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 123, 124, 125, 126 },
                CreateLineString(new(0, 0), new(0, 50), new(50, 50), new(50, 0))
            ),
            // only one point inside
            new(
                3,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 123, 127 },
                CreateLineString(new(10, 10), new(-10, -20))
            ),
            // completely outside
            new(
                4,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 129, 128 },
                CreateLineString(new(-10, -10), new(-20, -20))
            )
        };

        foreach (var p in lines)
            await SaveLine(p, now);

        var result = await repo.GetLines(
            gf.CreatePolygon(
                new LinearRing(
                    new Coordinate[] { new(0, 0), new(0, 50), new(50, 50), new(50, 0), new(0, 0) }
                )
            )
        );

        result.OrderBy(x => x.WayId).Should().BeEquivalentTo(lines[0..2]);
    }

    [Fact]
    public async Task CanRetrievePolygons()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var polygons = new OsmPolygon[]
        {
            new(
                1,
                CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
                new long[] { 1, 2, 3, 1 },
                CreatePolygon((10, 15), (11, 16), (12, 16), (10, 15)),
                CreateLineString((10, 15), (11, 16), (12, 16), (10, 15))
            ),
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 4, 1, 5, 4 },
                CreatePolygon((10, 16), (12, 13), (10, 15), (18, 19), (10, 16)),
                CreateLineString((10, 16), (12, 13), (10, 15), (18, 19), (10, 16))
            )
        };

        foreach (var p in polygons)
            await SavePolygon(p, now);

        var result = await repo.GetPolygons(
            gf.CreatePolygon(
                new LinearRing(
                    new Coordinate[] { new(0, 0), new(0, 50), new(50, 50), new(50, 0), new(0, 0) }
                )
            )
        );

        result.OrderBy(x => x.AreaId).Should().BeEquivalentTo(polygons);
    }

    [Fact]
    public async Task LimitsPolygonsInBbox()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);

        var polygons = new OsmPolygon[]
        {
            // same as the boundary
            new(
                0,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 123, 124, 125, 126, 123 },
                CreatePolygon((0, 0), (0, 50), (50, 50), (50, 0), (0, 0)),
                CreateLineString((0, 0), (0, 50), (50, 50), (50, 0), (0, 0))
            ),
            // only one point inside
            new(
                1,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 123, 127, 128, 123 },
                CreatePolygon((10, 10), (-10, -20), (-20, -20), (10, 10)),
                CreateLineString((10, 10), (-10, -20), (-20, -20), (10, 10))
            ),
            // completely outside
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 129, 128, 130, 129 },
                CreatePolygon((-10, -10), (-20, -20), (-30, -20), (-10, -10)),
                CreateLineString((-10, -10), (-20, -20), (-30, -20), (-10, -10))
            ),
            // completely outside but overlaps
            new(
                3,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 987, 986, 985, 984, 987 },
                CreatePolygon((-10, 20), (-10, 40), (60, 35), (60, 26), (-10, 20)),
                CreateLineString((-10, 20), (-10, 40), (60, 35), (60, 26), (-10, 20))
            )
        };

        foreach (var p in polygons)
            await SavePolygon(p, now);

        var result = await repo.GetPolygons(
            gf.CreatePolygon(
                new LinearRing(
                    new Coordinate[] { new(0, 0), new(0, 50), new(50, 50), new(50, 0), new(0, 0) }
                )
            )
        );

        result
            .OrderBy(x => x.AreaId)
            .Should()
            .BeEquivalentTo(polygons[0..2].Concat(polygons[3..4]));
    }

    [Fact]
    public async Task CanRetrieveMultiPolygons()
    {
        var repo = new OsmRepository(DbContext);
        var now = new DateTime(2023, 03, 01, 15, 11, 00, DateTimeKind.Utc);
        var polygons = new OsmLine[]
        {
            new(
                1,
                CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
                new long[] { 1, 2, 3, 1 },
                CreateLineString((10, 15), (11, 16), (12, 16), (10, 15))
            ),
            new(
                2,
                CreateTags(new KeyValuePair<string, string>("amenity", "vending_machine")),
                new long[] { 4, 1, 5, 4 },
                CreateLineString((10, 16), (12, 13), (10, 15), (18, 19), (10, 16))
            )
        };
        foreach (var polygon in polygons)
            await SaveLine(polygon, now);
        var multiPolygon = new OsmMultiPolygon()
        {
            AreaId = 1,
            Tags = CreateTags(new("amenity", "bicycle_parking"), new("bicycle_parking", "stands")),
            Members = polygons.Select(
                x =>
                    new Entities.Raw.OsmLine()
                    {
                        WayId = x.WayId,
                        Geometry = x.Geometry,
                        Tags = x.Tags,
                        Nodes = x.Nodes.ToList(),
                        UpdatedAt = now
                    }
            ),
            Geometry = CreateMultiPolygon(polygons.Select(x => x.Geometry).ToArray()),
            UpdatedAt = now
        };

        await SaveMultiPolygon(multiPolygon, now);

        var result = (
            await repo.GetMultiPolygons(
                gf.CreatePolygon(
                    new LinearRing(
                        new Coordinate[]
                        {
                            new(0, 0),
                            new(0, 50),
                            new(50, 50),
                            new(50, 0),
                            new(0, 0)
                        }
                    )
                )
            )
        ).ToList();

        result.Should().HaveCount(1);
        var mp = result[0];
        mp.AreaId.Should().Be(multiPolygon.AreaId);
        mp.Tags.Should().BeEquivalentTo(multiPolygon.Tags);
        ((IComparable)mp.Geometry).Should().Be(multiPolygon.Geometry);
        mp.Members.Should().BeEquivalentTo(polygons);
    }

    private Point CreatePoint(double x, double y) => gf.CreatePoint(new Coordinate(x, y));

    private LineString CreateLineString(params (double x, double y)[] nodes) =>
        gf.CreateLineString(nodes.Select(x => new Coordinate(x.x, x.y)).ToArray());

    private Polygon CreatePolygon(params (double x, double y)[] nodes) =>
        gf.CreatePolygon(nodes.Select(x => new Coordinate(x.x, x.y)).ToArray());

    private MultiPolygon CreateMultiPolygon(params LineString[] members) =>
        gf.CreateMultiPolygon(members.Select(x => new Polygon(new(x.Coordinates))).ToArray());

    private static IReadOnlyDictionary<string, string> CreateTags(
        params KeyValuePair<string, string>[] items
    ) => new Dictionary<string, string>(items);

    private async Task SavePoint(OsmPoint p, DateTime now) =>
        await DbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO osm_points (node_id, tags, geom, updated_at) VALUES (@p0, @p1::jsonb, @p2, @p3)",
            p.NodeId,
            p.Tags,
            p.Geometry,
            now
        );

    private async Task SaveLine(OsmLine p, DateTime now) =>
        await DbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO osm_lines (way_id, tags, geom, nodes, updated_at) VALUES (@p0, @p1::jsonb, @p2, @p3, @p4)",
            p.WayId,
            p.Tags,
            p.Geometry,
            p.Nodes,
            now
        );

    private async Task SavePolygon(OsmPolygon p, DateTime now) =>
        await DbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO osm_polygons (area_id, tags, geom, geom_linestring, nodes, updated_at) VALUES (@p0, @p1::jsonb, @p2, @p3, @p4, @p5)",
            p.AreaId,
            p.Tags,
            p.Geometry,
            p.GeometryAsLinestring,
            p.Nodes,
            now
        );

    private async Task SaveMultiPolygon(OsmMultiPolygon p, DateTime now)
    {
        await DbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO osm_multipolygons (area_id, tags, geom, updated_at) VALUES (@p0, @p1::jsonb, @p2, @p3)",
            p.AreaId,
            p.Tags,
            p.Geometry,
            now
        );
        await DbContext.OsmMultiPolygonsM2M.AddRangeAsync(
            p.Members.Select(
                l => new OsmMultiPolygonM2M() { OsmMultiPolygonId = p.AreaId, OsmLineId = l.WayId }
            )
        );
        await DbContext.SaveChangesAsync();
    }

    public OsmRepositoryTests(DatabaseFixture dbFixture)
        : base(dbFixture) { }
}
