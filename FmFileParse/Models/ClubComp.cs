namespace FmFileParse.Models;

public class ClubComp : BaseData
{
    public string Name { get; set; } = string.Empty;

    public string LongName { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public int NationId { get; set; }
}
