namespace GraphBuilding;

using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Ports;

public partial class WallGraphCutter
{
    private readonly ILogger<WallGraphCutter> logger;

    public void Run(GraphHolder holder, IReadOnlyDictionary<long, OsmPoint> osmPoints)
    {
        LogStarting();

        var edgeIndicesToRemove = new HashSet<int>();

        foreach (var level in holder.WallEdgeLevels)
        {
            var toRemove = ProcessSingleLevel(holder, level, osmPoints);

            foreach (var i in toRemove)
                _ = edgeIndicesToRemove.Add(i);
        }

        LogFinished(edgeIndicesToRemove.Count);
        // drop remapped indices
        foreach (var i in edgeIndicesToRemove.OrderDescending())
            holder.Edges.RemoveAt(i);
    }

    private IEnumerable<int> ProcessSingleLevel(
        GraphHolder holder,
        decimal level,
        IReadOnlyDictionary<long, OsmPoint> osmPoints
    )
    {
        LogStartingLevel(level);
        var edgeIndicesToRemove = new List<int>();
        var wallNodes = new Dictionary<int, InMemoryNode>();
        var edgesLineStrings = new List<LineString>();
        var wallEdgeStars = new Dictionary<int, HashSet<int>>();
        foreach (var (a, b) in holder.GetWallEdges(level))
        {
            _ = wallNodes[a.Id] = a.Node;
            _ = wallNodes[b.Id] = b.Node;
            edgesLineStrings.Add(
                new(new[] { a.Node.Coordinates.Coordinate, b.Node.Coordinates.Coordinate })
            );
            if (wallEdgeStars.TryGetValue(a.Id, out var starA))
                _ = starA.Add(b.Id);
            else
                wallEdgeStars[a.Id] = new() { b.Id };
            if (wallEdgeStars.TryGetValue(b.Id, out var starB))
                _ = starB.Add(a.Id);
            else
                wallEdgeStars[b.Id] = new() { a.Id };
        }

        var ml = new MultiLineString(edgesLineStrings.ToArray());

        foreach (var wallNode in wallNodes)
        {
            if (wallNode.Value.SourceId is null)
                continue;
            var sourceId = wallNode.Value.SourceId.Value;
            // skip doors, gates etc. - those are fine
            if (osmPoints.ContainsKey(sourceId) && IsWallOpening(osmPoints[sourceId].Tags))
                continue;
            // TODO: this is O(n) for the number of edges, but we need changes from previous iterations to be reflected
            var routingEdges = GetEdgesFrom(wallNode.Key, holder);
            // skip if there are no edges from/to this node (or only one)
            if (routingEdges.Count <= 1)
                continue;

            var centerCoordinate = wallNode.Value.Coordinates.Coordinate;
            var adjacentCoordinates = wallEdgeStars[wallNode.Key]
                .Select(x => holder.Nodes[x].Coordinates.Coordinate)
                // order by angle
                .OrderBy(t => Math.Atan2(centerCoordinate.Y - t.Y, centerCoordinate.X - t.X))
                .ToList();
            // add first to the end, otherwise the last two walls would be ignored
            adjacentCoordinates.Add(adjacentCoordinates.First());

            // node.edges are ordered counterclockwise
            foreach (
                var (l, r) in SeqModule.Windowed(2, adjacentCoordinates).Select(x => (x[0], x[1]))
            )
            {
                var bisectorAngle = AngleUtility.Bisector(r, centerCoordinate, l);
                var newCoordinate = MoveCoordinate(centerCoordinate, bisectorAngle, 0.000001);
                var newNode = wallNode.Value with { Coordinates = Gf.CreatePoint(newCoordinate) };
                var newId = holder.AddNode(newNode, checkUniqueness: false);
                var edgeCandidates = routingEdges
                    .Select(
                        x =>
                            x.Edge.FromId == wallNode.Key
                                ? x.Edge with
                                {
                                    FromId = newId
                                }
                                : x.Edge with
                                {
                                    ToId = newId
                                }
                    )
                    .Select(
                        x =>
                            x with
                            {
                                Geometry = holder.Nodes[(int)x.FromId].Coordinates.GetLineStringTo(
                                    holder.Nodes[(int)x.ToId].Coordinates
                                )
                            }
                    )
                    .Where(x => !x.Geometry.Crosses(ml));
                foreach (var e in edgeCandidates)
                    holder.Edges.Add(e);
            }

            edgeIndicesToRemove.AddRange(routingEdges.Select(x => x.Index));
        }

        LogStartingLevelGlobal(level);
        edgeIndicesToRemove.AddRange(
            holder.Edges
                .Select((x, i) => (x, i))
                .Where(
                    x =>
                        holder.Nodes[(int)x.x.FromId].Level == level
                        && holder.Nodes[(int)x.x.ToId].Level == level
                )
                .Where(x => x.x.Geometry.Crosses(ml))
                .Select(x => x.i)
        );

        return edgeIndicesToRemove;
    }

    private static List<(int Index, InMemoryEdge Edge)> GetEdgesFrom(long id, GraphHolder holder) =>
        holder.Edges
            .Select((x, i) => (i, x))
            .Where(x => x.x.FromId == id || x.x.ToId == id)
            .ToList();

    private static bool IsWallOpening(IReadOnlyDictionary<string, string> tags)
    {
        if (tags.ContainsKey("door") || tags.ContainsKey("entrance"))
            return true;
        else if (tags.GetValueOrDefault("barrier") is "turnstile" or "gate")
            return true;
        else
            return false;
    }

    private static readonly GeometryFactory Gf = new(new(), 4326);

    public WallGraphCutter(ILogger<WallGraphCutter> logger) => this.logger = logger;

    private static Coordinate MoveCoordinate(Coordinate source, double angle, double distance) =>
        new(source.X + (Math.Cos(angle) * distance), source.Y + (Math.Sin(angle) * distance));

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting wall cutting")]
    private partial void LogStarting();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting wall cutting on level {Level}"
    )]
    private partial void LogStartingLevel(decimal level);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting global wall cutting on level {Level}"
    )]
    private partial void LogStartingLevelGlobal(decimal level);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Finished wall cutting, removing {RemovedNodeCount} edges"
    )]
    private partial void LogFinished(int removedNodeCount);
}
