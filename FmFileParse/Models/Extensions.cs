namespace FmFileParse.Models;

public static class Extensions
{
    public static Player? GetMostRelevantPlayer(this IEnumerable<Player> players)
        => players.OrderByDescending(x => true).GetMostRelevantPlayer();

    public static Player? GetMostRelevantPlayer(this IOrderedEnumerable<Player> players)
    {
        return players
            .ThenByDescending(x => x.WorldReputation)
            .ThenByDescending(x => x.CurrentReputation)
            .ThenByDescending(x => x.HomeReputation)
            .ThenByDescending(x => x.CurrentAbility)
            .ThenByDescending(x => x.PotentialAbility == -1 ? 120 : (x.PotentialAbility == -2 ? 160 : x.PotentialAbility))
            .FirstOrDefault();
    }
}
