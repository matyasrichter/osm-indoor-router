namespace GraphBuilding;

using NetTopologySuite.Geometries;

public static class GeometryExtensions
{
    /// <summary>
    /// Get distance in meters using the Haversine formula for great circle distance.
    /// </summary>
    public static double GetMetricDistance(this Point a, Point b, decimal levelDelta) =>
        GetMetricDistance(a.Coordinate, b.Coordinate, levelDelta);

    public static LineString GetLineStringTo(this Point a, Point b) =>
        Gf.CreateLineString(new[] { a.Coordinate, b.Coordinate });

    /// <summary>
    /// Get distance in meters using the Haversine formula for great circle distance.
    /// </summary>
    public static double GetMetricDistance(this Coordinate a, Coordinate b, decimal levelDelta)
    {
        var distance = Get2DDistance(a, b);
        if (levelDelta != 0)
        {
            // This is a very rough estimate,
            // but hopefully good enough to de-prioritize unnecessary level connections.
            // We're pretending that the level connection is the hypotenuse of a triangle.
            const double levelVerticalDistance = 3.0;
            var totalVerticalDistance = levelVerticalDistance * (double)Math.Abs(levelDelta);
            distance = Math.Sqrt(Math.Pow(distance, 2) + Math.Pow(totalVerticalDistance, 2));
        }

        return distance;
    }

    private static double Get2DDistance(Coordinate a, Coordinate b)
    {
        const double radius = 6371e3;

        var deltaLat = ToRadians(b.Y - a.Y);
        var deltaLon = ToRadians(b.X - a.X);

        var x =
            (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2))
            + (
                Math.Cos(ToRadians(a.Y))
                * Math.Cos(ToRadians(b.Y))
                * Math.Sin(deltaLon / 2)
                * Math.Sin(deltaLon / 2)
            );

        var greatCircleDistance = 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));

        return radius * greatCircleDistance;
    }

    private static readonly GeometryFactory Gf = new(new(), 4326);

    private static double ToRadians(double angle) => angle * (Math.PI / 180.0);
}
