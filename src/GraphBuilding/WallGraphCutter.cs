namespace GraphBuilding;

using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Ports;

public partial class WallGraphCutter
{
    private readonly ILogger<WallGraphCutter> logger;

    public void Run(
        GraphHolder holder,
        IReadOnlyDictionary<long, OsmPoint> osmPoints,
        IReadOnlyDictionary<long, OsmPolygon> osmPolygons,
        IReadOnlyDictionary<long, OsmMultiPolygon> osmMultiPolygons
    )
    {
        LogStarting();

        foreach (var level in holder.WallEdgeLevels)
        {
            ProcessSingleLevel(holder, level, osmPoints, osmPolygons, osmMultiPolygons);
        }

        LogFinished();
    }

    private void ProcessSingleLevel(
        GraphHolder holder,
        decimal level,
        IReadOnlyDictionary<long, OsmPoint> osmPoints,
        IReadOnlyDictionary<long, OsmPolygon> osmPolygons,
        IReadOnlyDictionary<long, OsmMultiPolygon> osmMultiPolygons
    )
    {
        LogStartingLevel(level);
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

        var index = new STRtree<LineString>(edgesLineStrings.Count);
        foreach (var lineString in edgesLineStrings)
            index.Insert(lineString.EnvelopeInternal, lineString);

        foreach (var wallNode in wallNodes)
        {
            if (wallNode.Value.Source is null)
                continue;
            var sourceId = wallNode.Value.Source.Value.Id;
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

            // skip if this is the end vertex of a wall
            if (adjacentCoordinates.Count <= 1)
                continue;
            // add first to the end, otherwise the last two walls would be ignored
            adjacentCoordinates.Add(adjacentCoordinates.First());

            foreach (
                var (l, r) in SeqModule.Windowed(2, adjacentCoordinates).Select(x => (x[0], x[1]))
            )
            {
                // angle of right arm plus half of the angle between arms
                var bisectorAngle =
                    AngleUtility.Angle(centerCoordinate, r)
                    - (AngleUtility.InteriorAngle(l, centerCoordinate, r) / 2);
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
                    .Where(x =>
                    {
                        var candidates = index.Query(x.Geometry.EnvelopeInternal);
                        return candidates.Count == 0
                            || candidates.All(potentialCross =>
                            {
                                var intersection = x.Geometry.Relate(potentialCross);
                                return !intersection.IsIntersects()
                                    // boundary-only intersections are ok
                                    || intersection.Matches("FF*FT****");
                            });
                    })
                    .Where(x =>
                    {
                        // check that we didn't move it completely outside of original polygon (e.g. to the next room)
                        if (
                            x.Source is { Type: SourceType.Polygon, Id: var polygonId }
                            && osmPolygons.ContainsKey(polygonId)
                        )
                            return x.Geometry.Intersects(osmPolygons[polygonId].Geometry);
                        if (
                            x.Source is { Type: SourceType.Multipolygon, Id: var multiPolygonId }
                            && osmMultiPolygons.ContainsKey(multiPolygonId)
                        )
                            return x.Geometry.Intersects(osmMultiPolygons[multiPolygonId].Geometry);
                        return true;
                    });
                foreach (var e in edgeCandidates)
                    holder.Edges.Add(e);
            }

            foreach (var i in routingEdges.Select(x => x.Index).OrderDescending())
                holder.Edges.RemoveAt(i);
        }

        LogStartingLevelGlobal(level);
        var toRemoveGlobal = holder.Edges
            .Select((x, i) => (x, i))
            .Where(
                x =>
                    holder.Nodes[(int)x.x.FromId].Level == level
                    && holder.Nodes[(int)x.x.ToId].Level == level
            )
            .Where(x =>
            {
                var candidates = index.Query(x.x.Geometry.EnvelopeInternal);
                return candidates.Count > 0
                    && candidates.Any(potentialCross =>
                    {
                        var intersection = x.x.Geometry.Relate(potentialCross);
                        return !intersection.IsDisjoint() && !intersection.Matches("FF*FT****");
                        // var intersection = x.x.Geometry.Intersection(potentialCross);
                        // if (intersection.Dimension == Dimension.False || intersection.IsEmpty) return false;
                        // if (intersection.Dimension == Dimension.P)
                        //     return !wallNodes.ContainsKey((int)x.x.FromId) &&
                        //            !wallNodes.ContainsKey((int)x.x.ToId);
                        // return true;
                    });
            })
            .Select(x => x.i);
        foreach (var i in toRemoveGlobal.OrderDescending())
            holder.Edges.RemoveAt(i);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished wall cutting")]
    private partial void LogFinished();
}
