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
            new(Gf.CreatePoint(new Coordinate(0, 0)), 0, new(SourceType.Point, 1)),
            new(Gf.CreatePoint(new Coordinate(6, 0)), 0, new(SourceType.Point, 2)),
            new(Gf.CreatePoint(new Coordinate(11, 0)), 0, new(SourceType.Point, 3)),
            // top node
            new(Gf.CreatePoint(new Coordinate(4, 2)), 0, new(SourceType.Point, 4)),
            // bottom nodes
            new(Gf.CreatePoint(new Coordinate(6, -2)), 0, new(SourceType.Point, 5)),
            new(Gf.CreatePoint(new Coordinate(8, -2)), 0, new(SourceType.Point, 6)),
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
                new(SourceType.Line, 10),
                1
            ),
            // bottom edges
            new(
                nodeIds[1],
                nodeIds[4],
                nodes[1].Coordinates.GetLineStringTo(nodes[4].Coordinates),
                1,
                1,
                new(SourceType.Line, 11),
                1
            ),
            new(
                nodeIds[5],
                nodeIds[1],
                nodes[5].Coordinates.GetLineStringTo(nodes[1].Coordinates),
                1,
                1,
                new(SourceType.Line, 12),
                1
            ),
        };
        foreach (var e in edges)
            holder.AddEdge(e);
        holder.AddWallEdge(((int)nodeIds[0], (int)nodeIds[1]), 0);
        holder.AddWallEdge(((int)nodeIds[1], (int)nodeIds[2]), 0);

        // sanity checks
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

    [Fact]
    public void PreservesEdgesAroundCorner()
    {
        // tests this situation (\/) is a wall, (.) are edges
        //     x   x
        //      \ .
        //       x
        //      / .
        //     x   x
        var holder = new GraphHolder();
        var nodes = new InMemoryNode[]
        {
            // wall nodes
            new(Gf.CreatePoint(new Coordinate(0, 2)), 0, new(SourceType.Point, 1)),
            new(Gf.CreatePoint(new Coordinate(2, 0)), 0, new(SourceType.Point, 2)),
            new(Gf.CreatePoint(new Coordinate(0, -2)), 0, new(SourceType.Point, 3)),
            // top node
            new(Gf.CreatePoint(new Coordinate(4, 2)), 0, new(SourceType.Point, 4)),
            // bottom node
            new(Gf.CreatePoint(new Coordinate(4, -2)), 0, new(SourceType.Point, 5)),
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
                new(SourceType.Line, 10),
                1
            ),
            // bottom edges
            new(
                nodeIds[1],
                nodeIds[4],
                nodes[1].Coordinates.GetLineStringTo(nodes[4].Coordinates),
                1,
                1,
                new(SourceType.Line, 11),
                1
            ),
        };
        foreach (var e in edges)
            holder.AddEdge(e);
        holder.AddWallEdge(((int)nodeIds[0], (int)nodeIds[1]), 0);
        holder.AddWallEdge(((int)nodeIds[1], (int)nodeIds[2]), 0);

        // sanity checks
        holder.Nodes.Should().HaveCount(5);
        holder.Edges.Should().HaveCount(2);
        holder.WallEdgeLevels.Should().BeEquivalentTo(new[] { 0M });
        holder.GetWallEdges(0).Should().HaveCount(2);

        var cutter = new WallGraphCutter(Mock.Of<ILogger<WallGraphCutter>>());
        // passing empty points dict - no door
        cutter.Run(holder, new Dictionary<long, OsmPoint>());

        // verifying the result
        holder.Edges.Should().HaveCount(2, "no new edges created, only remapped");
        holder.Nodes.Should().HaveCount(7, "should have added two new nodes, one on each side");
        var edgeToTop = holder.Edges
            .Where(x => x.FromId == nodeIds[3] || x.ToId == nodeIds[3])
            .ToList();
        var edgeToBottom = holder.Edges
            .Where(x => x.FromId == nodeIds[4] || x.ToId == nodeIds[4])
            .ToList();
        edgeToTop.Should().HaveCount(1);
        edgeToBottom.Should().HaveCount(1);
        new HashSet<long>()
        {
            edgeToTop[0].FromId,
            edgeToTop[0].ToId,
            edgeToBottom[0].FromId,
            edgeToBottom[0].ToId
        }
            .Should()
            .HaveCount(3);
    }

    [Fact]
    public void PreservesEdgesAlongWall()
    {
        // tests this situation
        // x===x===x
        var holder = new GraphHolder();
        var nodes = new InMemoryNode[]
        {
            // wall nodes
            new(Gf.CreatePoint(new Coordinate(0, 0)), 0, new(SourceType.Point, 1)),
            new(Gf.CreatePoint(new Coordinate(2, 0)), 0, new(SourceType.Point, 2)),
            new(Gf.CreatePoint(new Coordinate(4, 0)), 0, new(SourceType.Point, 3)),
        };
        var nodeIds = nodes.Select(x => (long)holder.AddNode(x)).ToList();
        var edges = new InMemoryEdge[]
        {
            // top edge
            new(
                nodeIds[0],
                nodeIds[1],
                nodes[0].Coordinates.GetLineStringTo(nodes[1].Coordinates),
                1,
                1,
                new(SourceType.Line, 10),
                1
            ),
            // bottom edges
            new(
                nodeIds[1],
                nodeIds[2],
                nodes[1].Coordinates.GetLineStringTo(nodes[2].Coordinates),
                1,
                1,
                new(SourceType.Line, 11),
                1
            ),
        };
        foreach (var e in edges)
            holder.AddEdge(e);
        holder.AddWallEdge(((int)nodeIds[0], (int)nodeIds[1]), 0);
        holder.AddWallEdge(((int)nodeIds[1], (int)nodeIds[2]), 0);

        // sanity checks
        holder.Nodes.Should().HaveCount(3);
        holder.Edges.Should().HaveCount(2);
        holder.WallEdgeLevels.Should().BeEquivalentTo(new[] { 0M });
        holder.GetWallEdges(0).Should().HaveCount(2);

        var cutter = new WallGraphCutter(Mock.Of<ILogger<WallGraphCutter>>());
        // passing empty points dict - no door
        cutter.Run(holder, new Dictionary<long, OsmPoint>());

        // verifying the result
        holder.Edges.Should().HaveCount(4, "new edges created on each side of the wall");
        holder.Nodes.Should().HaveCount(5, "should have added two new nodes, one on each side");
        var edgeToLeft = holder.Edges
            .Where(x => x.FromId == nodeIds[0] || x.ToId == nodeIds[0])
            .ToList();
        var edgeToRight = holder.Edges
            .Where(x => x.FromId == nodeIds[2] || x.ToId == nodeIds[2])
            .ToList();
        edgeToLeft.Should().HaveCount(2);
        edgeToRight.Should().HaveCount(2);
    }

    [Fact]
    public void DoesNotProduceEdgesCrossingWallFromEdgesAlong()
    {
        // tests this situation (walls form rooms with sourceId 10,11,12)
        //     0-----1------2
        //     |    /      /
        // 3---4---5---6--7
        // |          /
        // |         /
        // 8--------9
        var holder = new GraphHolder();
        var nodes = new InMemoryNode[]
        {
            // wall nodes, left-to-right, top-to-bottom
            new(Gf.CreatePoint(new Coordinate(4, 2)), 1, new(SourceType.Point, 0)),
            new(Gf.CreatePoint(new Coordinate(10, 2)), 1, new(SourceType.Point, 1)),
            new(Gf.CreatePoint(new Coordinate(18, 2)), 1, new(SourceType.Point, 2)),
            new(Gf.CreatePoint(new Coordinate(0, 0)), 1, new(SourceType.Point, 3)),
            new(Gf.CreatePoint(new Coordinate(4, 0)), 1, new(SourceType.Point, 4)),
            new(Gf.CreatePoint(new Coordinate(8, 0)), 1, new(SourceType.Point, 5)),
            new(Gf.CreatePoint(new Coordinate(12, 0)), 1, new(SourceType.Point, 6)),
            new(Gf.CreatePoint(new Coordinate(16, 0)), 1, new(SourceType.Point, 7)),
            new(Gf.CreatePoint(new Coordinate(0, -3)), 1, new(SourceType.Point, 8)),
            new(Gf.CreatePoint(new Coordinate(9, -3)), 1, new(SourceType.Point, 9)),
        };
        var nodeIds = nodes.Select(x => (long)holder.AddNode(x)).ToList();

        InMemoryEdge NewEdge(int from, int to, long sourceId)
        {
            var e = new InMemoryEdge(
                from,
                to,
                nodes![from].Coordinates.GetLineStringTo(nodes[to].Coordinates),
                1,
                1,
                new(SourceType.Line, sourceId),
                1
            );
            holder.AddEdge(e);
            return e;
        }

        var edges = new InMemoryEdge[]
        {
            NewEdge(0, 1, 10),
            NewEdge(0, 5, 10),
            NewEdge(0, 4, 10),
            NewEdge(1, 2, 11),
            NewEdge(1, 4, 10),
            NewEdge(1, 7, 11),
            NewEdge(1, 6, 11),
            NewEdge(1, 5, 10),
            NewEdge(1, 5, 11),
            NewEdge(2, 7, 11),
            NewEdge(2, 6, 11),
            NewEdge(2, 5, 11),
            NewEdge(3, 4, 12),
            NewEdge(3, 9, 12),
            NewEdge(3, 8, 12),
            NewEdge(4, 5, 10),
            NewEdge(4, 5, 12),
            NewEdge(4, 9, 12),
            NewEdge(4, 8, 12),
            NewEdge(5, 6, 11),
            NewEdge(5, 6, 12),
            NewEdge(5, 9, 12),
            NewEdge(5, 8, 12),
            NewEdge(6, 7, 11),
            NewEdge(6, 9, 12),
            NewEdge(6, 8, 12),
            NewEdge(8, 9, 12),
        };

        holder.AddWallEdge((0, 1), 1);
        holder.AddWallEdge((0, 4), 1);
        holder.AddWallEdge((1, 2), 1);
        holder.AddWallEdge((1, 5), 1);
        holder.AddWallEdge((2, 7), 1);
        holder.AddWallEdge((3, 4), 1);
        holder.AddWallEdge((3, 8), 1);
        holder.AddWallEdge((4, 5), 1);
        holder.AddWallEdge((5, 6), 1);
        holder.AddWallEdge((6, 9), 1);
        holder.AddWallEdge((6, 7), 1);
        holder.AddWallEdge((8, 9), 1);

        // sanity checks
        holder.Nodes.Should().HaveCount(10);
        holder.Edges.Should().HaveCount(27);
        holder.WallEdgeLevels.Should().BeEquivalentTo(new[] { 1M });
        holder.GetWallEdges(1).Should().HaveCount(12);

        var cutter = new WallGraphCutter(Mock.Of<ILogger<WallGraphCutter>>());
        // passing empty points dict - no door
        cutter.Run(holder, new Dictionary<long, OsmPoint>());

        // verifying the result
        holder.Nodes
            .Should()
            .HaveCount(10 + 24, "should have added new nodes, on each side of each node");
        holder.Edges.Should().HaveCount(40, "new edges created on each side of the wall");
        holder.Edges
            .Where(x => holder.Nodes[(int)x.FromId].Source == holder.Nodes[(int)x.ToId].Source)
            .Should()
            .BeEmpty();
    }

    private static readonly GeometryFactory Gf = new(new(), 4326);
}
