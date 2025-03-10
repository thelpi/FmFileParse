using FmFileParse.Models.Attributes;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Confederation : BaseData
{
    [DataPosition(4, Length = 50)]
    public string ContinentName { get; set; } = string.Empty;

    [DataPosition(61, Length = 100)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(163, Length = 10)]
    public string Acronym { get; set; } = string.Empty;

    internal static Confederation Convert(byte[] source)
    {
        var confederation = new Confederation();

        DataPositionAttributeParser.SetDataPositionableProperties(confederation, source);

        return confederation;
    }
}
