using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class ClubComp : BaseData
{
    public string Name { get; set; } = string.Empty;

    public string LongName { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public int NationId { get; set; }

    public byte Reputation { get; set; }

    internal static ClubComp Convert(byte[] source)
    {
        return new ClubComp
        {
            Id = ByteHandler.GetIntFromBytes(source, 0),
            LongName = ByteHandler.GetStringFromBytes(source, 4, 50),
            Name = ByteHandler.GetStringFromBytes(source, 56, 25),
            Reputation = ByteHandler.GetByteFromBytes(source, 82),
            NationId = ByteHandler.GetIntFromBytes(source, 93),
            Abbreviation = ByteHandler.GetStringFromBytes(source, 83, 3)
        };
    }
}
