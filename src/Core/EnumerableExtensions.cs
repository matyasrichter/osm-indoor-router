namespace Core;

public static class EnumerableExtensions
{
    public static IEnumerable<(T, T)> GetPairs<T>(this IEnumerable<T> source)
    {
        var list = source.ToList();
        for (var i = 0; i < list.Count - 1; i++)
            for (var j = i + 1; j < list.Count; j++)
                yield return (list[i], list[j]);
    }
}
