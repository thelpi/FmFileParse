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
}
