namespace GraphBuilding.Tests;

using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Ports;

public class WallGraphCutterTests
{
    [Fact]
    public void CutsEdgesFromBothSides()
    {
        // tests this situation (===) is a wall, (\|) are edges
        //     x
        //      \
        // x=====x=====x
        //       |\
        //       x x
        var holder = new GraphHolder();
        var nodes = new InMemoryNode[]
        {
            // wall nodes
            new(Gf.CreatePoint(new Coordinate(0, 0)), 0, 1),
            new(Gf.CreatePoint(new Coordinate(6, 0)), 0, 2),
            new(Gf.CreatePoint(new Coordinate(11, 0)), 0, 3),
            // top node
            new(Gf.CreatePoint(new Coordinate(4, 2)), 0, 4),
            // bottom nodes
            new(Gf.CreatePoint(new Coordinate(6, -2)), 0, 5),
            new(Gf.CreatePoint(new Coordinate(8, -2)), 0, 6),
        };
        var nodeIds = nodes.Select(x => (long)holder.AddNode(x)).ToList();
        var edges = new InMemoryEdge[]
        {
            // top edge
            new(
                nodeIds[3],
                nodeIds[1],
                nodes[3].Coordinates.GetLineStringTo(nodes[1].Coordinates),
                1,
                1,
                10,
                1
            ),
            // bottom edges
            new(
                nodeIds[1],
                nodeIds[4],
                nodes[1].Coordinates.GetLineStringTo(nodes[4].Coordinates),
                1,
                1,
                11,
                1
            ),
            new(
                nodeIds[5],
                nodeIds[1],
                nodes[5].Coordinates.GetLineStringTo(nodes[1].Coordinates),
                1,
                1,
                12,
                1
            ),
        };
        foreach (var e in edges)
            holder.AddEdge(e);
        // sanity checks
        holder.AddWallEdge(((int)nodeIds[0], (int)nodeIds[1]), 0);
        holder.AddWallEdge(((int)nodeIds[1], (int)nodeIds[2]), 0);
        holder.Nodes.Should().HaveCount(6);
        holder.Edges.Should().HaveCount(3);
        holder.WallEdgeLevels.Should().BeEquivalentTo(new[] { 0M });
        holder.GetWallEdges(0).Should().HaveCount(2);

        var cutter = new WallGraphCutter(Mock.Of<ILogger<WallGraphCutter>>());
        // passing empty points dict - no door
        cutter.Run(holder, new Dictionary<long, OsmPoint>());

        // verifying the result
        holder.Edges.Should().HaveCount(3, "no new edges created, only remapped");
        holder.Nodes.Should().HaveCount(8, "should have added two new nodes, one on each side");
    }

    private static readonly GeometryFactory Gf = new(new(), 4326);
}
