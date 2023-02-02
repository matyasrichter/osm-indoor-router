namespace Persistence.Repositories;

using GraphBuilder.Ports;
using NetTopologySuite.Geometries;

public class MapRepository : IGraphSavingPort
{
    public Task SavePoints(IEnumerable<Point> points, Guid version) => throw new NotImplementedException();

    public Task SaveEdges(IEnumerable<(Point, Point)> edges, Guid version) => throw new NotImplementedException();

    public Task SaveCurrentVersion(Guid version) => throw new NotImplementedException();
}
