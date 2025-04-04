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

        // players

        return dbFileData;
    }

    private static Dictionary<int, string> GetStringData(string datFileTemplatePath, string dataName, int splitPosition)
    {
        var stringData = StringHandler.ExtractFileData(datFileTemplatePath, dataName, splitPosition);

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
        var stringData = StringHandler.ExtractFileData(datFileTemplatePath, dataName, splitPosition);

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
