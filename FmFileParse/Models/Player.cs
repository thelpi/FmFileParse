namespace FmFileParse.Models;

public class Player : Staff
{
    public int PlayerId { get; set; }

    public short CurrentAbility { get; set; }

    public short PotentialAbility { get; set; }

    public short Reputation { get; set; }

    public short DomesticReputation { get; set; }

    public short WorldReputation { get; set; }

    public byte GK { get; set; }

    public byte SW { get; set; }

    public byte DF { get; set; }

    public byte DM { get; set; }

    public byte MF { get; set; }

    public byte AM { get; set; }

    public byte ST { get; set; }

    public byte WingBack { get; set; }

    public byte Left { get; set; }

    public byte Right { get; set; }

    public byte Centre { get; set; }

    public byte FreeRole { get; set; }

    public byte LeftFoot { get; set; }

    public byte RightFoot { get; set; }

    public byte Acceleration { get; set; }

    public byte Agility { get; set; }

    public byte Balance { get; set; }

    public byte Jumping { get; set; }

    public byte Pace { get; set; }

    public byte Stamina { get; set; }

    public byte Strength { get; set; }

    public byte Aggression { get; set; }

    public byte Bravery { get; set; }

    public byte Consistency { get; set; }

    public byte Flair { get; set; }

    public byte ImportantMatches { get; set; }

    public byte Influence { get; set; }

    public byte Teamwork { get; set; }

    public byte WorkRate { get; set; }

    // intrinsic
    public byte Anticipation { get; set; }

    // intrinsic
    public byte Creativity { get; set; }

    // intrinsic
    public byte Crossing { get; set; }

    // intrinsic
    public byte Decisions { get; set; }

    // intrinsic
    public byte Dribbling { get; set; }

    // intrinsic
    public byte Finishing { get; set; }

    // intrinsic
    public byte Handling { get; set; }

    // intrinsic
    public byte Heading { get; set; }

    // intrinsic
    public byte LongShots { get; set; }

    // intrinsic
    public byte Marking { get; set; }

    // intrinsic
    public byte OffTheBall { get; set; }

    // intrinsic
    public byte OneOnOnes { get; set; }

    // intrinsic
    public byte Passing { get; set; }

    // intrinsic
    public byte Positioning { get; set; }

    // intrinsic
    public byte Reflexes { get; set; }

    // intrinsic
    public byte Tackling { get; set; }

    public byte Technique { get; set; }

    public byte Corners { get; set; }

    public byte Dirtiness { get; set; }

    public byte FreeKicks { get; set; }

    public byte InjuryProneness { get; set; }

    public byte NaturalFitness { get; set; }

    // intrinsic
    public byte Penalties { get; set; }

    // intrinsic
    public byte ThrowIns { get; set; }

    public byte Versatility { get; set; }

    internal void PopulateStaffPropertiers(Staff staff, Contract? contract)
    {
        Adaptability = staff.Adaptability;
        Ambition = staff.Ambition;
        ClubId = staff.ClubId;
        CommonNameId = staff.CommonNameId;
        ContractExpiryDate = staff.ContractExpiryDate;
        Determination = staff.Determination;
        DOB = staff.DOB;
        FirstNameId = staff.FirstNameId;
        Id = staff.Id;
        InternationalCaps = staff.InternationalCaps;
        InternationalGoals = staff.InternationalGoals;
        Loyalty = staff.Loyalty;
        NationId = staff.NationId;
        Pressure = staff.Pressure;
        Professionalism = staff.Professionalism;
        SecondaryNationId = staff.SecondaryNationId;
        SecondNameId = staff.SecondNameId;
        Sportsmanship = staff.Sportsmanship;
        StaffPlayerId = staff.StaffPlayerId;
        Temperament = staff.Temperament;
        Value = staff.Value;
        Wage = staff.Wage;
        Contract = contract;
    }
}
