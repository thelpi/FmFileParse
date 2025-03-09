using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public abstract class BaseData
{
    [DataPosition(0)]
    public int Id { get; set; }
}
