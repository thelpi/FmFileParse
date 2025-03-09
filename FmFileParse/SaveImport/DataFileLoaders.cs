using FmFileParse.Models;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    public static Dictionary<int, Staff> GetDataFileStaffDictionary(SaveGameFile savegame, out List<Staff> duplicateStaff)
    {
        var dic = new Dictionary<int, Staff>();
        duplicateStaff = [];
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Staff);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var converter = new StaffConverter();

        foreach (var item in bytes)
        {
            var staff = converter.Convert(item);
            staff.Value = (int)(staff.Value * SaveGameData.ValueMultiplier);
            staff.Wage = (int)(staff.Wage * SaveGameData.ValueMultiplier);

            if (staff.StaffPlayerId != -1)
            {
                if (!dic.TryAdd(staff.StaffPlayerId, staff))
                {
                    duplicateStaff.Add(staff);
                }
            }
        }

        return dic;
    }

    public static Dictionary<int, Contract> GetDataFileContractDictionary(SaveGameFile savegame)
    {
        var dic = new Dictionary<int, Contract>();
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Contracts);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var converter = new ContractConverter();

        for (var i = 0; i < bytes.Count; i++)
        {
            var item = bytes[i];
            var contract = converter.Convert(item);

            if (contract.PlayerId != -1)
            {
                dic.TryAdd(contract.PlayerId, contract);
            }
        }

        return dic;
    }

    public static Dictionary<int, Club> GetDataFileClubDictionary(SaveGameFile savegame)
    {
        var dic = new Dictionary<int, Club>();
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Clubs);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var converter = new ClubConverter();

        foreach (var item in bytes)
        {
            var club = converter.Convert(item);

            if (club.Id != -1)
            {
                dic.TryAdd(club.Id, club);
            }
        }

        return dic;
    }

    public static Dictionary<int, ClubComp> GetDataFileClubCompetitionDictionary(SaveGameFile savegame)
    {
        var dic = new Dictionary<int, ClubComp>();
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Club_Comps);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var converter = new ClubCompConverter();

        foreach (var item in bytes)
        {
            var comp = converter.Convert(item);
            dic.Add(comp.Id, comp);
        }

        return dic;
    }

    public static Dictionary<int, Country> GetDataFileNationDictionary(SaveGameFile savegame)
    {
        var dic = new Dictionary<int, Country>();
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Nations);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var converter = new NationConverter();

        foreach (var item in bytes)
        {
            var nation = converter.Convert(item);

            if (nation.Id != -1)
            {
                dic.TryAdd(nation.Id, nation);
            }
        }

        return dic;
    }

    public static List<byte[]> GetDataFileBytes(SaveGameFile savegame, DataFileType fileType, int sizeOfData)
    {
        var dataFile = savegame.DataBlockNameList.First(x => x.FileFacts.Type == fileType);
        return ByteHandler.GetAllDataFromFile(dataFile, savegame.FileName, sizeOfData);
    }
}
