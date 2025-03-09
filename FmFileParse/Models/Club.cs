namespace FmFileParse.Models;

public class Club : BaseData
{
    public string LongName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int NationId { get; set; }

    public int DivisionId { get; set; }

    public short Reputation { get; set; }
}
