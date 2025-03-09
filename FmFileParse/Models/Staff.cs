using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Staff : BaseData
{
    public Contract? Contract { get; set; }

    public int StaffPlayerId { get; set; }

    public int FirstNameId { get; set; }

    public int SecondNameId { get; set; }

    public int CommonNameId { get; set; }

    public DateTime DOB { get; set; }

    public int NationId { get; set; }

    public int SecondaryNationId { get; set; }

    public byte InternationalCaps { get; set; }

    public byte InternationalGoals { get; set; }

    public DateTime? ContractExpiryDate { get; set; }

    public int Wage { get; set; }

    public int Value { get; set; }

    public int ClubId { get; set; }

    public byte Adaptability { get; set; }

    public byte Ambition { get; set; }

    public byte Determination { get; set; }

    public byte Loyalty { get; set; }

    public byte Pressure { get; set; }

    public byte Professionalism { get; set; }

    public byte Sportsmanship { get; set; }

    public byte Temperament { get; set; }

    internal static Staff Convert(byte[] source)
    {
        return new Staff
        {
            Id = ByteHandler.GetIntFromBytes(source, 0),
            StaffPlayerId = ByteHandler.GetIntFromBytes(source, 97),
            FirstNameId = ByteHandler.GetIntFromBytes(source, 4),
            SecondNameId = ByteHandler.GetIntFromBytes(source, 8),
            CommonNameId = ByteHandler.GetIntFromBytes(source, 12),
            DOB = ByteHandler.GetDateFromBytes(source, 16) ?? new DateTime(1985, 1, 1),
            NationId = ByteHandler.GetIntFromBytes(source, 26),
            SecondaryNationId = ByteHandler.GetIntFromBytes(source, 30),
            InternationalCaps = ByteHandler.GetByteFromBytes(source, 34),
            InternationalGoals = ByteHandler.GetByteFromBytes(source, 35),
            ContractExpiryDate = ByteHandler.GetDateFromBytes(source, 70),
            Wage = (int)(ByteHandler.GetIntFromBytes(source, 78) * SaveGameData.ValueMultiplier),
            Value = (int)(ByteHandler.GetIntFromBytes(source, 82) * SaveGameData.ValueMultiplier),
            ClubId = ByteHandler.GetIntFromBytes(source, 57),
            Adaptability = ByteHandler.GetByteFromBytes(source, 86),
            Ambition = ByteHandler.GetByteFromBytes(source, 87),
            Determination = ByteHandler.GetByteFromBytes(source, 88),
            Loyalty = ByteHandler.GetByteFromBytes(source, 89),
            Pressure = ByteHandler.GetByteFromBytes(source, 90),
            Professionalism = ByteHandler.GetByteFromBytes(source, 91),
            Sportsmanship = ByteHandler.GetByteFromBytes(source, 92),
            Temperament = ByteHandler.GetByteFromBytes(source, 93)
        };
    }
}
