using FmFileParse.Models;

namespace FmFileParse.SaveImport;

public class SaveGameData
{
    public Dictionary<int, string> FirstNames { get; set; }

    public Dictionary<int, string> Surnames { get; set; }

    public Dictionary<int, string> CommonNames { get; set; }

    public Dictionary<int, Country> Nations { get; set; }

    public Dictionary<int, Club> Clubs { get; set; }

    public Dictionary<int, ClubComp> ClubComps { get; set; }

    public List<Player> Players { get; set; }

    public DateTime GameDate { get; set; }

    public decimal ValueMultiplier { get { return 2.5m; } }
}
