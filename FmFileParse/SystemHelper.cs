namespace FmFileParse;

internal static class SystemHelper
{
    private static readonly int[] Months30Days = [4, 6, 9, 11];

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

    /// <summary>
    /// Searches, into <paramref name="collection"/>, the value from the <paramref name="keySelector"/> with the most occurences.
    /// </summary>
    /// <typeparam name="InT"></typeparam>
    /// <typeparam name="OutT"></typeparam>
    /// <param name="collection">A not empty collection.</param>
    /// <param name="keySelector"></param>
    /// <returns>The value with the most occurences, and the items related to this value.</returns>
    public static IGrouping<OutT, InT> GetMaxOccurence<InT, OutT>(
        this IEnumerable<InT> collection,
        Func<InT, OutT> keySelector)
        => collection.GroupBy(keySelector).OrderByDescending(x => x.Count()).First();

    /// <summary>
    /// Parses a nullable object into <see cref="DBNull.Value"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="otherDbNullValues"></param>
    /// <returns></returns>
    public static object DbNullIf(
        this object? value,
        params object[] otherDbNullValues)
        => value is null || otherDbNullValues.Contains(value) ? DBNull.Value : value;
}
