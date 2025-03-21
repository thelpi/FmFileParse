using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    public static Dictionary<int, Club> GetDataFileClubsDictionary(this SaveGameFile savegame)
        => savegame.GetDataFileDictionary<Club>(DataFileType.Clubs, out _).ManageDuplicateClubs();

    public static Dictionary<int, ClubCompetition> GetDataFileClubCompetitionsDictionary(this SaveGameFile savegame)
        => savegame.GetDataFileDictionary<ClubCompetition>(DataFileType.ClubCompetitions, out _);

    public static Dictionary<int, Nation> GetDataFileNationsDictionary(this SaveGameFile savegame)
        => savegame.GetDataFileDictionary<Nation>(DataFileType.Nations, out _);

    public static Dictionary<int, Confederation> GetDataFileConfederationsDictionary(this SaveGameFile savegame)
        => savegame.GetDataFileDictionary<Confederation>(DataFileType.Confederations, out _);

    public static Dictionary<int, string> GetDataFileStringsDictionary(
        this SaveGameFile savegame,
        DataFileType type)
    {
        var fileFacts = type.GetDataFileFact();
        var fileData = savegame.GetDataFileBytes(fileFacts.Type, fileFacts.DataSize);

        var fileContents = new Dictionary<int, string>(fileData.Count);
        for (var i = 0; i < fileData.Count; i++)
        {
            fileContents.Add(i, fileData[i].GetStringFromBytes(0, fileFacts.StringLength));
        }

        return fileContents;
    }

    public static List<Player> GetDataFilePlayersList(this SaveGameFile savegame)
    {
        return ConstructSearchablePlayers(
            savegame.GetDataFileStaffDictionary(out _),
            savegame.GetDataFilePlayersData(),
            savegame.GetDataFileContractsDictionary()).ToList();
    }

    private static Dictionary<int, Staff> GetDataFileStaffDictionary(this SaveGameFile savegame, out List<Staff> duplicateStaff)
        => savegame.GetDataFileDictionary(DataFileType.Staff, x => x.StaffPlayerId, out duplicateStaff);

    private static Dictionary<int, Contract> GetDataFileContractsDictionary(this SaveGameFile savegame)
        => savegame.GetDataFileDictionary<Contract>(DataFileType.Contracts, x => x.PlayerId, out _);

    private static List<byte[]> GetDataFileBytes(
        this SaveGameFile savegame,
        DataFileType fileType,
        int sizeOfData)
    {
        var dataFile = savegame.DataBlockNameList.First(x => x.FileFacts.Type == fileType);
        return dataFile.GetAllDataFromFile(savegame.FileName, sizeOfData);
    }

    private static List<Player> GetDataFilePlayersData(this SaveGameFile savegame)
    {
        var fileFacts = DataFileType.Players.GetDataFileFact();
        return savegame.GetDataFileBytes(fileFacts.Type, fileFacts.DataSize)
            .Select(x => new Player().SetDataPositionableProperties(x).ComputeAndSetIntrinsicAttributes())
            .ToList();
    }

    private static IEnumerable<Player> ConstructSearchablePlayers(
        Dictionary<int, Staff> staffDic,
        List<Player> players,
        Dictionary<int, Contract> contracts)
    {
        foreach (var player in players)
        {
            if (staffDic.TryGetValue(player.PlayerId, out var staff))
            {
                player.Contract = contracts.TryGetValue(staff.Id, out var contract) ? contract : null;
                player.PopulateStaffPropertiers(staff);
                yield return player;
            }
        }
    }

    private static Dictionary<int, T> GetDataFileDictionary<T>(
        this SaveGameFile savegame,
        DataFileType type,
        out List<T> duplicates)
        where T : BaseData, new()
        => GetDataFileDictionary(savegame, type, x => x.Id, out duplicates);

    private static Dictionary<int, T> GetDataFileDictionary<T>(
        this SaveGameFile savegame,
        DataFileType type,
        Func<T, int> getId,
        out List<T> duplicates)
        where T : new()
    {
        duplicates = [];

        var fileFacts = type.GetDataFileFact();
        var bytes = savegame.GetDataFileBytes(fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, T>(bytes.Count);
        foreach (var item in bytes)
        {
            var data = new T().SetDataPositionableProperties(item);
            if (getId(data) >= 0)
            {
                if (!dic.TryAdd(getId(data), data))
                {
                    duplicates.Add(data);
                }
            }
        }

        return dic;
    }

    private static Dictionary<int, Club> ManageDuplicateClubs(this Dictionary<int, Club> clubs)
    {
        var clubsGroups = clubs.Values.GetMaxOccurence(c => $"{c.LongName};{c.NationId};{c.DivisionId};");

        if (clubsGroups.Count() > 1)
        {
            var i = 1;
            foreach (var club in clubsGroups.OrderByDescending(c => c.Reputation))
            {
                if (i > 1)
                {
                    club.LongName = $"{club.LongName}-{i}";
                }
                i++;
            }
        }

        return clubs;
    }
}
