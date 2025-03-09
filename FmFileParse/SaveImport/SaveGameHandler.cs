namespace FmFileParse.SaveImport;

public static class SaveGameHandler
{
    const int ByteBlockSize = 268;

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

            var fileFacts = DataFileFacts.GetDataFileFact(internalName);

            savegame.DataBlockNameList.Add(new DataFile(fileFacts, ByteHandler.GetIntFromBytes(fileHeader, 0), ByteHandler.GetIntFromBytes(fileHeader, 4)));
        }
    }

    private static void LoadGameData(SaveGameFile savegame)
    {
        var general = savegame.DataBlockNameList.First(x => x.FileFacts.Type == DataFileType.General);
        var fileFacts = DataFileFacts.GetDataFileFacts().First(x => x.Type == DataFileType.General);

        ByteHandler.GetAllDataFromFile(general, savegame.FileName, fileFacts.DataSize);

        var fileData = ByteHandler.GetAllDataFromFile(general, savegame.FileName, fileFacts.DataSize);
        savegame.GameDate = ByteHandler.GetDateFromBytes(fileData[0], fileFacts.DataSize - 8)!.Value;
    }
}
