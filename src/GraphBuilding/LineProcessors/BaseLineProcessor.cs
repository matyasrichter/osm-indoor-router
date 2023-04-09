namespace GraphBuilding.LineProcessors;

using NetTopologySuite.Geometries;
using Ports;

public abstract class BaseLineProcessor
{
    protected static readonly GeometryFactory Gf = new(new(), 4326);
    protected IOsmPort Osm { get; }

    protected BaseLineProcessor(IOsmPort osm) => Osm = osm;
}
