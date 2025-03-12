using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Contract
{
    [DataPosition(0)]
    public int PlayerId { get; set; }

    [DataPosition(16)]
    public DateTime? DateJoined { get; set; }

    [DataPosition(37)]
    public DateTime? ContractStartDate { get; set; }

    [DataPosition(45)]
    public DateTime? ContractEndDate { get; set; }

    [DataPosition(12)]
    public int WagePerWeek { get; set; }

    [DataPosition(16)]
    public int GoalBonus { get; set; }

    [DataPosition(20)]
    public int AssistBonus { get; set; }

    [DataPosition(28)]
    public bool NonPromotionReleaseClause { get; set; }

    [DataPosition(29)]
    public bool MinimumFeeReleaseClause { get; set; }

    [DataPosition(30)]
    public bool NonPlayingReleaseClause { get; set; }

    [DataPosition(31)]
    public bool RelegationReleaseClause { get; set; }

    [DataPosition(32)]
    public bool ManagerReleaseClause { get; set; }

    [DataPosition(33)]
    public int ReleaseClauseValue { get; set; }

    [DataPosition(78)]
    public byte TransferStatus { get; set; }

    [DataPosition(79)]
    public byte SquadStatus { get; set; }
}
