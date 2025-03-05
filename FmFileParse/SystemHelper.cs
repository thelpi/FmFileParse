namespace FmFileParse;

internal static class SystemHelper
{
    /// <summary>
    /// Gets the most represented value of a collection, and the number of occurences.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static (T value, int occurences) GetRepresentativeValue<T>(this IEnumerable<T> collection)
    {
        var group = collection.GroupBy(x => x).OrderByDescending(x => x.Count()).First();
        return (group.Key, group.Count());
    }

    /// <summary>
    /// Shortcut to <see cref="Array.IndexOf(Array, object?)"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int IndexOf<T>(this T[] array, T value)
        => Array.IndexOf(array, value);
}
