using FmFileParse.Models.Attributes;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Country : BaseData
{
    [DataPosition(113)]
    public short ConfederationId { get; set; }

    [DataPosition(4, Length = 50)]
    public string Name { get; set; } = string.Empty;

    // 2 for EU
    [DataPosition(127)]
    public byte IsEu { get; set; }

    [DataPosition(83, Length = 3)]
    public string Acronym { get; set; } = string.Empty;

    [DataPosition(133)]
    public byte LeagueStandard { get; set; }

    [DataPosition(142)]
    public short Reputation { get; set; }

    internal static Country Convert(byte[] source)
    {
        var country = new Country();

        DataPositionAttributeParser.SetDataPositionableProperties(country, source);

        return country;
    }
}
