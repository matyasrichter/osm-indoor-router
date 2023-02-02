namespace GraphBuilder.Ports;

using NetTopologySuite.Geometries;

public interface IGraphSavingPort
{
    Task SavePoints(IEnumerable<Point> points, Guid version);
    Task SaveEdges(IEnumerable<(Point, Point)> edges, Guid version);
    Task SaveCurrentVersion(Guid version);
}
