namespace GraphBuilding;

using NetTopologySuite.Geometries;
using Ports;
using Settings;

public class MapProcessor
{
    private readonly IOsmPort osm;
    private readonly IGraphSavingPort savingPort;
    private readonly AppSettings settings;

    public MapProcessor(IOsmPort osm, IGraphSavingPort savingPort, AppSettings settings)
    {
        this.osm = osm;
        this.savingPort = savingPort;
        this.settings = settings;
    }

    public async Task Process(CancellationToken ct)
    {
        var version = await savingPort.AddVersion();
        if (ct.IsCancellationRequested)
        {
            return;
        }

        var graphBuilder = new GraphBuilder(version);
        await BuildGraph(graphBuilder, ct);

        if (ct.IsCancellationRequested)
        {
            return;
        }

        await savingPort.FinalizeVersion(version);
    }

    private async Task BuildGraph(GraphBuilder builder, CancellationToken ct)
    {
        var lines = await osm.GetLines(settings.Bbox.AsRectangle());
        foreach (var line in lines)
        {
            if (line.Tags.ContainsKey("highway"))
            {
                Node? prev = null;
                var edgesToInsert = await CreateEdgesFromLine(builder, line, prev).ToListAsync(ct);

                var edges = await savingPort.SaveEdges(edgesToInsert);
                foreach (var e in edges)
                {
                    builder = builder.AddEdge(e);
                }
            }
        }
    }

    private async IAsyncEnumerable<InsertedEdge> CreateEdgesFromLine(
        GraphBuilder builder,
        OsmLine line,
        Node? prev
    )
    {
        foreach (var (coord, nodeOsmId) in line.Geometry.Coordinates.Zip(line.Nodes))
        {
            var node = await GetOrCreateNode(builder, nodeOsmId, coord);

            if (node is not null && prev is not null)
            {
                var distance = prev.Coordinates.Distance(node.Coordinates);
                yield return new(builder.Version, prev.Id, node.Id, distance, distance, line.WayId);
            }

            prev = node;
        }
    }

    private async Task<Node?> GetOrCreateNode(
        GraphBuilder builder,
        long nodeOsmId,
        Coordinate coord
    )
    {
        if (!builder.HasNodeBySourceId(nodeOsmId))
        {
            var osmNode = await osm.GetPointByOsmId(nodeOsmId);
            if (osmNode is null)
            {
                return null;
            }

            var node = await savingPort.SaveNode(new(builder.Version, new(coord), 0, nodeOsmId));
            _ = builder.AddNode(node);
            return node;
        }

        return builder.GetNodeBySourceId(nodeOsmId)!;
    }
}
