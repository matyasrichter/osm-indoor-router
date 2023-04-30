namespace Core.Tests;

using Core;

public class EnumerableExtensionsTests
{
    public static TheoryData<IEnumerable<int>, IEnumerable<(int, int)>> GetPairsIntData() =>
        new()
        {
            { new[] { 10, 20, 30 }, new[] { (10, 20), (10, 30), (30, 20) } },
            { new[] { -1, 0, 1, 2 }, new[] { (-1, 0), (-1, 1), (-1, 2), (0, 1), (0, 2), (1, 2) } }
        };

    [Theory]
    [MemberData(nameof(GetPairsIntData))]
    public void TestGetPairsForValueTypes(IEnumerable<int> source, IEnumerable<(int, int)> expected)
    {
        var result = source.GetPairs().ToList();
        var expectedL = expected.ToList();
        result.Should().HaveSameCount(expectedL);
        result
            // using hashsets because we don't care about pair order
            .Select(x => new HashSet<int>() { x.Item1, x.Item2 })
            .Should()
            .BeEquivalentTo(expectedL.Select(x => new HashSet<int>() { x.Item1, x.Item2 }));
    }

    private sealed record TestR(int Value);

    [Theory]
    [MemberData(nameof(GetPairsIntData))]
    public void TestGetPairsForReferenceTypes(
        IEnumerable<int> source,
        IEnumerable<(int, int)> expected
    )
    {
        var result = source.Select(x => new TestR(x)).GetPairs().ToList();
        var expectedL = expected.Select(x => (new TestR(x.Item1), new TestR(x.Item2))).ToList();
        result.Should().HaveSameCount(expectedL);
        result
            // using hashsets because we don't care about pair order
            .Select(x => new HashSet<TestR>() { x.Item1, x.Item2 })
            .Should()
            .BeEquivalentTo(expectedL.Select(x => new HashSet<TestR>() { x.Item1, x.Item2 }));
    }
}
