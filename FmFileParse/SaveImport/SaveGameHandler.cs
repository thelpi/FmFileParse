using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

public static class SaveGameHandler
{
    const int ByteBlockSize = 268;

    private static readonly List<DataFileFact> _facts =
    [
        new(DataFileType.FirstNames, "first_names.dat", 60, 50),
        new(DataFileType.SecondNames, "second_names.dat", 60, 50),
        new(DataFileType.CommonNames, "common_names.dat", 60, 50),
        new(DataFileType.Staff, "staff.dat", 110, 0),
        new(DataFileType.Players, "player.dat", 70, 0),
        new(DataFileType.Clubs, "club.dat", 581, 0),
        new(DataFileType.ClubComps, "club_comp.dat", 107, 0),
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

        using var sr = new StreamReader(fileName);
        ReadFileHeaders(sr, savegame);
        
        LoadGameData(savegame);

        return PlayerLoader.LoadPlayers(savegame);
    }

    internal static DataFileFact GetDataFileFact(DataFileType type)
        => _facts.First(x => x.Type == type);

    private static DataFileFact GetDataFileFact(string name)
        => _facts.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            ?? new DataFileFact(DataFileType.Unknown, name, 0, 0);

    private static void ReadFileHeaders(StreamReader sr, SaveGameFile savegame)
    {
        using var br = new BinaryReader(sr.BaseStream);
        savegame.IsCompressed = br.ReadInt32() == 4;

        sr.BaseStream.Seek(4, SeekOrigin.Current);

        var blockCount = br.ReadInt32();
        for (var j = 0; j < blockCount; j++)
        {
            var fileHeader = new byte[ByteBlockSize];
            br.Read(fileHeader, 0, ByteBlockSize);
            var internalName = ByteHandler.GetStringFromBytes(fileHeader, 8);

            var fileFacts = GetDataFileFact(internalName);

            savegame.DataBlockNameList.Add(new DataFile(fileFacts, ByteHandler.GetIntFromBytes(fileHeader, 0), ByteHandler.GetIntFromBytes(fileHeader, 4)));
        }
    }

    private static void LoadGameData(SaveGameFile savegame)
    {
        var general = savegame.DataBlockNameList.First(x => x.FileFacts.Type == DataFileType.General);
        var fileFacts = GetDataFileFact(DataFileType.General);

        ByteHandler.GetAllDataFromFile(general, savegame.FileName, fileFacts.DataSize);

        var fileData = ByteHandler.GetAllDataFromFile(general, savegame.FileName, fileFacts.DataSize);
        savegame.GameDate = ByteHandler.GetDateFromBytes(fileData[0], fileFacts.DataSize - 8)!.Value;
    }
}
