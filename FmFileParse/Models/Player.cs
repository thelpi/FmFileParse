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
    public short CurrentReputation { get; set; }

    [DataPosition(11)]
    public short HomeReputation { get; set; }

    [DataPosition(13)]
    public short WorldReputation { get; set; }

    [DataPosition(15)]
    public byte GoalKeeperPos { get; set; }

    [DataPosition(16)]
    public byte SweeperPos { get; set; }

    [DataPosition(17)]
    public byte DefenderPos { get; set; }

    [DataPosition(18)]
    public byte DefensiveMidfielderPos { get; set; }

    [DataPosition(19)]
    public byte MidfielderPos { get; set; }

    [DataPosition(20)]
    public byte AttackingMidfielderPos { get; set; }

    [DataPosition(21)]
    public byte StrikerPos { get; set; }

    [DataPosition(22)]
    public byte WingBackPos { get; set; }

    [DataPosition(26)]
    public byte FreeRolePos { get; set; }

    [DataPosition(24)]
    public byte LeftSide { get; set; }

    [DataPosition(23)]
    public byte RightSide { get; set; }

    [DataPosition(25)]
    public byte CentreSide { get; set; }

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
    [ReversedAttribute]
    public byte Dirtiness { get; set; }

    [DataPosition(41)]
    public byte FreeKicks { get; set; }

    [DataPosition(45)]
    [ReversedAttribute]
    public byte InjuryProneness { get; set; }

    [DataPosition(52)]
    public byte NaturalFitness { get; set; }

    [DataPosition(66)]
    public byte Versatility { get; set; }

    [DataPosition(30)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Anticipation { get; set; }

    [DataPosition(67)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) Creativity { get; set; }

    [DataPosition(35)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) Crossing { get; set; }

    [DataPosition(36)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Decisions { get; set; }

    [DataPosition(38)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) Dribbling { get; set; }

    [DataPosition(39)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) Finishing { get; set; }

    [DataPosition(42)]
    [IntrinsicAttribute(IntrinsicType.GoalkeeperAttribute)]
    public (byte current, byte potential) Handling { get; set; }

    [DataPosition(43)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Heading { get; set; }

    [DataPosition(49)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) LongShots { get; set; }

    [DataPosition(50)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) Marking { get; set; }

    [DataPosition(51)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) OffTheBall { get; set; }

    [DataPosition(53)]
    [IntrinsicAttribute(IntrinsicType.GoalkeeperAttribute)]
    public (byte current, byte potential) OneOnOnes { get; set; }

    [DataPosition(55)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Passing { get; set; }

    [DataPosition(57)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Positioning { get; set; }

    [DataPosition(58)]
    [IntrinsicAttribute(IntrinsicType.GoalkeeperAttribute)]
    public (byte current, byte potential) Reflexes { get; set; }

    [DataPosition(62)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Tackling { get; set; }

    [DataPosition(56)]
    [IntrinsicAttribute(IntrinsicType.General)]
    public (byte current, byte potential) Penalties { get; set; }

    [DataPosition(65)]
    [IntrinsicAttribute(IntrinsicType.FieldPlayerAttribute)]
    public (byte current, byte potential) ThrowIns { get; set; }

    internal void PopulateStaffPropertiers(Staff staff)
    {
        foreach (var property in OverridableProperties)
        {
            property.SetValue(this, property.GetValue(staff));
        }
    }
}
