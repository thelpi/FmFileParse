using FmFileParse.SaveImport;

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

    internal static new Player Convert(byte[] source)
    {
        return new Player
        {
            PlayerId = ByteHandler.GetIntFromBytes(source, 0),
            CurrentAbility = ByteHandler.GetShortFromBytes(source, 5),
            PotentialAbility = ByteHandler.GetShortFromBytes(source, 7),
            Reputation = ByteHandler.GetShortFromBytes(source, 9),
            DomesticReputation = ByteHandler.GetShortFromBytes(source, 11),
            WorldReputation = ByteHandler.GetShortFromBytes(source, 13),
            GK = ByteHandler.GetByteFromBytes(source, 15),
            SW = ByteHandler.GetByteFromBytes(source, 16),
            DF = ByteHandler.GetByteFromBytes(source, 17),
            DM = ByteHandler.GetByteFromBytes(source, 18),
            MF = ByteHandler.GetByteFromBytes(source, 19),
            AM = ByteHandler.GetByteFromBytes(source, 20),
            ST = ByteHandler.GetByteFromBytes(source, 21),
            WingBack = ByteHandler.GetByteFromBytes(source, 22),
            Left = ByteHandler.GetByteFromBytes(source, 24),
            Right = ByteHandler.GetByteFromBytes(source, 23),
            Centre = ByteHandler.GetByteFromBytes(source, 25),
            FreeRole = ByteHandler.GetByteFromBytes(source, 26),
            Acceleration = ByteHandler.GetByteFromBytes(source, 27),
            Aggression = ByteHandler.GetByteFromBytes(source, 28),
            Agility = ByteHandler.GetByteFromBytes(source, 29),
            Anticipation = ByteHandler.GetByteFromBytes(source, 30), //x
            Balance = ByteHandler.GetByteFromBytes(source, 31),
            Bravery = ByteHandler.GetByteFromBytes(source, 32),
            Consistency = ByteHandler.GetByteFromBytes(source, 33),
            Corners = ByteHandler.GetByteFromBytes(source, 34),
            Creativity = ByteHandler.GetByteFromBytes(source, 67), //x
            Crossing = ByteHandler.GetByteFromBytes(source, 35), //x
            Decisions = ByteHandler.GetByteFromBytes(source, 36), //x
            Dirtiness = ByteHandler.GetByteFromBytes(source, 37),
            Dribbling = ByteHandler.GetByteFromBytes(source, 38), //x
            Finishing = ByteHandler.GetByteFromBytes(source, 39), //x
            Flair = ByteHandler.GetByteFromBytes(source, 40),
            FreeKicks = ByteHandler.GetByteFromBytes(source, 41),
            Handling = ByteHandler.GetByteFromBytes(source, 42), //x
            Heading = ByteHandler.GetByteFromBytes(source, 43), //x
            ImportantMatches = ByteHandler.GetByteFromBytes(source, 44),
            Influence = ByteHandler.GetByteFromBytes(source, 47),
            InjuryProneness = ByteHandler.GetByteFromBytes(source, 45),
            Jumping = ByteHandler.GetByteFromBytes(source, 46),
            LongShots = ByteHandler.GetByteFromBytes(source, 49), //x
            Marking = ByteHandler.GetByteFromBytes(source, 50), //x
            NaturalFitness = ByteHandler.GetByteFromBytes(source, 52),
            OffTheBall = ByteHandler.GetByteFromBytes(source, 51), //x
            OneOnOnes = ByteHandler.GetByteFromBytes(source, 53), //x
            Pace = ByteHandler.GetByteFromBytes(source, 54),
            Passing = ByteHandler.GetByteFromBytes(source, 55), //x
            Penalties = ByteHandler.GetByteFromBytes(source, 56), //x
            Positioning = ByteHandler.GetByteFromBytes(source, 57), //x
            Reflexes = ByteHandler.GetByteFromBytes(source, 58), //x
            Stamina = ByteHandler.GetByteFromBytes(source, 60),
            Strength = ByteHandler.GetByteFromBytes(source, 61),
            Tackling = ByteHandler.GetByteFromBytes(source, 62), //x
            Teamwork = ByteHandler.GetByteFromBytes(source, 63),
            Technique = ByteHandler.GetByteFromBytes(source, 64),
            ThrowIns = ByteHandler.GetByteFromBytes(source, 65), //x
            Versatility = ByteHandler.GetByteFromBytes(source, 66),
            WorkRate = ByteHandler.GetByteFromBytes(source, 68),
            LeftFoot = ByteHandler.GetByteFromBytes(source, 48),
            RightFoot = ByteHandler.GetByteFromBytes(source, 59)
        };
    }

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
