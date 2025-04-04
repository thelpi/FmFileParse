using System.Text;

namespace FmFileParse.SaveImport;

internal static class StringHandler
{
    internal static readonly Encoding Encoding = Encoding.Latin1;

    internal static string StringGet(string s, int pos, int len)
        => s.Substring(pos, len).Replace("\0", string.Empty);

    internal static int IntGet(string s, int pos)
        => BitConverter.ToInt32(Encoding.GetBytes(s.Substring(pos, 4)), 0);

    internal static short ShortGet(string s, int pos)
        => BitConverter.ToInt16(Encoding.GetBytes(s.Substring(pos, 2)), 0);

    internal static byte ByteGet(string s, int pos)
        => Encoding.GetBytes(s.Substring(pos, 1))[0];

    internal static bool BoolGet(string s, int pos)
        => s.Substring(pos, 1).ToCharArray()[0] == 'ÿ';

    internal static double DoubleGet(string s, int pos)
        => BitConverter.ToDouble(Encoding.GetBytes(s.Substring(pos, 8)), 0);

    internal static DateTime? DateGet(string s, int pos)
    {
        var year = ShortGet(s, pos + 2);
        var days = ShortGet(s, pos) + 1;

        // TODO: mutualize with the "ByteHandler" version
        // might be bugged
        return year <= 0
            ? null
            : new DateTime(year, 1, 1).AddDays(days);
    }

    internal static List<string> ExtractFileData(string datFileTemplatePath, string fileName, int splitPosition)
    {
        using var sr = new StreamReader(string.Format(datFileTemplatePath, fileName), Encoding);

        var data = sr.ReadToEnd();

        var dataCollection = new List<string>((data.Length / splitPosition) + 1);

        var posInTxt = 0;
        while (posInTxt < data.Length)
        {
            var rawData = data.Substring(posInTxt, splitPosition);
            if (!string.IsNullOrWhiteSpace(rawData))
            {
                dataCollection.Add(rawData);
            }
            posInTxt += splitPosition;
        }

        return dataCollection;
    }
}
