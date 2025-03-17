namespace FmFileParse;

internal static class SystemHelper
{
    private static readonly int[] Months30Days = [4, 6, 9, 11];

    /// <summary>
    /// Shortcut to <see cref="Array.IndexOf(Array, object?)"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int IndexOf<T>(this T[] array, T value)
        => Array.IndexOf(array, value);

    /// <summary>
    /// Computes the average date of a list of dates.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static DateTime Average(this IEnumerable<DateTime> source)
    {
        var year = (int)Math.Round(source.Select(x => x.Year).Average());
        var month = (int)Math.Round(source.Select(x => x.Month).Average());
        var day = (int)Math.Round(source.Select(x => x.Day).Average());

        var maxDayFebruary = DateTime.IsLeapYear(year) ? 29 : 28;
        if (month == 2 && day > maxDayFebruary)
        {
            day = maxDayFebruary;
        }
        else if (day == 31 && Months30Days.Contains(month))
        {
            day = 30;
        }

        return new DateTime(year, month, day);
    }

    public static IGrouping<OutT, InT>? GetMaxOccurence<InT, OutT>(
        this IEnumerable<InT> collection,
        Func<InT, OutT> keySelector)
        => collection.GroupBy(keySelector).OrderByDescending(x => x.Count()).FirstOrDefault();
}
