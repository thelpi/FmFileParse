using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    private static Dictionary<int, Staff> GetDataFileStaffDictionary(SaveGameFile savegame, out List<Staff> duplicateStaff)
        => GetDataFileDictionary(savegame, DataFileType.Staff, x => x.StaffPlayerId, out duplicateStaff);

    private static Dictionary<int, Contract> GetDataFileContractsDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Contract>(savegame, DataFileType.Contracts, x => x.PlayerId, out _);

    public static Dictionary<int, Club> GetDataFileClubsDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Club>(savegame, DataFileType.Clubs, out _);

    public static Dictionary<int, ClubCompetition> GetDataFileClubCompetitionsDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<ClubCompetition>(savegame, DataFileType.ClubCompetitions, out _);

    public static Dictionary<int, Country> GetDataFileCountriesDictionary(SaveGameFile savegame)
        => GetDataFileDictionary<Country>(savegame, DataFileType.Countries, out _);

    public static Dictionary<int, Confederation> GetDataFileConfederationsDictionary(SaveGameFile savegame)
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

    public static List<Player> GetDataFilePlayersList(SaveGameFile savegame)
    {
        return ConstructSearchablePlayers(
            GetDataFileStaffDictionary(savegame, out _),
            GetDataFilePlayersData(savegame),
            GetDataFileContractsDictionary(savegame)).ToList();
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

    public static List<Player> GetDataFilePlayersData(SaveGameFile savegame)
    {
        var fileFacts = SaveGameHandler.GetDataFileFact(DataFileType.Players);
        return GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize)
            .Select(x =>
            {
                var player = new Player();
                DataPositionAttributeParser.SetDataPositionableProperties(player, x);
                // TODO: do better
                var i = 0;
                foreach (var property in Player.IntrinsicAttributeProperties)
                {
                    var intrinsicValue = (byte)property.GetValue(player)!;
                    var inGameValue = IntrisincToInGameAttributeValue((sbyte)intrinsicValue, i, player.CurrentAbility, player.GoalKeeperPos);
                    property.SetValue(player, inGameValue);
                    i++;
                }
                // TODO: make more dynamic
                player.InjuryProneness = (byte)(21 - player.InjuryProneness);
                player.Dirtiness = (byte)(21 - player.Dirtiness);
                return player;
            })
            .ToList();
    }

    private static byte IntrisincToInGameAttributeValue(sbyte intrinsicValue, int i, short currentAbility, byte goalKeeperRate)
    {
        if (i == 0 || i == 3 || i == 6 || i == 7 || i == 10 || i == 11 || i == 12 || i == 13)
        {
            return HighConvert(currentAbility, intrinsicValue);
        }
        else
        {
            if (i == 15 || i == 16 || i == 17)
            {
                return goalKeeperRate > 14
                    ? HighConvert(currentAbility, intrinsicValue)
                    : LowConvert(currentAbility, intrinsicValue);
            }
            else if (i == 1 || i == 2 || i == 4 || i == 5 || i == 8 || i == 9 || i == 14)
            {
                return goalKeeperRate > 14
                    ? LowConvert(currentAbility, intrinsicValue)
                    : HighConvert(currentAbility, intrinsicValue);
            }
        }

        return 0; // whatever
    }

    private static byte LowConvert(short currentAbility, sbyte intrinsicValue)
    {
        var d = (intrinsicValue / 10.0) + (currentAbility / 200.0) + 10;

        var r = (d * d / 30.0) + (d / 3.0) + 0.5;

        if (r < 1)
        {
            r = 1;
        }
        else if (r > 20)
        {
            r = 20;
        }

        return (byte)Math.Truncate(r);
    }

    private static byte HighConvert(short currentAbility, sbyte intrinsicValue)
    {
        var d = (intrinsicValue / 10.0) + (currentAbility / 20.0) + 10;

        var r = (d * d / 30.0) + (d / 3.0) + 0.5;

        if (r < 1)
        {
            r = 1;
        }
        else if (r > 20)
        {
            r = 20;
        }

        return (byte)Math.Truncate(r);
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
