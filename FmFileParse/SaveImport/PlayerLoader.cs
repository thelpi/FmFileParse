using FmFileParse.Models;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class PlayerLoader
{
    public static SaveGameData LoadPlayers(SaveGameFile savegame)
    {
        return new SaveGameData
        {
            GameDate = savegame.GameDate,
            FirstNames = GetDataFileStringsDictionary(savegame, DataFileType.FirstNames),
            Surnames = GetDataFileStringsDictionary(savegame, DataFileType.SecondNames),
            CommonNames = GetDataFileStringsDictionary(savegame, DataFileType.CommonNames),
            Nations = DataFileLoaders.GetDataFileNationDictionary(savegame),
            Clubs = DataFileLoaders.GetDataFileClubDictionary(savegame),
            Players = ConstructSearchablePlayers(
                DataFileLoaders.GetDataFileStaffDictionary(savegame, out _),
                GetDataFilePlayerData(savegame),
                DataFileLoaders.GetDataFileContractDictionary(savegame)).ToList(),
            ClubComps = DataFileLoaders.GetDataFileClubCompetitionDictionary(savegame)
        };
    }

    private static IEnumerable<Player> ConstructSearchablePlayers(Dictionary<int, Staff> staffDic, List<Player> players, Dictionary<int, Contract> contracts)
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

    private static Dictionary<int, string> GetDataFileStringsDictionary(SaveGameFile savegame, DataFileType type)
    {
        var fileFacts = SaveGameHandler.GetDataFileFact(type);
        var fileData = DataFileLoaders.GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var fileContents = new Dictionary<int, string>(fileData.Count);
        for (var i = 0; i < fileData.Count; i++)
        {
            fileContents.Add(i, ByteHandler.GetStringFromBytes(fileData[i], 0, fileFacts.StringLength));
        }

        return fileContents;
    }

    private static List<Player> GetDataFilePlayerData(SaveGameFile savegame)
    {
        var fileFacts = SaveGameHandler.GetDataFileFact(DataFileType.Players);
        return DataFileLoaders.GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize)
            .Select(Player.Convert)
            .ToList();
    }
}
