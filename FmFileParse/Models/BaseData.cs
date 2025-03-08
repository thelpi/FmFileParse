namespace FmFileParse.Models;

public abstract class BaseData
{
    [DataFileInfo(0)]
    public int Id { get; set; }
}
