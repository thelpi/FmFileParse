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

        var firstNames = GetStringData("first_names", 60);
        var lastNames = GetStringData("second_names", 60);
        var commonNames = GetStringData("common_names", 60);

        return new DbFileData
        {
            ClubCompetitions = GetData<ClubCompetition>("club_comp", 107),
            Clubs = GetData<Club>("club", 581).ManageDuplicateClubs(),
            Confederations = GetData<Confederation>("continent", 198),
            Nations = GetData<Nation>("nation", 290),
            Players = GetPlayersList(staffList, staffEndPosition + coachEndPosition, firstNames, lastNames, commonNames)
        };
    }

    private static List<Player> GetPlayersList(Dictionary<int, Staff> staffList, int startPosition,
        Dictionary<int, string> firstNames,
        Dictionary<int, string> lastNames,
        Dictionary<int, string> commonNames)
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
                p.FirstName = firstNames[p.FirstNameId].Sanitize();
                p.LastName = lastNames[p.LastNameId].Sanitize();
                p.CommonName = commonNames[p.CommonNameId].Sanitize();
                players.Add(p);
            }
        }

        var duplicatePlayersGroups = players
            .GroupBy(x => (x.CommonName, x.FirstName, x.LastName, x.ClubId, x.ActualYearOfBirth, x.DateOfBirth.Month, x.DateOfBirth.Day))
            .Where(x => x.Count() > 1)
            .ToList();

        foreach (var duplicateGroup in duplicatePlayersGroups)
        {
            var keptPlayer = duplicateGroup.GetMostRelevantPlayer();
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
