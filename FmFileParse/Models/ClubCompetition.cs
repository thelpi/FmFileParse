using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class ClubCompetition : BaseData
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
}
