using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Player : Staff
{
    [DataPosition(0)]
    public int PlayerId { get; set; }

    [DataPosition(5)]
    public short CurrentAbility { get; set; }

    [DataPosition(7)]
    public short PotentialAbility { get; set; }

    [DataPosition(9)]
    public short Reputation { get; set; }

    [DataPosition(11)]
    public short DomesticReputation { get; set; }

    [DataPosition(13)]
    public short WorldReputation { get; set; }

    [DataPosition(15)]
    public byte GK { get; set; }

    [DataPosition(16)]
    public byte SW { get; set; }

    [DataPosition(17)]
    public byte DF { get; set; }

    [DataPosition(18)]
    public byte DM { get; set; }

    [DataPosition(19)]
    public byte MF { get; set; }

    [DataPosition(20)]
    public byte AM { get; set; }

    [DataPosition(21)]
    public byte ST { get; set; }

    [DataPosition(22)]
    public byte WingBack { get; set; }

    [DataPosition(24)]
    public byte Left { get; set; }

    [DataPosition(23)]
    public byte Right { get; set; }

    [DataPosition(25)]
    public byte Centre { get; set; }

    [DataPosition(26)]
    public byte FreeRole { get; set; }

    [DataPosition(48)]
    public byte LeftFoot { get; set; }

    [DataPosition(59)]
    public byte RightFoot { get; set; }

    [DataPosition(27)]
    public byte Acceleration { get; set; }

    [DataPosition(29)]
    public byte Agility { get; set; }

    [DataPosition(31)]
    public byte Balance { get; set; }

    [DataPosition(46)]
    public byte Jumping { get; set; }

    [DataPosition(54)]
    public byte Pace { get; set; }

    [DataPosition(60)]
    public byte Stamina { get; set; }

    [DataPosition(61)]
    public byte Strength { get; set; }

    [DataPosition(28)]
    public byte Aggression { get; set; }

    [DataPosition(32)]
    public byte Bravery { get; set; }

    [DataPosition(33)]
    public byte Consistency { get; set; }

    [DataPosition(40)]
    public byte Flair { get; set; }

    [DataPosition(44)]
    public byte ImportantMatches { get; set; }

    [DataPosition(47)]
    public byte Influence { get; set; }

    [DataPosition(63)]
    public byte Teamwork { get; set; }

    [DataPosition(68)]
    public byte WorkRate { get; set; }

    [DataPosition(64)]
    public byte Technique { get; set; }

    [DataPosition(34)]
    public byte Corners { get; set; }

    [DataPosition(37)]
    public byte Dirtiness { get; set; }

    [DataPosition(41)]
    public byte FreeKicks { get; set; }

    [DataPosition(45)]
    public byte InjuryProneness { get; set; }

    [DataPosition(52)]
    public byte NaturalFitness { get; set; }

    [DataPosition(66)]
    public byte Versatility { get; set; }

    #region Incorrect values

    [DataPosition(30)]
    public byte Anticipation { get; set; }

    [DataPosition(67)]
    public byte Creativity { get; set; }

    [DataPosition(35)]
    public byte Crossing { get; set; }

    [DataPosition(36)]
    public byte Decisions { get; set; }

    [DataPosition(38)]
    public byte Dribbling { get; set; }

    [DataPosition(39)]
    public byte Finishing { get; set; }

    [DataPosition(42)]
    public byte Handling { get; set; }

    [DataPosition(43)]
    public byte Heading { get; set; }

    [DataPosition(49)]
    public byte LongShots { get; set; }

    [DataPosition(50)]
    public byte Marking { get; set; }

    [DataPosition(51)]
    public byte OffTheBall { get; set; }

    [DataPosition(53)]
    public byte OneOnOnes { get; set; }

    [DataPosition(55)]
    public byte Passing { get; set; }

    [DataPosition(57)]
    public byte Positioning { get; set; }

    [DataPosition(58)]
    public byte Reflexes { get; set; }

    [DataPosition(62)]
    public byte Tackling { get; set; }

    [DataPosition(56)]
    public byte Penalties { get; set; }

    [DataPosition(65)]
    public byte ThrowIns { get; set; }

    #endregion Incorrect values

    internal void PopulateStaffPropertiers(Staff staff)
    {
        foreach (var property in OverridableProperties)
        {
            property.SetValue(this, property.GetValue(staff));
        }
    }
}
