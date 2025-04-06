using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DbFileHandler
{
    internal static DbFileData GetDbFileData()
    {
        var staffList = GetStaffList(out var staffEndPosition);

        // coach data (we don't care about)
        _ = StringHandler.ExtractFileData("staff", 68, out var coachEndPosition, startAt: staffEndPosition, stopAtIdBreak: true);

        return new DbFileData
        {
            FirstNames = GetStringData("first_names", 60),
            ClubCompetitions = GetData<ClubCompetition>("club_comp", 107),
            Clubs = GetData<Club>("club", 581).ManageDuplicateClubs(),
            CommonNames = GetStringData("common_names", 60),
            Confederations = GetData<Confederation>("continent", 198),
            LastNames = GetStringData("second_names", 60),
            Nations = GetData<Nation>("nation", 290),
            Players = GetPlayersList(staffList, staffEndPosition + coachEndPosition)
        };
    }

    private static List<Player> GetPlayersList(Dictionary<int, Staff> staffList, int startPosition)
    {
        var stringData = StringHandler.ExtractFileData("staff", 70, out _, startAt: startPosition);

        var players = new List<Player>(stringData.Count);
        foreach (var singleString in stringData)
        {
            var p = new Player();
            p.SetDataPositionableProperties(singleString);
            p.ApplyDbFileCoefficients();
            if (staffList.TryGetValue(p.PlayerId, out var staff))
            {
                p.PopulateStaffPropertiers(staff);
                if (staff.ClubId >= 0)
                {
                    p.Contract = new Contract
                    {
                        ContractEndDate = staff.DateContractEnd
                    };
                }
                players.Add(p);
            }
        }

        var duplicatePlayersGroups = players
            .GroupBy(x => (x.CommonNameId, x.FirstNameId, x.LastNameId, x.ClubId, x.ComputedDateOfBirth))
            .Where(x => x.Count() > 1)
            .ToList();

        foreach (var duplicateGroup in duplicatePlayersGroups)
        {
            var keptPlayer = duplicateGroup
                .OrderByDescending(x => x.WorldReputation)
                .ThenByDescending(x => x.CurrentReputation)
                .ThenByDescending(x => x.HomeReputation)
                .ThenByDescending(x => x.CurrentAbility)
                .ThenByDescending(x => x.PotentialAbility == -1 ? 120 : (x.PotentialAbility == -2 ? 160 : x.PotentialAbility))
                .First();
            players.RemoveAll(p => duplicateGroup.Contains(p) && p != keptPlayer);
        }

        return players;
    }

    private static Dictionary<int, Staff> GetStaffList(out int staffEndPosition)
    {
        var stringData = StringHandler.ExtractFileData("staff", 157, out staffEndPosition, stopAtIdBreak: true);

        var staffList = new Dictionary<int, Staff>(stringData.Count);
        foreach (var singleString in stringData)
        {
            var s = new Staff();
            s.SetDataPositionableProperties(singleString);
            if (s.DbStaffPlayerId >= 0)
            {
                staffList.Add(s.DbStaffPlayerId, s);
            }
        }

        return staffList;
    }

    private static Dictionary<int, string> GetStringData(string dataName, int splitPosition)
    {
        var stringData = StringHandler.ExtractFileData(dataName, splitPosition, out _);

        var dataList = new Dictionary<int, string>(stringData.Count);
        for (var i = 0; i < stringData.Count; i++)
        {
            dataList.Add(StringHandler.IntGet(stringData[i], 51), StringHandler.StringGet(stringData[i], 0, 50));
        }

        return dataList;
    }

    private static Dictionary<int, T> GetData<T>(string dataName, int splitPosition)
        where T : BaseData, new()
    {
        var stringData = StringHandler.ExtractFileData(dataName, splitPosition, out _);

        var dataList = new Dictionary<int, T>(stringData.Count);
        foreach (var singleString in stringData)
        {
            var d = new T();
            d.SetDataPositionableProperties(singleString);
            dataList.Add(d.Id, d);
        }

        return dataList;
    }
}
