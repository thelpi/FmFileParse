using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    public static Dictionary<int, Staff> GetDataFileStaffDictionary(SaveGameFile savegame, out List<Staff> duplicateStaff)
        => GetDataFileDictionary(savegame, DataFileType.Staff, Staff.Convert, x => x.StaffPlayerId, out duplicateStaff);

    public static Dictionary<int, Contract> GetDataFileContractDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.Contracts, Contract.Convert, x => x.PlayerId, out _);

    public static Dictionary<int, Club> GetDataFileClubDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.Clubs, Club.Convert, out _);

    public static Dictionary<int, ClubComp> GetDataFileClubCompetitionDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.ClubComps, ClubComp.Convert, out _);

    public static Dictionary<int, Country> GetDataFileNationDictionary(SaveGameFile savegame)
        => GetDataFileDictionary(savegame, DataFileType.Nations, Country.Convert, out _);

    public static List<byte[]> GetDataFileBytes(SaveGameFile savegame, DataFileType fileType, int sizeOfData)
    {
        var dataFile = savegame.DataBlockNameList.First(x => x.FileFacts.Type == fileType);
        return ByteHandler.GetAllDataFromFile(dataFile, savegame.FileName, sizeOfData);
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
