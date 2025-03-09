using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    private static Dictionary<int, Staff> GetDataFileStaffDictionary(SaveGameFile savegame, out List<Staff> duplicateStaff)
        => GetDataFileDictionary(savegame, DataFileType.Staff, Staff.Convert, x => x.StaffPlayerId, out duplicateStaff);

    private static Dictionary<int, Contract> GetDataFileContractDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.Contracts, Contract.Convert, x => x.PlayerId, out _);

    public static Dictionary<int, Club> GetDataFileClubDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.Clubs, Club.Convert, out _);

    public static Dictionary<int, ClubComp> GetDataFileClubCompetitionDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.ClubComps, ClubComp.Convert, out _);

    public static Dictionary<int, Country> GetDataFileNationDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.Nations, Country.Convert, out _);

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
                player.PopulateStaffPropertiers(staff, contracts.TryGetValue(staff.Id, out var contract) ? contract : null);
                yield return player;
            }
        }
    }

    public static List<Player> GetDataFilePlayerData(SaveGameFile savegame)
    {
        var fileFacts = SaveGameHandler.GetDataFileFact(DataFileType.Players);
        return GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize)
            .Select(Player.Convert)
            .ToList();
    }

    private static Dictionary<int, T> GetDataFileDictionary<T>(
        SaveGameFile savegame,
        DataFileType type,
        Func<byte[], T> converter,
        out List<T> duplicates)
        where T : BaseData
        => GetDataFileDictionary(savegame, type, converter, x => x.Id, out duplicates);

    private static Dictionary<int, T> GetDataFileDictionary<T>(
        SaveGameFile savegame,
        DataFileType type,
        Func<byte[], T> converter,
        Func<T, int> getId,
        out List<T> duplicates)
    {
        duplicates = [];

        var fileFacts = SaveGameHandler.GetDataFileFact(type);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, T>(bytes.Count);
        foreach (var item in bytes)
        {
            var data = converter(item);
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
