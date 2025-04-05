using FmFileParse.Models.Attributes;
using FmFileParse.Models.Internal;

namespace FmFileParse.Models;

public class Nation : BaseData
{
    private const int EuId = 2;

    [DataPosition(113)]
    public short ConfederationId { get; set; }

    [DataPosition(4, Length = 50)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(127)]
    public byte IsEuId { get; set; }

    [DataPosition(83, Length = 3)]
    public string Acronym { get; set; } = string.Empty;

    [DataPosition(133)]
    public byte LeagueStandard { get; set; }

    [DataPosition(142)]
    public short Reputation { get; set; }

    public bool IsEu => IsEuId == EuId;

    public override IEnumerable<string> Describe(BaseFileData data)
    {
        yield return $"Name: {Name} - Acronym: {Acronym}";
        yield return $"Reputation: {Reputation} - LeagueStandard: {LeagueStandard}";
        yield return $"IsEu: {IsEu}";

        yield return string.Empty;
        yield return "---- Confederation (from country) details ----";
        data.Confederations.TryGetValue(ConfederationId, out var confederation);
        if (confederation is not null)
        {
            foreach (var row in confederation.Describe(data))
            {
                yield return row;
            }
        }
        else
        {
            yield return ConfederationId >= 0
                ? $"No confederation with id {ConfederationId} found!"
                : "Confederation is not set on the country.";
        }
    }
}
