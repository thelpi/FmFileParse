namespace FmFileParse.Models.Internal;

public class BaseFileData
{
    public Dictionary<int, string> FirstNames { get; set; } = [];

    public Dictionary<int, string> LastNames { get; set; } = [];

    public Dictionary<int, string> CommonNames { get; set; } = [];

    public Dictionary<int, Nation> Nations { get; set; } = [];

    public Dictionary<int, Club> Clubs { get; set; } = [];

    public Dictionary<int, ClubCompetition> ClubCompetitions { get; set; } = [];

    public Dictionary<int, Confederation> Confederations { get; set; } = [];

    public List<Player> Players { get; set; } = [];
}
