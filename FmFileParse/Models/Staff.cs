using System.Reflection;
using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Staff : BaseData
{
    internal static readonly PropertyInfo[] OverridableProperties =
        typeof(Staff).GetProperties().Where(p => p.Name != nameof(Contract) && p.CanWrite).ToArray();

    public Contract? Contract { get; set; }

    [DataPosition(97)]
    public int StaffPlayerId { get; set; }

    [DataPosition(4)]
    public int FirstNameId { get; set; }

    [DataPosition(8)]
    public int LastNameId { get; set; }

    [DataPosition(12)]
    public int CommonNameId { get; set; }

    [DataPosition(16)]
    public DateTime DateOfBirth { get; set; }

    [DataPosition(26)]
    public int CountryId { get; set; }

    [DataPosition(30)]
    public int SecondaryCountryId { get; set; }

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
}
