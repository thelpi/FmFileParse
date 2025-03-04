namespace FmFileParse;

internal static class SystemHelpers
{
    /// <summary>
    /// Computes an average date from a bunch of dates.
    /// </summary>
    /// <param name="dates"></param>
    /// <returns></returns>
    internal static DateTime GetAverageDate(this IEnumerable<DateTime> dates)
    {
        var count = dates.Count();
        var temp = 0D;
        for (var i = 0; i < count; i++)
        {
            temp += dates.ElementAt(i).Ticks / (double)count;
        }
        return new DateTime((long)temp);
    }
}
