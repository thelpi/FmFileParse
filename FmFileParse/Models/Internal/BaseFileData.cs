namespace FmFileParse.Models.Internal;

public class BaseFileData
{
    public Dictionary<int, Nation> Nations { get; set; } = [];

    public Dictionary<int, Club> Clubs { get; set; } = [];

    public Dictionary<int, ClubCompetition> ClubCompetitions { get; set; } = [];

    public Dictionary<int, Confederation> Confederations { get; set; } = [];

    public List<Player> Players { get; set; } = [];
}
