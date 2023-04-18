namespace GraphBuilding.Tests;

using NetTopologySuite.Geometries;

public class GeometryExtensionsTests
{
    private readonly GeometryFactory gf = new(new(), 4326);

    [Theory]
    [InlineData(40.7486, -73.9864, 40.74, -73.98, 0, 1098)]
    [InlineData(40.74001, -73.98, 40.74, -73.98, 0, 1.112)]
    [InlineData(-40.74001, 73.98, -40.74, 73.98, 0, 1.112)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 1, 3)]
    [InlineData(0, 45, 45, 0, 0, 6672_000)]
    public void CanCalculateDistance(
        double latA,
        double lonA,
        double latB,
        double lonB,
        decimal levelDelta,
        double expectedDistance
    )
    {
        var a = gf.CreatePoint(new Coordinate(lonA, latA));
        var b = gf.CreatePoint(new Coordinate(lonB, latB));
        var distance = a.GetMetricDistance(b, levelDelta);
        var precision = 0.003 * expectedDistance;
        distance.Should().BeApproximately(expectedDistance, precision);
    }
}
