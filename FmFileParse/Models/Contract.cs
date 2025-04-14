using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Contract
{
    [DataPosition(0)]
    public int PlayerId { get; set; }

    [DataPosition(45)]
    public DateTime? ContractEndDate { get; set; }

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
    public TransferStatus? TransferStatus { get; set; }

    [DataPosition(79)]
    public SquadStatus? SquadStatus { get; set; }

    [DataPosition(74)]
    public int FutureClubId { get; set; }
}
