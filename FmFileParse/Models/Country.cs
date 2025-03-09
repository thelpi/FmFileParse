using FmFileParse.Models.Attributes;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Country : BaseData
{
    [DataPosition(4, Length = 50)]
    public string Name { get; set; } = string.Empty;

    internal static Country Convert(byte[] source)
    {
        var country = new Country();

        DataPositionAttributeParser.SetDataPositionableProperties(country, source);

        return country;
    }
}
