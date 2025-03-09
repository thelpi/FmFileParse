using FmFileParse.Models;

namespace FmFileParse.SaveImport;

internal static class DataFileLoaders
{
    public static Dictionary<int, Staff> GetDataFileStaffDictionary(SaveGameFile savegame, out List<Staff> duplicateStaff)
    {
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Staff);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, Staff>(bytes.Count);
        duplicateStaff = [];
        foreach (var item in bytes)
        {
            var staff = Staff.Convert(item);
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
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Contracts);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, Contract>(bytes.Count);
        for (var i = 0; i < bytes.Count; i++)
        {
            var contract = Contract.Convert(bytes[i]);
            if (contract.PlayerId != -1)
            {
                dic.TryAdd(contract.PlayerId, contract);
            }
        }

        return dic;
    }

    public static Dictionary<int, Club> GetDataFileClubDictionary(SaveGameFile savegame)
    {
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Clubs);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, Club>(bytes.Count);
        foreach (var item in bytes)
        {
            var club = Club.Convert(item);
            if (club.Id != -1)
            {
                dic.TryAdd(club.Id, club);
            }
        }

        return dic;
    }

    public static Dictionary<int, ClubComp> GetDataFileClubCompetitionDictionary(SaveGameFile savegame)
    {
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Club_Comps);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, ClubComp>(bytes.Count);
        foreach (var item in bytes)
        {
            var comp = ClubComp.Convert(item);
            if (comp.Id != -1)
            {
                dic.TryAdd(comp.Id, comp);
            }
        }

        return dic;
    }

    public static Dictionary<int, Country> GetDataFileNationDictionary(SaveGameFile savegame)
    {
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.Nations);
        var bytes = GetDataFileBytes(savegame, fileFacts.Type, fileFacts.DataSize);

        var dic = new Dictionary<int, Country>(bytes.Count);
        foreach (var item in bytes)
        {
            var nation = Country.Convert(item);
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
