namespace FmFileParse.Models;

public class Country : BaseData
{
    [DataFileInfo(4, 50)]
    public string Name { get; set; } = string.Empty;
}
