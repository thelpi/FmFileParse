using FmFileParse.Models.Attributes;
using FmFileParse.Models.Internal;

namespace FmFileParse.Models;

public class ClubCompetition : BaseData
{
    [DataPosition(56, Length = 25)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(4, Length = 50)]
    public string LongName { get; set; } = string.Empty;

    [DataPosition(83, Length = 3)]
    public string Acronym { get; set; } = string.Empty;

    [DataPosition(93)]
    public int NationId { get; set; }

    [DataPosition(105)]
    public short Reputation { get; set; }

    public override IEnumerable<string> Describe(BaseFileData data)
    {
        yield return $"Name: {Name} - Acronym: {Acronym}";
        yield return $"LongName: {LongName} - Reputation: {Reputation}";

        yield return string.Empty;
        yield return "---- Nation (from club competition) details ----";
        data.Nations.TryGetValue(NationId, out var nation);
        if (nation is not null)
        {
            foreach (var row in nation.Describe(data))
            {
                yield return row;
            }
        }
        else
        {
            yield return NationId >= 0
                ? $"No nation with id {NationId} found!"
                : "Nation is not set on the club competition.";
        }
    }
}
