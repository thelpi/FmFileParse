using FmFileParse.DataClasses;

namespace FmFileParse.Import.DataClasses;

public abstract class BaseData
{
    [DataFileInfo(0)]
    public int Id { get; set; }
}
