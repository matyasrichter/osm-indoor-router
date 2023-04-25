namespace GraphBuilding.Tests;

using FluentAssertions.Primitives;

public class GraphHolderAssertions : ReferenceTypeAssertions<GraphHolder, GraphHolderAssertions>
{
    [CustomAssertion]
    public AndConstraint<GraphHolderAssertions> HaveEdgesBetweenSourceIds(
        (long Id, decimal Level) nodeId,
        IEnumerable<(long OtherId, decimal Level)> expected
    )
    {
        Subject.Edges
            .Join(
                Subject.Nodes.Select((x, i) => (x, i)),
                x => x.FromId,
                x => x.i,
                (x, y) => (Edge: x, FromSId: y.x.SourceId, FromLevel: y.x.Level)
            )
            .Join(
                Subject.Nodes.Select((x, i) => (x, i)),
                x => x.Edge.ToId,
                x => x.i,
                (x, y) => (x.Edge, x.FromSId, x.FromLevel, ToSId: y.x.SourceId, ToLevel: y.x.Level)
            )
            .Where(
                x =>
                    (x.FromSId == nodeId.Id && x.FromLevel == nodeId.Level)
                    || (x.ToSId == nodeId.Id && x.ToLevel == nodeId.Level)
            )
            .Select(x => x.FromSId == nodeId.Id ? (x.ToSId, x.ToLevel) : (x.FromSId, x.FromLevel))
            .Should()
            .BeEquivalentTo(expected, o => o.WithoutStrictOrdering());
        return new(this);
    }

    public GraphHolderAssertions(GraphHolder subject)
        : base(subject) { }

    protected override string Identifier => "GraphHolderAssertion";
}

public static class GraphHolderAssertionsExtensions
{
    public static GraphHolderAssertions Should(this GraphHolder instance) => new(instance);
}
