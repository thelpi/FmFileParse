using FmFileParse.DataClasses;

namespace FmFileParse.Models;

public class Club : BaseData
{
    [DataFileInfo(4, 50)]
    public string LongName { get; set; } = string.Empty;

    [DataFileInfo(56, 25)]
    public string Name { get; set; } = string.Empty;

    [DataFileInfo(83)]
    public int NationId { get; set; }

    [DataFileInfo(87)]
    public int DivisionId { get; set; }

    [DataFileInfo(128)]
    public short Reputation { get; set; }
}
