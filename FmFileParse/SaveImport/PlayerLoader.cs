using FmFileParse.Models;

namespace FmFileParse.SaveImport;

internal static class PlayerLoader
{
    public static SaveGameData LoadPlayers(SaveGameFile savegame)
    {
        var saveData = new SaveGameData();

        var clubcomps = DataFileLoaders.GetDataFileClubCompetitionDictionary(savegame);

        var firstnames = GetDataFileStringsDictionary(savegame, DataFileType.First_Names);

        var secondNames = GetDataFileStringsDictionary(savegame, DataFileType.Second_Names);

        var commonNames = GetDataFileStringsDictionary(savegame, DataFileType.Common_Names);

        var nations = DataFileLoaders.GetDataFileNationDictionary(savegame);

        var clubs = DataFileLoaders.GetDataFileClubDictionary(savegame);

        var duplicates = new List<Staff>();
        var staffDic = DataFileLoaders.GetDataFileStaffDictionary(savegame, out duplicates);

        var players = GetDataFilePlayerData(savegame);

        var playerContracts = DataFileLoaders.GetDataFileContractDictionary(savegame);

        var searchablePlayers = ConstructSearchablePlayers(staffDic, players, playerContracts).ToList();

        saveData.GameDate = savegame.GameDate;
        saveData.FirstNames = firstnames;
        saveData.Surnames = secondNames;
        saveData.CommonNames = commonNames;
        saveData.Nations = nations;
        saveData.Clubs = clubs;
        saveData.Players = searchablePlayers;
        saveData.ClubComps = clubcomps;

        return saveData;
    }


    private static IEnumerable<Player> ConstructSearchablePlayers(Dictionary<int, Staff> staffDic, List<Player> players, Dictionary<int, Contract> contracts)
    {
        foreach (var player in players)
        {
            if (staffDic.ContainsKey(player.PlayerId))
            {
                var staff = staffDic[player.PlayerId];

                player.Adaptability = staff.Adaptability;
                player.Ambition = staff.Ambition;
                player.ClubId = staff.ClubId;
                player.CommonNameId = staff.CommonNameId;
                player.ContractExpiryDate = staff.ContractExpiryDate;
                player.Determination = staff.Determination;
                player.DOB = staff.DOB;
                player.FirstNameId = staff.FirstNameId;
                player.Id = staff.Id;
                player.InternationalCaps = staff.InternationalCaps;
                player.InternationalGoals = staff.InternationalGoals;
                player.Loyalty = staff.Loyalty;
                player.NationId = staff.NationId;
                player.Pressure = staff.Pressure;
                player.Professionalism = staff.Professionalism;
                player.SecondaryNationId = staff.SecondaryNationId;
                player.SecondNameId = staff.SecondNameId;
                player.Sportsmanship = staff.Sportsmanship;
                player.StaffPlayerId = staff.StaffPlayerId;
                player.Temperament = staff.Temperament;
                player.Value = staff.Value;
                player.Wage = staff.Wage;
                player.Contract = contracts.TryGetValue(staff.Id, out var contract) ? contract : null;

                yield return player;
            }
        }
    }

    private static Dictionary<int, string> GetDataFileStringsDictionary(SaveGameFile savegame, DataFileType type)
    {
        var fileContents = new Dictionary<int, string>();
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == type);
        var fileData = DataFileLoaders.GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        for (var i = 0; i < fileData.Count; i++)
        {
            fileContents.Add(i, ByteHandler.GetStringFromBytes(fileData[i], 0, fileFacts.StringLength));
        }

        return fileContents;
    }

    private static List<Player> GetDataFilePlayerData(SaveGameFile savegame)
    {
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Players);
        var bytes = DataFileLoaders.GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);
        var converter = new PlayerDataConverter();
        var collect = new List<Player>();

        foreach (var source in bytes)
        {
            collect.Add(converter.Convert(source));
        }

        return collect;
    }
}
