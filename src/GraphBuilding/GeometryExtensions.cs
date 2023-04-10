namespace GraphBuilding;

using System.Diagnostics;
using NetTopologySuite.Geometries;

public static class GeometryExtensions
{
    /// <summary>
    /// Get distance in meters using the Haversine formula for great circle distance.
    /// </summary>
    public static double GetMetricDistance(this Point a, Point b)
    {
        Debug.Assert(a.SRID == 4326 && b.SRID == 4326, "SRID must be 4326");
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

        var greatCircleDistance = 2 * Math.Asin(Math.Min(1, Math.Sqrt(x)));

        return radius * greatCircleDistance;
    }

    private static double ToRadians(double angle) => angle * (Math.PI / 180.0);
}
