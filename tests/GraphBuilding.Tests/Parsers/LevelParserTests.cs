namespace GraphBuilding.Tests.Parsers;

using GraphBuilding.Parsers;
using Microsoft.Extensions.Logging;

public class LevelParserTests
{
    // test data source: https://taginfo.openstreetmap.org/keys/level#values
    public static TheoryData<string, IEnumerable<decimal>> ParsingData =>
        new()
        {
            { "", new[] { 0m } },
            { "0", new[] { 0m } },
            { "G", new[] { 0m } },
            { "1", new[] { 1m } },
            { "1.0", new[] { 1m } },
            { "+1", new[] { 1m } },
            { "2", new[] { 2m } },
            { "-1", new[] { -1m } },
            { "0;1", new[] { 0m, 1m } },
            { "-1;2;5", new[] { -1m, 2m, 5m } },
            { "2-4", new[] { 2m, 3m, 4m } },
            { "0;2-3", new[] { 0m, 2m, 3m } },
            { "-1-1", new[] { -1m, 0m, 1m } },
            { "-3--1", new[] { -3m, -2m, -1m } },
            { "0.5", new[] { 0.5m } },
            { ".5", new[] { 0.5m } },
            { "-0.5", new[] { -0.5m } },
            { "-.5", new[] { -0.5m } },
            { "-1.5;-0.5", new[] { -1.5m, -0.5m } },
            { "-1.5--0.5", new[] { -1.5m, -0.5m } }
        };

    [Theory]
    [MemberData(nameof(ParsingData))]
    public void Parses(string input, IEnumerable<decimal> expected) =>
        new LevelParser(new Mock<ILogger<LevelParser>>().Object)
            .Parse(input)
            .Should()
            .BeEquivalentTo(expected);
}
