namespace GraphBuilding.Parsers;

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

public partial class LevelParser
{
    private readonly Regex rangeRegex = RangeRegex();
    private readonly ILogger<LevelParser> logger;

    public LevelParser(ILogger<LevelParser> logger) => this.logger = logger;

    [GeneratedRegex(@"^(?<LowerBound>-?[.0-9]+)-(?<UpperBound>-?[.0-9]+)$")]
    private static partial Regex RangeRegex();

    public IEnumerable<decimal> Parse(string str)
    {
        str = str.Trim();
        if (string.IsNullOrEmpty(str))
        {
            return new[] { 0m };
        }

        return str.Split(";").SelectMany(ParseWithoutSemicolons).Distinct();
    }

    private IEnumerable<decimal> ParseWithoutSemicolons(string str)
    {
        var rangeMatch = rangeRegex.Match(str);
        if (rangeMatch.Success && rangeMatch.Groups["LowerBound"].Success && rangeMatch.Groups["UpperBound"].Success)
        {
            return ParseRange(str, rangeMatch);
        }

        if (!decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            LogSingleValueParseFailure(str);
            return new[] { 0m };
        }

        return new[] { result };
    }

    private IEnumerable<decimal> ParseRange(string str, Match rangeMatch)
    {
        // we're dealing with a range
        if (!decimal.TryParse(rangeMatch.Groups["LowerBound"].Value,
                NumberStyles.Number, CultureInfo.InvariantCulture, out var lowerBound))
        {
            LogLowerBoundParseFailure(str);
            return new[] { 0m };
        }

        if (!decimal.TryParse(rangeMatch.Groups["UpperBound"].Value,
                NumberStyles.Number, CultureInfo.InvariantCulture, out var upperBound))
        {
            LogUpperBoundParseFailure(str);
            return new[] { 0m };
        }

        if (lowerBound > upperBound)
        {
            LogLowerBoundHigher(str);
            return new[] { 0m };
        }

        var results = new List<decimal>();
        while (lowerBound <= upperBound)
        {
            results.Add(lowerBound);
            lowerBound++;
        }

        return results;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to parse lower bound from {Input}")]
    private partial void LogLowerBoundParseFailure(string input);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to parse upper bound from {Input}")]
    private partial void LogUpperBoundParseFailure(string input);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to parse bounds: lower>upper from {Input}")]
    private partial void LogLowerBoundHigher(string input);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to parse single value from {Input}")]
    private partial void LogSingleValueParseFailure(string input);
}
