using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

public static class SaveGameHandler
{
    const int ByteBlockSize = 268;

    private static readonly List<DataFileFact> _facts =
    [
        new(DataFileType.Confederations, "continent.dat", 198, 0),
        new(DataFileType.FirstNames, "first_names.dat", 60, 50),
        new(DataFileType.LastNames, "second_names.dat", 60, 50),
        new(DataFileType.CommonNames, "common_names.dat", 60, 50),
        new(DataFileType.Staff, "staff.dat", 110, 0),
        new(DataFileType.Players, "player.dat", 70, 0),
        new(DataFileType.Clubs, "club.dat", 581, 0),
        new(DataFileType.ClubCompetitions, "club_comp.dat", 107, 0),
        new(DataFileType.Nations, "nation.dat", 290, 0),
        new(DataFileType.General, "general.dat", 3952, 0),
        new(DataFileType.Contracts, "contract.dat", 80, 0, headerOverload: new DataFileHeaderInformation(0, 4, 8, 21, 17))
    ];

    public static SaveGameData OpenSaveGameIntoMemory(string fileName)
    {
        var savegame = new SaveGameFile
        {
            FileName = fileName
        };

        savegame.ReadFileHeaders(fileName);

        savegame.LoadGameData();

        return new SaveGameData
        {
            GameDate = savegame.GameDate,
            Confederations = savegame.GetDataFileConfederationsDictionary(),
            FirstNames = savegame.GetDataFileStringsDictionary(DataFileType.FirstNames),
            LastNames = savegame.GetDataFileStringsDictionary(DataFileType.LastNames),
            CommonNames = savegame.GetDataFileStringsDictionary(DataFileType.CommonNames),
            Nations = savegame.GetDataFileNationsDictionary(),
            Clubs = savegame.GetDataFileClubsDictionary(),
            Players = savegame.GetDataFilePlayersList(),
            ClubCompetitions = savegame.GetDataFileClubCompetitionsDictionary()
        };
    }

    internal static DataFileFact GetDataFileFact(this DataFileType type)
        => _facts.First(x => x.Type == type);

    private static DataFileFact GetDataFileFact(string name)
        => _facts.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            ?? new DataFileFact(DataFileType.Unknown, name, 0, 0);

    private static void ReadFileHeaders(this SaveGameFile savegame, string fileName)
    {
        using var sr = new StreamReader(fileName);
        using var br = new BinaryReader(sr.BaseStream);
        savegame.IsCompressed = br.ReadInt32() == 4;

        sr.BaseStream.Seek(4, SeekOrigin.Current);

        var blockCount = br.ReadInt32();
        for (var j = 0; j < blockCount; j++)
        {
            var fileHeader = new byte[ByteBlockSize];
            br.Read(fileHeader, 0, ByteBlockSize);
            var internalName = fileHeader.GetStringFromBytes(8);

            var fileFacts = GetDataFileFact(internalName);

            savegame.DataBlockNameList.Add(new DataFile(fileFacts, fileHeader.GetIntFromBytes(0), fileHeader.GetIntFromBytes(4)));
        }
    }

    private static void LoadGameData(this SaveGameFile savegame)
    {
        var general = savegame.DataBlockNameList.First(x => x.FileFacts.Type == DataFileType.General);
        var fileFacts = DataFileType.General.GetDataFileFact();

        general.GetAllDataFromFile(savegame.FileName, fileFacts.DataSize);

        var fileData = general.GetAllDataFromFile(savegame.FileName, fileFacts.DataSize);
        savegame.GameDate = fileData[0].GetDateFromBytes(fileFacts.DataSize - 8)!.Value;
    }
}
