using FmFileParse.Models.Attributes;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class ClubComp : BaseData
{
    [DataPosition(56, Length = 25)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(4, Length = 50)]
    public string LongName { get; set; } = string.Empty;

    [DataPosition(83, Length = 3)]
    public string Abbreviation { get; set; } = string.Empty;

    [DataPosition(93)]
    public int NationId { get; set; }

    [DataPosition(82)]
    public byte Reputation { get; set; }

    internal static ClubComp Convert(byte[] source)
    {
        var clubComp = new ClubComp();

        DataPositionAttributeParser.SetDataPositionableProperties(clubComp, source);

        return clubComp;
    }
}
