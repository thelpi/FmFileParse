using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    private static Dictionary<int, Staff> GetDataFileStaffDictionary(SaveGameFile savegame, out List<Staff> duplicateStaff)
        => GetDataFileDictionary(savegame, DataFileType.Staff, x => x.StaffPlayerId, out duplicateStaff);

    private static Dictionary<int, Contract> GetDataFileContractDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Contract>(savegame, DataFileType.Contracts, x => x.PlayerId, out _);

    public static Dictionary<int, Club> GetDataFileClubDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Club>(savegame, DataFileType.Clubs, out _);

    public static Dictionary<int, ClubComp> GetDataFileClubCompetitionDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<ClubComp>(savegame, DataFileType.ClubComps, out _);

    public static Dictionary<int, Country> GetDataFileNationDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Country>(savegame, DataFileType.Nations, out _);

    public static Dictionary<int, Confederation> GetDataFileConfederationDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Confederation>(savegame, DataFileType.Confederations, out _);

    private static List<byte[]> GetDataFileBytes(SaveGameFile savegame, DataFileType fileType, int sizeOfData)
    {
        var dataFile = savegame.DataBlockNameList.First(x => x.FileFacts.Type == fileType);
        return ByteHandler.GetAllDataFromFile(dataFile, savegame.FileName, sizeOfData);
    }

    public static Dictionary<int, string> GetDataFileStringsDictionary(SaveGameFile savegame, DataFileType type)
    {
        var fileFacts = SaveGameHandler.GetDataFileFact(type);
        var fileData = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var fileContents = new Dictionary<int, string>(fileData.Count);
        for (var i = 0; i < fileData.Count; i++)
        {
            fileContents.Add(i, ByteHandler.GetStringFromBytes(fileData[i], 0, fileFacts.StringLength));
        }

        return fileContents;
    }

    public static List<Player> GetDataFilePlayerList(SaveGameFile savegame)
    {
        return ConstructSearchablePlayers(
            GetDataFileStaffDictionary(savegame, out _),
            GetDataFilePlayerData(savegame),
            GetDataFileContractDictionary(savegame)).ToList();
    }

    public static IEnumerable<Player> ConstructSearchablePlayers(Dictionary<int, Staff> staffDic, List<Player> players, Dictionary<int, Contract> contracts)
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

    public static List<Player> GetDataFilePlayerData(SaveGameFile savegame)
    {
        var fileFacts = SaveGameHandler.GetDataFileFact(DataFileType.Players);
        return GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize)
            .Select(x =>
            {
                var player = new Player();
                DataPositionAttributeParser.SetDataPositionableProperties(player, x);
                return player;
            })
            .ToList();
    }

    private static Dictionary<int, T> GetDataFileDictionary<T>(
        SaveGameFile savegame,
        DataFileType type,
        out List<T> duplicates)
        where T : BaseData, new()
        => GetDataFileDictionary(savegame, type, x => x.Id, out duplicates);

    private static Dictionary<int, T> GetDataFileDictionary<T>(
        SaveGameFile savegame,
        DataFileType type,
        Func<T, int> getId,
        out List<T> duplicates)
        where T : new()
    {
        duplicates = [];

        var fileFacts = SaveGameHandler.GetDataFileFact(type);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, T>(bytes.Count);
        foreach (var item in bytes)
        {
            var data = new T();
            DataPositionAttributeParser.SetDataPositionableProperties(data, item);
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
}
