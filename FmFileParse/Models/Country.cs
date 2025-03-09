using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Country : BaseData
{
    public string Name { get; set; } = string.Empty;

    internal static Country Convert(byte[] source)
    {
        return new Country
        {
            Id = ByteHandler.GetIntFromBytes(source, 0),
            Name = ByteHandler.GetStringFromBytes(source, 4, 50)
        };
    }
}
