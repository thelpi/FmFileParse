using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Club : BaseData
{
    public string LongName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int NationId { get; set; }

    public int DivisionId { get; set; }

    public short Reputation { get; set; }

    internal static Club Convert(byte[] source)
    {
        return new Club
        {
            Id = ByteHandler.GetIntFromBytes(source, 0),
            LongName = ByteHandler.GetStringFromBytes(source, 4, 50),
            Name = ByteHandler.GetStringFromBytes(source, 56, 25),
            NationId = ByteHandler.GetIntFromBytes(source, 83),
            DivisionId = ByteHandler.GetIntFromBytes(source, 87),
            Reputation = ByteHandler.GetShortFromBytes(source, 128)
        };
    }
}
