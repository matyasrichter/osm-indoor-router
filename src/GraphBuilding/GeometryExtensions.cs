namespace GraphBuilding;

using System.Diagnostics;
using CoordinateSharp;
using NetTopologySuite.Geometries;

public static class GeometryExtensions
{
    public static double GetMetricDistance(this Point a, Point b) =>
        new Distance(a.ToCoordinateSharp(), b.ToCoordinateSharp()).Meters;

    private static CoordinateSharp.Coordinate ToCoordinateSharp(this Point point)
    {
        Debug.Assert(point.SRID == 4326);
        return new(point.Y, point.X);
    }
}
