namespace GraphBuilding.Tests.Processors;

using ElementProcessors;
using FluentAssertions.Primitives;

public class ProcessingResultAssertions
    : ReferenceTypeAssertions<ProcessingResult, ProcessingResultAssertions>
{
    [CustomAssertion]
    public AndConstraint<ProcessingResultAssertions> HaveEdgesBetweenSourceIds(
        long nodeId,
        IEnumerable<long> expected
    )
    {
        Subject.Edges
            .Join(
                Subject.Nodes.Select((x, i) => (x, i)),
                x => x.FromId,
                x => x.i,
                (x, y) => (Edge: x, FromSId: y.x.SourceId)
            )
            .Join(
                Subject.Nodes.Select((x, i) => (x, i)),
                x => x.Edge.ToId,
                x => x.i,
                (x, y) => (x.Edge, x.FromSId, ToSId: y.x.SourceId)
            )
            .Where(x => x.FromSId == nodeId || x.ToSId == nodeId)
            .Select(x => new HashSet<long?>() { x.FromSId, x.ToSId })
            .Should()
            .BeEquivalentTo(expected.Select(x => new HashSet<decimal>() { nodeId, x }));
        return new(this);
    }

    [CustomAssertion]
    public AndConstraint<ProcessingResultAssertions> HaveEdgesBetweenLevels(
        IEnumerable<HashSet<decimal>> expected
    )
    {
        Subject.Edges
            .Join(
                Subject.Nodes.Select((x, i) => (x, i)),
                x => x.FromId,
                x => x.i,
                (x, y) => (Edge: x, FromLevel: y.x.Level)
            )
            .Join(
                Subject.Nodes.Select((x, i) => (x, i)),
                x => x.Edge.ToId,
                x => x.i,
                (x, y) => (x.Edge, x.FromLevel, ToLevel: y.x.Level)
            )
            .Select(x => new HashSet<decimal?>() { x.FromLevel, x.ToLevel })
            .Should()
            .BeEquivalentTo(expected);
        return new(this);
    }

    public ProcessingResultAssertions(ProcessingResult subject)
        : base(subject) { }

    protected override string Identifier => "ProcessingResultAssertion";
}

public static class ProcessingResultAssertionExtensions
{
    public static ProcessingResultAssertions Should(this ProcessingResult instance) =>
        new(instance);
}
