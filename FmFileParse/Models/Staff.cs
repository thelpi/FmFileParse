﻿using System.Reflection;
using FmFileParse.Models.Attributes;
using FmFileParse.Models.Internal;

namespace FmFileParse.Models;

public class Staff : BaseData
{
    internal static readonly PropertyInfo[] OverridableProperties =
        typeof(Staff).GetProperties().Where(p => p.Name != nameof(Contract) && p.CanWrite).ToArray();

    public Contract? Contract { get; set; }

    [DataPosition(97, FileType = PositionAttributeFileTypes.SaveFileOnly)]
    public int SaveStaffPlayerId { get; set; }

    [DataPosition(145, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DbStaffPlayerId { get; set; }

    [DataPosition(4)]
    public int FirstNameId { get; set; }

    [DataPosition(8)]
    public int LastNameId { get; set; }

    [DataPosition(12)]
    public int CommonNameId { get; set; }

    [DataPosition(16)]
    public DateTime DateOfBirth { get; set; }

    [DataPosition(26)]
    public int NationId { get; set; }

    [DataPosition(30)]
    public int SecondaryNationId { get; set; }

    [DataPosition(34)]
    public byte InternationalCaps { get; set; }

    [DataPosition(35)]
    public byte InternationalGoals { get; set; }

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

    [DataPosition(24, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public short YearOfBirth { get; set; }

    [DataPosition(70, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public DateTime? DateContractEnd { get; set; }

    [DataPosition(97, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int FavClub1 { get; set; }

    [DataPosition(101, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int FavClub2 { get; set; }

    [DataPosition(105, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int FavClub3 { get; set; }

    [DataPosition(109, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DislikeClub1 { get; set; }

    [DataPosition(113, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DislikeClub2 { get; set; }

    [DataPosition(117, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DislikeClub3 { get; set; }

    [DataPosition(121, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int FavStaff1 { get; set; }

    [DataPosition(125, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int FavStaff2 { get; set; }

    [DataPosition(129, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int FavStaff3 { get; set; }

    [DataPosition(133, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DislikeStaff1 { get; set; }

    [DataPosition(137, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DislikeStaff2 { get; set; }

    [DataPosition(141, FileType = PositionAttributeFileTypes.DbFileOnly)]
    public int DislikeStaff3 { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string CommonName { get; set; } = string.Empty;

    public int ActualYearOfBirth => DateOfBirth.Year > 1900
        ? DateOfBirth.Year
        : YearOfBirth;

    public string Fullname => !string.IsNullOrWhiteSpace(CommonName)
        ? CommonName
        : string.Concat(LastName, ", ", FirstName);

    public DateTime? ActualDateOfBirth => DateOfBirth.Year > 1900
        ? DateOfBirth
        : (YearOfBirth > 1900
            ? new DateTime(YearOfBirth, 7, 1)
            : null);

    public override IEnumerable<string> Describe(BaseFileData data) => [];
}
