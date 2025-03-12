using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Club : BaseData
{
    [DataPosition(4, Length = 50)]
    public string LongName { get; set; } = string.Empty;

    [DataPosition(56, Length = 25)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(83)]
    public int CountryId { get; set; }

    [DataPosition(87)]
    public int DivisionId { get; set; }

    [DataPosition(128)]
    public short Reputation { get; set; }
}
