using FmFileParse.Import.DataClasses;

namespace FmFileParse.DataClasses;

public class ClubComp : BaseData
{
    [DataFileInfo(56, 25)]
    public string Name { get; set; } = string.Empty;

    [DataFileInfo(4, 50)]
    public string LongName { get; set; } = string.Empty;

    [DataFileInfo(83, 3)]
    public string Abbreviation { get; set; } = string.Empty;

    [DataFileInfo(93)]
    public int NationId { get; set; }
}
