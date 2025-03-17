using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Club : BaseData
{
    [DataPosition(4, Length = 50)]
    public string LongName { get; set; } = string.Empty;

    [DataPosition(56, Length = 25)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(83)]
    public int NationId { get; set; }

    [DataPosition(87)]
    public int DivisionId { get; set; }

    [DataPosition(128)]
    public short Reputation { get; set; }

    [DataPosition(101)]
    public int Bank { get; set; }

    [DataPosition(127)]
    public byte Facilities { get; set; }

    [DataPosition(155)]
    public int LikedStaff1 { get; set; }

    [DataPosition(159)]
    public int LikedStaff2 { get; set; }

    [DataPosition(163)]
    public int LikedStaff3 { get; set; }

    [DataPosition(167)]
    public int DislikedStaff1 { get; set; }

    [DataPosition(171)]
    public int DislikedStaff2 { get; set; }

    [DataPosition(175)]
    public int DislikedStaff3 { get; set; }

    [DataPosition(179)]
    public int RivalClub1 { get; set; }

    [DataPosition(183)]
    public int RivalClub2 { get; set; }

    [DataPosition(187)]
    public int RivalClub3 { get; set; }
}
