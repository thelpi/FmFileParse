﻿using FmFileParse.Models.Attributes;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Staff : BaseData
{
    const string DefaultDateOfBirthString = "1985-1-1";

    public Contract? Contract { get; set; }

    [DataPosition(97)]
    public int StaffPlayerId { get; set; }

    [DataPosition(4)]
    public int FirstNameId { get; set; }

    [DataPosition(8)]
    public int SecondNameId { get; set; }

    [DataPosition(12)]
    public int CommonNameId { get; set; }

    [DataPosition(16, Default = DefaultDateOfBirthString)]
    public DateTime DOB { get; set; }

    [DataPosition(26)]
    public int NationId { get; set; }

    [DataPosition(30)]
    public int SecondaryNationId { get; set; }

    [DataPosition(34)]
    public byte InternationalCaps { get; set; }

    [DataPosition(35)]
    public byte InternationalGoals { get; set; }

    [DataPosition(70)]
    public DateTime? ContractExpiryDate { get; set; }

    [DataPosition(78)]
    public int Wage { get; set; }

    [DataPosition(82)]
    public int Value { get; set; }

    [DataPosition(57)]
    public int ClubId { get; set; }

    [DataPosition(86)]
    public byte Adaptability { get; set; }

    [DataPosition(87)]
    public byte Ambition { get; set; }

    [DataPosition(88)]
    public byte Determination { get; set; }

    [DataPosition(89)]
    public byte Loyalty { get; set; }

    [DataPosition(90)]
    public byte Pressure { get; set; }

    [DataPosition(91)]
    public byte Professionalism { get; set; }

    [DataPosition(92)]
    public byte Sportsmanship { get; set; }

    [DataPosition(93)]
    public byte Temperament { get; set; }

    internal static Staff Convert(byte[] source)
    {
        var staff = new Staff();

        DataPositionAttributeParser.SetDataPositionableProperties(staff, source);

        staff.Value = (int)(staff.Value * SaveGameData.ValueMultiplier);
        staff.Wage = (int)(staff.Wage * SaveGameData.ValueMultiplier);

        return staff;
    }
}
