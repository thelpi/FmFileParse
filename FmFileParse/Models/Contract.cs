using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;

namespace FmFileParse.Models;

public class Contract
{
    public int PlayerId { get; set; }

    public DateTime? DateJoined { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    public int WagePerWeek { get; set; }

    public int GoalBonus { get; set; }

    public int AssistBonus { get; set; }

    public bool NonPromotionReleaseClause { get; set; }

    public bool MinimumFeeReleaseClause { get; set; }

    public bool NonPlayingReleaseClause { get; set; }

    public bool RelegationReleaseClause { get; set; }

    public bool ManagerReleaseClause { get; set; }

    public int ReleaseClauseValue { get; set; }

    public byte TransferStatus { get; set; }

    public byte SquadStatus { get; set; }

    internal static Contract Convert(byte[] source)
    {
        return new Contract
        {
            PlayerId = ByteHandler.GetIntFromBytes(source, 0),
            WagePerWeek = (int)(ByteHandler.GetIntFromBytes(source, 12) * SaveGameData.ValueMultiplier),
            GoalBonus = (int)(ByteHandler.GetIntFromBytes(source, 16) * SaveGameData.ValueMultiplier),
            AssistBonus = (int)(ByteHandler.GetIntFromBytes(source, 20) * SaveGameData.ValueMultiplier),
            NonPromotionReleaseClause = ByteHandler.GetByteFromBytes(source, 28) == 1,
            MinimumFeeReleaseClause = ByteHandler.GetByteFromBytes(source, 29) == 1,
            NonPlayingReleaseClause = ByteHandler.GetByteFromBytes(source, 30) == 1,
            RelegationReleaseClause = ByteHandler.GetByteFromBytes(source, 31) == 1,
            ManagerReleaseClause = ByteHandler.GetByteFromBytes(source, 32) == 1,
            ReleaseClauseValue = (int)(ByteHandler.GetIntFromBytes(source, 33) * SaveGameData.ValueMultiplier),
            ContractStartDate = ByteHandler.GetDateFromBytes(source, 37),
            DateJoined = ByteHandler.GetDateFromBytes(source, 16),
            ContractEndDate = ByteHandler.GetDateFromBytes(source, 45),
            TransferStatus = ByteHandler.GetByteFromBytes(source, 78),
            SquadStatus = ByteHandler.GetByteFromBytes(source, 79)
        };
    }
}
