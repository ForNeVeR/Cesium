namespace Cesium.Preprocessor;

public static class TokenIncludeStackExtensions
{
    public static IEnumerable<T> TakeWhileWithLastInclude<T>(this Stack<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        var result = new List<T>();
        foreach (var item in source)
        {
            if (predicate(item))
            {
                result.Add(item);
            }
            else
            {
                result.Add(item);
                break;
            }
        }

        return result;
    }

    public static void PopWhileWithLastInclude<T>(this Stack<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        while (source.TryPeek(out var lastItem))
        {
            if (predicate(lastItem))
            {
                source.Pop();
                continue;
            }
            source.Pop();
            break;
        }
    }
}
