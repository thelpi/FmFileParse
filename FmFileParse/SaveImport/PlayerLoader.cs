using FmFileParse.Models;

namespace FmFileParse.SaveImport;

internal static class PlayerLoader
{
    public static SaveGameData LoadPlayers(SaveGameFile savegame)
    {
        var clubcomps = DataFileLoaders.GetDataFileClubCompetitionDictionary(savegame);

        var firstnames = GetDataFileStringsDictionary(savegame, DataFileType.First_Names);

        var secondNames = GetDataFileStringsDictionary(savegame, DataFileType.Second_Names);

        var commonNames = GetDataFileStringsDictionary(savegame, DataFileType.Common_Names);

        var nations = DataFileLoaders.GetDataFileNationDictionary(savegame);

        var clubs = DataFileLoaders.GetDataFileClubDictionary(savegame);

        var staffDic = DataFileLoaders.GetDataFileStaffDictionary(savegame, out _);

        var players = GetDataFilePlayerData(savegame);

        var playerContracts = DataFileLoaders.GetDataFileContractDictionary(savegame);

        var searchablePlayers = ConstructSearchablePlayers(staffDic, players, playerContracts).ToList();

        return new SaveGameData
        {
            GameDate = savegame.GameDate,
            FirstNames = firstnames,
            Surnames = secondNames,
            CommonNames = commonNames,
            Nations = nations,
            Clubs = clubs,
            Players = searchablePlayers,
            ClubComps = clubcomps
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
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == type);
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
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Players);
        return DataFileLoaders.GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize)
            .Select(Player.Convert)
            .ToList();
    }
}
