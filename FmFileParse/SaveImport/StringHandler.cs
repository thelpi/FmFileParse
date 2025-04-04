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
        var days = ShortGet(s, pos);

        // TODO: mutualize with the "ByteHandler" version
        return year <= 1900
            ? null
            : new DateTime(year, 1, 1).AddDays(days);
    }

    internal static List<string> ExtractFileData(string datFileTemplatePath, string fileName, int splitPosition, out int endPosition, int startAt = 0, bool stopAtIdBreak = false)
    {
        endPosition = -1;

        using var sr = new StreamReader(string.Format(datFileTemplatePath, fileName), Encoding);

        var data = sr.ReadToEnd()[startAt..];

        var dataCollection = new List<string>((data.Length / splitPosition) + 1);

        var previousId = -1;
        var posInTxt = 0;
        while (posInTxt < data.Length)
        {
            var rawData = data.Substring(posInTxt, splitPosition);
            if (!string.IsNullOrWhiteSpace(rawData))
            {
                if (stopAtIdBreak)
                {
                    var id = IntGet(rawData, 0);
                    if (id != previousId + 1)
                    {
                        endPosition = posInTxt;
                        break;
                    }
                    previousId = id;
                }
                dataCollection.Add(rawData);
            }
            posInTxt += splitPosition;
        }

        return dataCollection;
    }
}
