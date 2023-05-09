namespace GraphBuilding.Tests.Processors;

using ElementProcessors;
using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;

public class LevelConnectingAreaProcessorTests
{
    private static readonly GeometryFactory Gf = new(new(), 4326);

    [Fact]
    public void TestProcessing()
    {
        var points = new List<Point>()
        {
            Gf.CreatePoint(new Coordinate(1, 1)),
            Gf.CreatePoint(new Coordinate(1, 2)),
            Gf.CreatePoint(new Coordinate(2, 2)),
            Gf.CreatePoint(new Coordinate(2, 1))
        };
        var processor = new LevelConnectingAreaProcessor(new(Mock.Of<ILogger<LevelParser>>()));

        var polygon = new OsmPolygon(
            123456,
            new Dictionary<string, string>()
            {
                { "indoor", "room" },
                { "stairs", "yes" },
                { "level", "1-4" }
            },
            new List<long>() { 1, 2, 3, 4, 1 },
            Gf.CreatePolygon(points.Append(points[0]).Select(x => x.Coordinate).ToArray()),
            Gf.CreateLineString(points.Append(points[0]).Select(x => x.Coordinate).ToArray())
        );
        var mp = new OsmMultiPolygon(
            polygon.AreaId,
            polygon.Tags,
            new(new[] { polygon.Geometry }),
            new[]
            {
                new OsmLine(
                    polygon.AreaId,
                    polygon.Tags,
                    polygon.Nodes,
                    polygon.GeometryAsLinestring
                )
            }
        );
        var osmPoints = new Dictionary<long, OsmPoint>()
        {
            {
                2,
                new(
                    2,
                    new Dictionary<string, string>() { { "door", "yes" }, { "repeat_on", "1-4" } },
                    points[3]
                )
            }
        };

        var result = processor.Process(mp, osmPoints, SourceType.Polygon);

        result.Nodes.Should().HaveCount(4 + (3 * 4), "4 doors, 4 wall nodes");
        result.Edges.Should().HaveCount(6);
    }
}
