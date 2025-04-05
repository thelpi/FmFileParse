using FmFileParse.Models.Attributes;
using FmFileParse.Models.Internal;

namespace FmFileParse.Models;

public abstract class BaseData
{
    [DataPosition(0)]
    public int Id { get; set; }

    public abstract IEnumerable<string> Describe(BaseFileData data);
}
