using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DbFileHandler
{
    internal static DbFileData GetDbFileData(string datFileTemplatePath)
    {
        var dbFileData = new DbFileData
        {
            FirstNames = GetStringData(datFileTemplatePath, "first_names", 60),
            ClubCompetitions = GetData<ClubCompetition>(datFileTemplatePath, "club_comp", 107),
            Clubs = GetData<Club>(datFileTemplatePath, "club", 581),
            CommonNames = GetStringData(datFileTemplatePath, "common_names", 60),
            Confederations = GetData<Confederation>(datFileTemplatePath, "continent", 198),
            LastNames = GetStringData(datFileTemplatePath, "second_names", 60),
            Nations = GetData<Nation>(datFileTemplatePath, "nation", 290),
        };

        var stringData = StringHandler.ExtractFileData(datFileTemplatePath, "staff", 157, out var staffEndPosition, stopAtIdBreak: true);

        var staffList = new Dictionary<int, Staff>(stringData.Count);
        foreach (var singleString in stringData)
        {
            var s = new Staff();
            s.SetDataPositionableProperties(singleString);
            if (s.DbStaffPlayerId < 0)
            {

            }
            else if (staffList.ContainsKey(s.DbStaffPlayerId))
            {

            }
            else
            {
                staffList.Add(s.DbStaffPlayerId, s);
            }
        }

        // coach data (we don't care about)
        _ = StringHandler.ExtractFileData(datFileTemplatePath, "staff", 68, out var coachEndPosition, startAt: staffEndPosition, stopAtIdBreak: true);
        
        stringData = StringHandler.ExtractFileData(datFileTemplatePath, "staff", 70, out _, startAt: coachEndPosition + staffEndPosition);
        var player = new List<Player>(stringData.Count);
        foreach (var singleString in stringData)
        {
            var p = new Player();
            p.SetDataPositionableProperties(singleString);
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
                player.Add(p);
            }
        }

        dbFileData.Players = player;

        return dbFileData;
    }

    private static Dictionary<int, string> GetStringData(string datFileTemplatePath, string dataName, int splitPosition)
    {
        var stringData = StringHandler.ExtractFileData(datFileTemplatePath, dataName, splitPosition, out _);

        var dataList = new Dictionary<int, string>(stringData.Count);
        for (var i = 0; i < stringData.Count; i++)
        {
            dataList.Add(StringHandler.IntGet(stringData[i], 51), StringHandler.StringGet(stringData[i], 0, 50));
        }

        return dataList;
    }

    private static Dictionary<int, T> GetData<T>(string datFileTemplatePath, string dataName, int splitPosition)
        where T : BaseData, new()
    {
        var stringData = StringHandler.ExtractFileData(datFileTemplatePath, dataName, splitPosition, out _);

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
